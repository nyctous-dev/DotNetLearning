
let connection = null;
let dotNetHelper = null;
let canvas = null;
let context = null;
let latestSnapshot = null;
let resizeObserver = null;

let worldWidth = 0;
let worldHeight = 0;

let cameraX = 0;
let cameraY = 0;
let zoom = 1;

let isDragging = false;
let activePointerId = null;
let lastPointerX = 0;
let lastPointerY = 0;

const ZOOM_FACTOR = 1.1;
const MAX_ZOOM = 8;

function notifyStatus(status) {
    if (!dotNetHelper) {
        return;
    }

    dotNetHelper
        .invokeMethodAsync("OnConnectionStatusChanged", status)
        .catch(() => {
            // The Blazor component may already have been disposed.
        });
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

    cameraX = Math.max(-maxCameraX, Math.min(maxCameraX, cameraX));
    cameraY = Math.max(-maxCameraY, Math.min(maxCameraY, cameraY));
}

function resizeCanvas() {
    if (!canvas) {
        return;
    }

    const rect = canvas.getBoundingClientRect();
    const newWidth = Math.max(1, Math.round(rect.width));
    const newHeight = Math.max(1, Math.round(rect.height));

    if (canvas.width === newWidth && canvas.height === newHeight) {
        return;
    }

    canvas.width = newWidth;
    canvas.height = newHeight;

    zoom = clampZoom(zoom);
    clampCamera();
    renderLatestSnapshot();
}

function getCanvasPoint(event) {
    const rect = canvas.getBoundingClientRect();

    return {
        x: (event.clientX - rect.left) * canvas.width / rect.width,
        y: (event.clientY - rect.top) * canvas.height / rect.height
    };
}

function renderSnapshot(snapshot) {
    if (!context || !canvas || !snapshot) {
        return;
    }

    const width = canvas.width;
    const height = canvas.height;

    context.clearRect(0, 0, width, height);
    context.fillStyle = "#101820";
    context.fillRect(0, 0, width, height);

    context.save();

    context.translate(width / 2, height / 2);
    context.scale(zoom, zoom);
    context.translate(-cameraX, -cameraY);

    const halfWorldWidth = worldWidth / 2;
    const halfWorldHeight = worldHeight / 2;

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

    context.fillStyle = "#65d9ff";

    for (const boid of snapshot.boids ?? []) {
        const position = boid.position;
        const velocity = boid.velocity;
        const angle = Math.atan2(velocity.y, velocity.x);

        context.save();
        context.translate(position.x, position.y);
        context.rotate(angle);

        context.beginPath();
        context.moveTo(8, 0);
        context.lineTo(-5, -4);
        context.lineTo(-3, 0);
        context.lineTo(-5, 4);
        context.closePath();
        context.fill();

        context.restore();
    }

    context.restore();
}

function renderLatestSnapshot() {
    if (latestSnapshot) {
        renderSnapshot(latestSnapshot);
    }
}

async function publishSnapshot(snapshot) {
    latestSnapshot = snapshot;
    renderSnapshot(snapshot);
}

function handlePointerDown(event) {
    if (event.button !== 0) {
        return;
    }

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
    if (!isDragging || event.pointerId !== activePointerId) {
        return;
    }

    const point = getCanvasPoint(event);
    const deltaX = point.x - lastPointerX;
    const deltaY = point.y - lastPointerY;

    lastPointerX = point.x;
    lastPointerY = point.y;

    cameraX -= deltaX / zoom;
    cameraY -= deltaY / zoom;

    clampCamera();
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

function handleWheel(event) {
    event.preventDefault();

    const point = getCanvasPoint(event);

    const worldX =
        cameraX + (point.x - canvas.width / 2) / zoom;

    const worldY =
        cameraY + (point.y - canvas.height / 2) / zoom;

    const zoomMultiplier =
        event.deltaY < 0
            ? ZOOM_FACTOR
            : 1 / ZOOM_FACTOR;

    zoom = clampZoom(zoom * zoomMultiplier);

    cameraX =
        worldX - (point.x - canvas.width / 2) / zoom;

    cameraY =
        worldY - (point.y - canvas.height / 2) / zoom;

    clampCamera();
    renderLatestSnapshot();
}

function attachCanvasInteractions() {
    canvas.style.cursor = "grab";
    canvas.style.touchAction = "none";

    canvas.addEventListener("pointerdown", handlePointerDown);
    canvas.addEventListener("pointermove", handlePointerMove);
    canvas.addEventListener("pointerup", endDragging);
    canvas.addEventListener("pointercancel", endDragging);
    canvas.addEventListener("lostpointercapture", endDragging);
    canvas.addEventListener("wheel", handleWheel, { passive: false });
}

function detachCanvasInteractions() {
    if (!canvas) {
        return;
    }

    canvas.removeEventListener("pointerdown", handlePointerDown);
    canvas.removeEventListener("pointermove", handlePointerMove);
    canvas.removeEventListener("pointerup", endDragging);
    canvas.removeEventListener("pointercancel", endDragging);
    canvas.removeEventListener("lostpointercapture", endDragging);
    canvas.removeEventListener("wheel", handleWheel);
}

export async function connect(
    helper,
    canvasElement,
    simulationWorldWidth,
    simulationWorldHeight
) {
    dotNetHelper = helper;
    canvas = canvasElement;
    context = canvas.getContext("2d");

    worldWidth = simulationWorldWidth;
    worldHeight = simulationWorldHeight;

    cameraX = 0;
    cameraY = 0;
    zoom = 1;

    resizeCanvas();
    zoom = clampZoom(1);
    clampCamera();

    resizeObserver = new ResizeObserver(resizeCanvas);
    resizeObserver.observe(canvas);

    attachCanvasInteractions();

    const newConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/simulation")
        .withAutomaticReconnect()
        .build();

    newConnection.on("SimulationSnapshot", async snapshot => {
        try {
            await publishSnapshot(snapshot);
        } catch (error) {
            console.error("Failed to render simulation snapshot.", error);
        }
    });

    newConnection.onreconnecting(() => {
        notifyStatus("Reconnecting");
    });

    newConnection.onreconnected(async () => {
        notifyStatus("Connected");
        await requestSnapshot();
    });

    newConnection.onclose(() => {
        notifyStatus("Disconnected");
    });

    connection = newConnection;

    try {
        await connection.start();
        notifyStatus("Connected");
        await requestSnapshot();
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
    await publishSnapshot(snapshot);
}

export async function disconnect() {
    detachCanvasInteractions();

    if (resizeObserver) {
        resizeObserver.disconnect();
        resizeObserver = null;
    }

    isDragging = false;
    activePointerId = null;

    const activeConnection = connection;
    connection = null;

    if (activeConnection) {
        await activeConnection.stop();
    }

    latestSnapshot = null;
    context = null;
    canvas = null;
    dotNetHelper = null;
}
