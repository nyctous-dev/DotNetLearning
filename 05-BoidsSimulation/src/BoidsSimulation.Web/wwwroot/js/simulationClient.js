
let connection = null;
let dotNetHelper = null;

let canvas = null;
let context = null;
let worldWidth = 5000;
let worldHeight = 5000;

async function notifyStatus(status) {
    if (dotNetHelper) {
        await dotNetHelper.invokeMethodAsync(
            "OnConnectionStatusChanged",
            status);
    }
}

function renderSnapshot(snapshot) {
    if (!context || !canvas) {
        return;
    }

    const boids = Array.isArray(snapshot?.boids)
        ? snapshot.boids
        : [];

    const width = canvas.width;
    const height = canvas.height;

    context.clearRect(0, 0, width, height);
    context.fillStyle = "#0b1220";
    context.fillRect(0, 0, width, height);

    // Preserve the world's aspect ratio and center it on the canvas.
    const scale = Math.min(
        width / worldWidth,
        height / worldHeight);

    const renderedWidth = worldWidth * scale;
    const renderedHeight = worldHeight * scale;

    const offsetX = (width - renderedWidth) / 2;
    const offsetY = (height - renderedHeight) / 2;

    context.fillStyle = "#111d31";
    context.fillRect(
        offsetX,
        offsetY,
        renderedWidth,
        renderedHeight);

    // Draw each boid as a small triangle pointing in its direction
    // of travel.
    context.fillStyle = "#7dd3fc";

    for (const boid of boids) {
        const position = boid.position;
        const velocity = boid.velocity;

        if (!position || !velocity) {
            continue;
        }

        if (
            !Number.isFinite(position.x) ||
            !Number.isFinite(position.y) ||
            !Number.isFinite(velocity.x) ||
            !Number.isFinite(velocity.y)
        ) {
            continue;
        }

        const x = offsetX + position.x * scale;
        const y = offsetY + position.y * scale;
        const angle = Math.atan2(velocity.y, velocity.x);

        context.save();
        context.translate(x, y);
        context.rotate(angle);

        context.beginPath();
        context.moveTo(6, 0);
        context.lineTo(-4, -3);
        context.lineTo(-4, 3);
        context.closePath();
        context.fill();

        context.restore();
    }
}

async function publishSnapshot(snapshot) {
    renderSnapshot(snapshot);

    const count = Array.isArray(snapshot?.boids)
        ? snapshot.boids.length
        : 0;

    if (dotNetHelper) {
        await dotNetHelper.invokeMethodAsync(
            "OnSnapshotReceived",
            count);
    }
}

export async function connect(
    helper,
    canvasElement,
    simulationWorldWidth,
    simulationWorldHeight) {

    if (connection) {
        return;
    }

    if (!canvasElement) {
        throw new Error("The simulation canvas was not found.");
    }

    if (
        !Number.isFinite(simulationWorldWidth) ||
        simulationWorldWidth <= 0 ||
        !Number.isFinite(simulationWorldHeight) ||
        simulationWorldHeight <= 0
    ) {
        throw new Error("The simulation world dimensions are invalid.");
    }

    dotNetHelper = helper;
    canvas = canvasElement;
    context = canvas.getContext("2d");
    worldWidth = simulationWorldWidth;
    worldHeight = simulationWorldHeight;

    if (!context) {
        throw new Error("A 2D canvas context could not be created.");
    }

    await notifyStatus("Connecting");

    const newConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/simulation")
        .withAutomaticReconnect()
        .build();

    connection = newConnection;

    newConnection.onreconnecting(async () => {
        await notifyStatus("Reconnecting");
    });

    newConnection.onreconnected(async () => {
        await notifyStatus("Connected");
        await requestSnapshot();
    });

    newConnection.onclose(async () => {
        await notifyStatus("Disconnected");
    });

    try {
        await newConnection.start();
        await notifyStatus("Connected");
        await requestSnapshot();
    } catch (error) {
        connection = null;
        await notifyStatus("Connection failed");
        throw error;
    }
}

export async function requestSnapshot() {
    if (
        !connection ||
        connection.state !== signalR.HubConnectionState.Connected
    ) {
        throw new Error("The simulation connection is not connected.");
    }

    const snapshot = await connection.invoke("GetSnapshot");
    await publishSnapshot(snapshot);
}

export async function disconnect() {
    const currentConnection = connection;
    connection = null;

    if (currentConnection) {
        await currentConnection.stop();
    }

    dotNetHelper = null;
    canvas = null;
    context = null;
}
