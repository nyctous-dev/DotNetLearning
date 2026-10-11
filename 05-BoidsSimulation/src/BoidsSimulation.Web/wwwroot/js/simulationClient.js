let connection = null;
let dotNetHelper = null;

let backgroundCanvas = null;
let backgroundContext = null;

let canvas = null;
let context = null;

// Persistent, offscreen canvas containing trail segments only.
let trailCanvas = null;
let trailContext = null;

// Temporary canvas used when reprojection is needed during pan or zoom.
let trailScratchCanvas = null;
let trailScratchContext = null;

let latestSnapshot = null;
let resizeObserver = null;
let animationFrameId = null;
let previousFrameTime = null;

let worldWidth = 0;
let worldHeight = 0;

let cameraX = 0;
let cameraY = 0;
let zoom = 1;

let isDragging = false;
let activePointerId = null;
let lastPointerX = 0;
let lastPointerY = 0;

// Middle-mouse interaction state.
// The server owns the actual forces; this client sends the active
// position and selected mode.
let interactionMode = "Attract";
let isInteracting = false;
let activeInteractionPointerId = null;
let activeInteractionPosition = null;
let interactionKeepAliveTimer = null;
let interactionStartPromise = null;
let interactionUpdatePending = false;

const INTERACTION_KEEP_ALIVE_MS = 250;

let previousBoidPositions = new Map();

let visualPreferences = {
    boidColor: "#36D9F5",
    backgroundColor: "#0A111A",
    showTrails: false
};

const ZOOM_FACTOR = 1.1;
const MAX_ZOOM = 8;

// Trail opacity decays exponentially over time.
// Lower lifetime values produce shorter trails.
const TRAIL_LIFETIME_SECONDS = 0.1;

// Prevent unusually large frame gaps from instantly clearing trails.
const MAX_FRAME_DELTA_SECONDS = 0.05;

function notifyStatus(status) {
    if (!dotNetHelper) return;

    dotNetHelper
        .invokeMethodAsync("OnConnectionStatusChanged", status)
        .catch(() => {});
}

function getMinimumZoom() {
    if (!canvas || worldWidth <= 0 || worldHeight <= 0) {
        return 1;
    }

    return Math.max(
        canvas.width / worldWidth,
        canvas.height / worldHeight
    );
}

function clampZoom(value) {
    const minimumZoom = getMinimumZoom();
    const maximumZoom = Math.max(MAX_ZOOM, minimumZoom);

    return Math.max(minimumZoom, Math.min(maximumZoom, value));
}

function clampCamera() {
    if (!canvas || worldWidth <= 0 || worldHeight <= 0) {
        return;
    }

    const halfWorldWidth = worldWidth / 2;
    const halfWorldHeight = worldHeight / 2;

    const halfViewWidth = canvas.width / (2 * zoom);
    const halfViewHeight = canvas.height / (2 * zoom);

    const maxCameraX = Math.max(
        0,
        halfWorldWidth - halfViewWidth
    );

    const maxCameraY = Math.max(
        0,
        halfWorldHeight - halfViewHeight
    );

    cameraX = Math.max(
        -maxCameraX,
        Math.min(maxCameraX, cameraX)
    );

    cameraY = Math.max(
        -maxCameraY,
        Math.min(maxCameraY, cameraY)
    );
}

function paintBackground() {
    if (!backgroundCanvas || !backgroundContext) {
        return;
    }

    backgroundContext.save();
    backgroundContext.setTransform(1, 0, 0, 1, 0, 0);
    backgroundContext.globalAlpha = 1;
    backgroundContext.globalCompositeOperation = "source-over";

    backgroundContext.clearRect(
        0,
        0,
        backgroundCanvas.width,
        backgroundCanvas.height
    );

    backgroundContext.fillStyle = visualPreferences.backgroundColor;

    backgroundContext.fillRect(
        0,
        0,
        backgroundCanvas.width,
        backgroundCanvas.height
    );

    backgroundContext.restore();
}

function clearVisibleCanvas() {
    if (!canvas || !context) return;

    context.save();
    context.setTransform(1, 0, 0, 1, 0, 0);
    context.globalAlpha = 1;
    context.globalCompositeOperation = "source-over";

    context.clearRect(0, 0, canvas.width, canvas.height);

    context.restore();
}

function clearTrailBuffer() {
    if (!trailCanvas || !trailContext) return;

    trailContext.save();
    trailContext.setTransform(1, 0, 0, 1, 0, 0);
    trailContext.globalAlpha = 1;
    trailContext.globalCompositeOperation = "source-over";

    trailContext.clearRect(
        0,
        0,
        trailCanvas.width,
        trailCanvas.height
    );

    trailContext.restore();

    previousBoidPositions.clear();
}

function resizeOffscreenCanvases(width, height) {
    if (!trailCanvas || !trailScratchCanvas) {
        return;
    }

    const sizeChanged =
        trailCanvas.width !== width ||
        trailCanvas.height !== height ||
        trailScratchCanvas.width !== width ||
        trailScratchCanvas.height !== height;

    if (!sizeChanged) return;

    trailCanvas.width = width;
    trailCanvas.height = height;

    trailScratchCanvas.width = width;
    trailScratchCanvas.height = height;

    previousBoidPositions.clear();
}

function resizeCanvas() {
    if (!canvas || !backgroundCanvas) return;

    const rect = canvas.getBoundingClientRect();

    const width = Math.max(1, Math.round(rect.width));
    const height = Math.max(1, Math.round(rect.height));

    const sizeChanged =
        canvas.width !== width ||
        canvas.height !== height ||
        backgroundCanvas.width !== width ||
        backgroundCanvas.height !== height;

    if (!sizeChanged) return;

    canvas.width = width;
    canvas.height = height;

    backgroundCanvas.width = width;
    backgroundCanvas.height = height;

    resizeOffscreenCanvases(width, height);

    zoom = clampZoom(zoom);
    clampCamera();

    paintBackground();
    clearTrailBuffer();
    renderLatestSnapshot();
}

function getCanvasPoint(event) {
    const rect = canvas.getBoundingClientRect();

    return {
        x: (event.clientX - rect.left) * canvas.width / rect.width,
        y: (event.clientY - rect.top) * canvas.height / rect.height
    };
}

function getWorldPoint(event) {
    const point = getCanvasPoint(event);

    return {
        x: cameraX + (point.x - canvas.width / 2) / zoom,
        y: cameraY + (point.y - canvas.height / 2) / zoom
    };
}

function reprojectTrailBuffer(previousCamera) {
    if (
        !trailCanvas ||
        !trailContext ||
        !trailScratchCanvas ||
        !trailScratchContext ||
        !previousCamera
    ) {
        return;
    }

    if (previousCamera.zoom <= 0 || zoom <= 0) {
        clearTrailBuffer();
        return;
    }

    const width = trailCanvas.width;
    const height = trailCanvas.height;

    // Copy the old trail image before clearing its destination.
    trailScratchContext.save();
    trailScratchContext.setTransform(1, 0, 0, 1, 0, 0);
    trailScratchContext.globalAlpha = 1;
    trailScratchContext.globalCompositeOperation = "source-over";

    trailScratchContext.clearRect(0, 0, width, height);
    trailScratchContext.drawImage(trailCanvas, 0, 0);

    trailScratchContext.restore();

    const scale = zoom / previousCamera.zoom;

    const offsetX =
        width / 2 * (1 - scale) +
        zoom * (previousCamera.x - cameraX);

    const offsetY =
        height / 2 * (1 - scale) +
        zoom * (previousCamera.y - cameraY);

    trailContext.save();
    trailContext.setTransform(1, 0, 0, 1, 0, 0);
    trailContext.globalAlpha = 1;
    trailContext.globalCompositeOperation = "source-over";

    trailContext.clearRect(0, 0, width, height);

    trailContext.setTransform(
        scale,
        0,
        0,
        scale,
        offsetX,
        offsetY
    );

    trailContext.drawImage(trailScratchCanvas, 0, 0);

    trailContext.restore();

    // The old positions are in world coordinates, so retain them.
    // The next segment will be drawn using the new camera transform.
}

function fadeTrailBuffer(deltaSeconds) {
    if (
        !trailCanvas ||
        !trailContext ||
        !visualPreferences.showTrails
    ) {
        return;
    }

    if (deltaSeconds <= 0) return;

    // Exponential decay is independent of the frame rate.
    const fadeAlpha =
        1 - Math.exp(-deltaSeconds / TRAIL_LIFETIME_SECONDS);

    trailContext.save();
    trailContext.setTransform(1, 0, 0, 1, 0, 0);
    trailContext.globalCompositeOperation = "destination-out";
    trailContext.globalAlpha = fadeAlpha;
    trailContext.fillStyle = "#000000";

    trailContext.fillRect(
        0,
        0,
        trailCanvas.width,
        trailCanvas.height
    );

    trailContext.restore();
}

function getBoidKey(boid, index) {
    return boid.id ?? index;
}

function drawTrailSegments(snapshot) {
    if (
        !visualPreferences.showTrails ||
        !trailContext ||
        !canvas ||
        !snapshot
    ) {
        previousBoidPositions.clear();
        return;
    }

    const boids = snapshot.boids ?? [];
    const currentPositions = new Map();

    trailContext.save();

    trailContext.setTransform(1, 0, 0, 1, 0, 0);
    trailContext.globalAlpha = 1;
    trailContext.globalCompositeOperation = "source-over";

    // Draw segments in world coordinates. The camera transform
    // converts their coordinates into viewport pixels.
    trailContext.translate(canvas.width / 2, canvas.height / 2);
    trailContext.scale(zoom, zoom);
    trailContext.translate(-cameraX, -cameraY);

    trailContext.beginPath();
    trailContext.rect(
        -worldWidth / 2,
        -worldHeight / 2,
        worldWidth,
        worldHeight
    );
    trailContext.clip();

    trailContext.strokeStyle = visualPreferences.boidColor;
    trailContext.lineWidth = 1.5 / zoom;
    trailContext.lineCap = "round";
    trailContext.lineJoin = "round";

    trailContext.beginPath();

    for (let index = 0; index < boids.length; index++) {
        const boid = boids[index];
        const position = boid.position;

        if (
            !position ||
            !Number.isFinite(position.x) ||
            !Number.isFinite(position.y)
        ) {
            continue;
        }

        const key = getBoidKey(boid, index);

        const current = {
            x: position.x,
            y: position.y
        };

        const previous = previousBoidPositions.get(key);

        if (previous) {
            const deltaX = current.x - previous.x;
            const deltaY = current.y - previous.y;

            // Boids wrap around the world. Do not draw a long
            // line across the entire world when a wrap occurs.
            const crossedWorldBoundary =
                Math.abs(deltaX) > worldWidth / 2 ||
                Math.abs(deltaY) > worldHeight / 2;

            if (!crossedWorldBoundary) {
                trailContext.moveTo(previous.x, previous.y);
                trailContext.lineTo(current.x, current.y);
            }
        }

        currentPositions.set(key, current);
    }

    trailContext.stroke();
    trailContext.restore();

    previousBoidPositions = currentPositions;
}

function drawBoids(snapshot) {
    if (!context || !canvas || !snapshot) return;

    const width = canvas.width;
    const height = canvas.height;

    context.save();
    context.setTransform(1, 0, 0, 1, 0, 0);
    context.globalAlpha = 1;
    context.globalCompositeOperation = "source-over";

    // Rebuild the visible canvas every frame.
    context.clearRect(0, 0, width, height);

    if (visualPreferences.showTrails && trailCanvas) {
        context.drawImage(trailCanvas, 0, 0);
    }

    context.translate(width / 2, height / 2);
    context.scale(zoom, zoom);
    context.translate(-cameraX, -cameraY);

    const halfWorldWidth = worldWidth / 2;
    const halfWorldHeight = worldHeight / 2;

    // Clip the drawing to the simulation world.
    context.beginPath();
    context.rect(
        -halfWorldWidth,
        -halfWorldHeight,
        worldWidth,
        worldHeight
    );
    context.clip();

    context.strokeStyle = "#405060";
    context.lineWidth = 1 / zoom;

    context.strokeRect(
        -halfWorldWidth,
        -halfWorldHeight,
        worldWidth,
        worldHeight
    );

    // Only visit boids within the visible camera area.
    const margin = 10 / zoom;

    const minX = Math.max(
        -halfWorldWidth,
        cameraX - width / (2 * zoom) - margin
    );

    const maxX = Math.min(
        halfWorldWidth,
        cameraX + width / (2 * zoom) + margin
    );

    const minY = Math.max(
        -halfWorldHeight,
        cameraY - height / (2 * zoom) - margin
    );

    const maxY = Math.min(
        halfWorldHeight,
        cameraY + height / (2 * zoom) + margin
    );

    // Batch all boid triangles into a single path.
    context.beginPath();

    for (const boid of snapshot.boids ?? []) {
        const position = boid.position;
        const velocity = boid.velocity;

        if (!position || !velocity) continue;

        const px = position.x;
        const py = position.y;

        if (
            px < minX || px > maxX ||
            py < minY || py > maxY
        ) {
            continue;
        }

        const angle = Math.atan2(velocity.y, velocity.x);
        const cos = Math.cos(angle);
        const sin = Math.sin(angle);

        // Forward tip: (8, 0)
        context.moveTo(
            px + 8 * cos,
            py + 8 * sin
        );

        // Rear upper vertex: (-5, -4)
        context.lineTo(
            px - 5 * cos + 4 * sin,
            py - 5 * sin - 4 * cos
        );

        // Rear center vertex: (-3, 0)
        context.lineTo(
            px - 3 * cos,
            py - 3 * sin
        );

        // Rear lower vertex: (-5, 4)
        context.lineTo(
            px - 5 * cos - 4 * sin,
            py - 5 * sin + 4 * cos
        );

        context.closePath();
    }

    context.fillStyle = visualPreferences.boidColor;
    context.fill();

    context.restore();
}

function renderFrame(deltaSeconds) {
    if (!canvas || !context) return;

    const snapshot = latestSnapshot;

    if (!snapshot) {
        clearVisibleCanvas();
        return;
    }

    if (visualPreferences.showTrails) {
        fadeTrailBuffer(deltaSeconds);
        drawTrailSegments(snapshot);
    } else {
        clearTrailBuffer();
    }

    drawBoids(snapshot);
}

function renderLatestSnapshot() {
    if (!canvas || !context) return;

    if (!latestSnapshot) {
        clearVisibleCanvas();
        return;
    }

    // Used for immediate camera and preference updates.
    // Do not advance the fade clock outside the animation loop.
    if (visualPreferences.showTrails) {
        drawTrailSegments(latestSnapshot);
    }

    drawBoids(latestSnapshot);
}

function publishSnapshot(snapshot) {
    latestSnapshot = snapshot;
}

function renderAnimationFrame(timestamp) {
    animationFrameId = null;

    if (!canvas || !context) return;

    let deltaSeconds = 0;

    if (previousFrameTime !== null) {
        deltaSeconds = Math.min(
            (timestamp - previousFrameTime) / 1000,
            MAX_FRAME_DELTA_SECONDS
        );
    }

    previousFrameTime = timestamp;

    try {
        renderFrame(deltaSeconds);
    } catch (error) {
        console.error(
            "Failed to render simulation frame.",
            error
        );
    }

    if (canvas && context) {
        animationFrameId = requestAnimationFrame(renderAnimationFrame);
    }
}

function startAnimationLoop() {
    if (animationFrameId !== null) return;

    previousFrameTime = null;
    animationFrameId = requestAnimationFrame(renderAnimationFrame);
}

function stopAnimationLoop() {
    if (animationFrameId !== null) {
        cancelAnimationFrame(animationFrameId);
        animationFrameId = null;
    }

    previousFrameTime = null;
}

// -----------------------------------------------------------------------------
// Mouse interactions with the server-authoritative simulation
// -----------------------------------------------------------------------------

async function startServerInteraction() {
    if (
        !isInteracting ||
        !activeInteractionPosition ||
        !connection ||
        connection.state !== signalR.HubConnectionState.Connected
    ) {
        return;
    }

    if (interactionKeepAliveTimer !== null) {
        clearInterval(interactionKeepAliveTimer);
        interactionKeepAliveTimer = null;
    }

    const position = activeInteractionPosition;
    const activeConnection = connection;

    const startPromise = activeConnection.invoke(
        "StartInteraction",
        position.x,
        position.y,
        interactionMode
    );

    interactionStartPromise = startPromise;

    try {
        await startPromise;

        if (interactionStartPromise === startPromise) {
            interactionStartPromise = null;
        }

        if (!isInteracting || connection !== activeConnection) {
            return;
        }

        // Send the freshest pointer position in case it moved during startup.
        await sendInteractionPosition();

        if (isInteracting && connection === activeConnection) {
            interactionKeepAliveTimer = window.setInterval(
                () => void sendInteractionPosition(),
                INTERACTION_KEEP_ALIVE_MS
            );
        }
    } catch (error) {
        // A reconnect will attempt to establish the interaction again.
        console.warn(
            "Could not start the simulation interaction. Sign-in may be required.",
            error
        );
    } finally {
        if (interactionStartPromise === startPromise) {
            interactionStartPromise = null;
        }
    }
}

async function sendInteractionPosition() {
    if (
        !isInteracting ||
        !activeInteractionPosition ||
        interactionStartPromise ||
        interactionUpdatePending ||
        !connection ||
        connection.state !== signalR.HubConnectionState.Connected
    ) {
        return;
    }

    interactionUpdatePending = true;
    const activeConnection = connection;

    try {
        await activeConnection.invoke(
            "UpdateInteractionPosition",
            activeInteractionPosition.x,
            activeInteractionPosition.y
        );
    } catch (error) {
        // The server expires stale interactions if an update is lost.
        console.debug("Interaction position update was interrupted.", error);
    } finally {
        interactionUpdatePending = false;
    }
}

async function beginInteraction(event) {
    if (event.button !== 1 || isInteracting) {
        return;
    }

    event.preventDefault();

    isInteracting = true;
    activeInteractionPointerId = event.pointerId;
    activeInteractionPosition = getWorldPoint(event);

    if (canvas && canvas.setPointerCapture) {
        canvas.setPointerCapture(event.pointerId);
    }

    await startServerInteraction();
}

async function stopInteraction() {
    if (!isInteracting && activeInteractionPointerId === null) {
        return;
    }

    const pointerId = activeInteractionPointerId;
    const pendingStart = interactionStartPromise;
    const activeConnection = connection;

    isInteracting = false;
    activeInteractionPointerId = null;

    if (interactionKeepAliveTimer !== null) {
        clearInterval(interactionKeepAliveTimer);
        interactionKeepAliveTimer = null;
    }

    // Do not send EndInteraction before an in-flight StartInteraction has
    // completed, otherwise the requests could be processed out of order.
    if (pendingStart) {
        try {
            await pendingStart;
        } catch {
            // Starting may fail if the client is unauthorized or reconnecting.
        }
    }

    if (
        activeConnection &&
        activeConnection.state === signalR.HubConnectionState.Connected
    ) {
        try {
            await activeConnection.invoke("EndInteraction");
        } catch (error) {
            // Disconnect cleanup and the server timeout remain fallbacks.
            console.debug(
                "Could not end the simulation interaction cleanly.",
                error
            );
        }
    }

    activeInteractionPosition = null;
    interactionUpdatePending = false;

    if (
        canvas &&
        pointerId !== null &&
        canvas.hasPointerCapture &&
        canvas.hasPointerCapture(pointerId)
    ) {
        try {
            canvas.releasePointerCapture(pointerId);
        } catch {
            // The browser may already have released the pointer capture.
        }
    }
}

// -----------------------------------------------------------------------------
// Canvas input: middle mouse interacts, left mouse pans, wheel zooms.
// -----------------------------------------------------------------------------

function handlePointerDown(event) {
    // Middle mouse controls attract/repel interaction.
    if (event.button === 1) {
        void beginInteraction(event);
        return;
    }

    // Left mouse continues to pan the camera.
    if (event.button !== 0) return;

    isDragging = true;
    activePointerId = event.pointerId;

    const point = getCanvasPoint(event);

    lastPointerX = point.x;
    lastPointerY = point.y;

    canvas.style.cursor = "grabbing";
    canvas.setPointerCapture(event.pointerId);

    event.preventDefault();
}

function handlePointerMove(event) {
    if (
        isInteracting &&
        event.pointerId === activeInteractionPointerId
    ) {
        activeInteractionPosition = getWorldPoint(event);
        void sendInteractionPosition();
        return;
    }

    if (!isDragging || event.pointerId !== activePointerId) {
        return;
    }

    const point = getCanvasPoint(event);

    const deltaX = point.x - lastPointerX;
    const deltaY = point.y - lastPointerY;

    lastPointerX = point.x;
    lastPointerY = point.y;

    const previousCamera = {
        x: cameraX,
        y: cameraY,
        zoom: zoom
    };

    cameraX -= deltaX / zoom;
    cameraY -= deltaY / zoom;

    clampCamera();

    reprojectTrailBuffer(previousCamera);
    renderLatestSnapshot();
}

function endDragging(event) {
    if (!isDragging || event.pointerId !== activePointerId) {
        return;
    }

    isDragging = false;
    activePointerId = null;

    if (canvas) {
        canvas.style.cursor = "grab";
    }
}

function handlePointerFinish(event) {
    endDragging(event);

    if (
        isInteracting &&
        event.pointerId === activeInteractionPointerId
    ) {
        void stopInteraction();
    }
}

function handleWindowBlur() {
    if (isInteracting) {
        void stopInteraction();
    }

    if (isDragging) {
        isDragging = false;
        activePointerId = null;

        if (canvas) {
            canvas.style.cursor = "grab";
        }
    }
}

function handleAuxClick(event) {
    if (event.button === 1) {
        event.preventDefault();
    }
}

function handleWheel(event) {
    event.preventDefault();

    const point = getCanvasPoint(event);

    const worldX =
        cameraX + (point.x - canvas.width / 2) / zoom;

    const worldY =
        cameraY + (point.y - canvas.height / 2) / zoom;

    const previousCamera = {
        x: cameraX,
        y: cameraY,
        zoom: zoom
    };

    const multiplier =
        event.deltaY < 0 ? ZOOM_FACTOR : 1 / ZOOM_FACTOR;

    zoom = clampZoom(zoom * multiplier);

    cameraX =
        worldX - (point.x - canvas.width / 2) / zoom;

    cameraY =
        worldY - (point.y - canvas.height / 2) / zoom;

    clampCamera();

    reprojectTrailBuffer(previousCamera);
    renderLatestSnapshot();
}

function attachCanvasInteractions() {
    if (!canvas) return;

    canvas.style.cursor = "grab";
    canvas.style.touchAction = "none";

    canvas.addEventListener("pointerdown", handlePointerDown);
    canvas.addEventListener("pointermove", handlePointerMove);
    canvas.addEventListener("pointerup", handlePointerFinish);
    canvas.addEventListener("pointercancel", handlePointerFinish);
    canvas.addEventListener("lostpointercapture", handlePointerFinish);
    canvas.addEventListener("auxclick", handleAuxClick);

    window.addEventListener("blur", handleWindowBlur);

    canvas.addEventListener("wheel", handleWheel, {
        passive: false
    });
}

function detachCanvasInteractions() {
    if (!canvas) return;

    canvas.removeEventListener("pointerdown", handlePointerDown);
    canvas.removeEventListener("pointermove", handlePointerMove);
    canvas.removeEventListener("pointerup", handlePointerFinish);
    canvas.removeEventListener("pointercancel", handlePointerFinish);
    canvas.removeEventListener("lostpointercapture", handlePointerFinish);
    canvas.removeEventListener("auxclick", handleAuxClick);
    canvas.removeEventListener("wheel", handleWheel);

    window.removeEventListener("blur", handleWindowBlur);
}

// -----------------------------------------------------------------------------
// SignalR connection and lifecycle
// -----------------------------------------------------------------------------

export async function connect(
    helper,
    backgroundCanvasElement,
    trailCanvasElement,
    simulationWorldWidth,
    simulationWorldHeight
) {
    dotNetHelper = helper;

    backgroundCanvas = backgroundCanvasElement;
    backgroundContext = backgroundCanvas.getContext("2d");

    canvas = trailCanvasElement;
    context = canvas.getContext("2d");

    trailCanvas = document.createElement("canvas");
    trailContext = trailCanvas.getContext("2d");

    trailScratchCanvas = document.createElement("canvas");
    trailScratchContext = trailScratchCanvas.getContext("2d");

    worldWidth = simulationWorldWidth;
    worldHeight = simulationWorldHeight;

    cameraX = 0;
    cameraY = 0;
    zoom = 1;

    previousBoidPositions.clear();
    latestSnapshot = null;
    previousFrameTime = null;

    resizeCanvas();

    zoom = clampZoom(1);
    clampCamera();

    paintBackground();
    clearTrailBuffer();
    clearVisibleCanvas();

    resizeObserver = new ResizeObserver(resizeCanvas);
    resizeObserver.observe(canvas);

    attachCanvasInteractions();

    const newConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/simulation")
        .withAutomaticReconnect()
        .build();

    newConnection.on("SimulationSnapshot", snapshot => {
        publishSnapshot(snapshot);
    });

    newConnection.onreconnecting(() => {
        if (interactionKeepAliveTimer !== null) {
            clearInterval(interactionKeepAliveTimer);
            interactionKeepAliveTimer = null;
        }

        // Calls on the old transport may still be settling. Allow a fresh
        // start and position update after SignalR reconnects.
        interactionStartPromise = null;
        interactionUpdatePending = false;

        notifyStatus("Reconnecting");
    });

    newConnection.onreconnected(async () => {
        notifyStatus("Connected");

        try {
            await requestSnapshot();

            // SignalR assigns a new connection ID after reconnecting, so the
            // server needs a fresh interaction registration for this client.
            if (isInteracting && activeInteractionPosition) {
                await startServerInteraction();
            }
        } catch (error) {
            console.error(
                "Failed to restore simulation state after reconnect.",
                error
            );
        }
    });

    newConnection.onclose(() => {
        notifyStatus("Disconnected");
    });

    connection = newConnection;

    try {
        await connection.start();

        notifyStatus("Connected");

        await requestSnapshot();

        startAnimationLoop();
    } catch (error) {
        notifyStatus("Disconnected");
        throw error;
    }
}

export async function requestSnapshot() {
    if (
        !connection ||
        connection.state !== signalR.HubConnectionState.Connected
    ) {
        return;
    }

    const snapshot = await connection.invoke("GetSnapshot");
    publishSnapshot(snapshot);
}

export async function disconnect() {
    await stopInteraction();

    stopAnimationLoop();
    detachCanvasInteractions();

    if (resizeObserver) {
        resizeObserver.disconnect();
        resizeObserver = null;
    }

    isDragging = false;
    activePointerId = null;

    isInteracting = false;
    activeInteractionPointerId = null;
    activeInteractionPosition = null;
    interactionStartPromise = null;
    interactionUpdatePending = false;

    if (interactionKeepAliveTimer !== null) {
        clearInterval(interactionKeepAliveTimer);
        interactionKeepAliveTimer = null;
    }

    const activeConnection = connection;
    connection = null;

    if (activeConnection) {
        await activeConnection.stop();
    }

    latestSnapshot = null;
    previousBoidPositions.clear();

    backgroundContext = null;
    backgroundCanvas = null;

    context = null;
    canvas = null;

    trailContext = null;
    trailCanvas = null;

    trailScratchContext = null;
    trailScratchCanvas = null;

    dotNetHelper = null;
}

// -----------------------------------------------------------------------------
// Visual preferences
// -----------------------------------------------------------------------------

export function setPreferences(
    boidColor,
    backgroundColor,
    showTrails
) {
    const isHexColor = value =>
        typeof value === "string" &&
        /^#[0-9a-fA-F]{6}$/.test(value);

    const previousBoidColor = visualPreferences.boidColor;
    const previousBackgroundColor = visualPreferences.backgroundColor;
    const previousShowTrails = visualPreferences.showTrails;

    if (isHexColor(boidColor)) {
        visualPreferences.boidColor = boidColor.toUpperCase();
    }

    if (isHexColor(backgroundColor)) {
        visualPreferences.backgroundColor = backgroundColor.toUpperCase();
    }

    visualPreferences.showTrails = Boolean(showTrails);

    const boidColorChanged =
        previousBoidColor !== visualPreferences.boidColor;

    const backgroundColorChanged =
        previousBackgroundColor !== visualPreferences.backgroundColor;

    const trailsChanged =
        previousShowTrails !== visualPreferences.showTrails;

    if (boidColorChanged || trailsChanged) {
        clearTrailBuffer();
    }

    if (backgroundColorChanged) {
        paintBackground();
    }

    renderLatestSnapshot();
}

// -----------------------------------------------------------------------------
// Interaction mode API for the Blazor UI
// -----------------------------------------------------------------------------

export async function setInteractionMode(mode) {
    if (mode !== "Attract" && mode !== "Repel") {
        throw new Error(
            "Interaction mode must be 'Attract' or 'Repel'."
        );
    }

    if (interactionMode === mode) {
        return;
    }

    interactionMode = mode;

    // Updating the interaction for this connection replaces its prior mode.
    if (isInteracting && activeInteractionPosition) {
        await startServerInteraction();
    }
}