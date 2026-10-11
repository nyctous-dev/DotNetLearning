# 05 — Boids Simulation

![Boids Simulation](BoidsSimulation.png)

<video src="BoidsSimulation.mp4" controls width=100%></video>


## Project Overview

Build a shared, real-time, browser-based Boids simulation using C#, .NET, ASP.NET Core, and Blazor.

The simulation will contain 10,000 boids moving through a large, sparse world. The server will run the authoritative simulation, process interactions from connected users, and distribute updates so that everyone sees the same evolving environment.

Users will be able to explore the world, customize its appearance, and interact with the boids. An administrator will be able to modify simulation-wide settings that affect every connected user.

This project brings together the concepts learned throughout the C# fundamentals, .NET fundamentals, ASP.NET Core, and Blazor sections. It will also introduce real-time communication, performance profiling, and scalable simulation design.

## Project Goals

- Build a functioning Boids simulation using separation, alignment, and cohesion.
- Simulate an initial population of 10,000 boids in a large, sparse world.
- Keep simulation state authoritative on the server and consistent across connected clients.
- Allow users to navigate the world through zooming and panning.
- Support mouse interactions that attract or repel boids while a mouse button is held.
- Provide guest access for viewing and customizing basic visual preferences.
- Implement user accounts, authentication, and authorization.
- Save guest visual preferences in the browser and synchronize preferences to user accounts after login.
- Provide an administrator role for changing simulation-wide settings.
- Use real-time communication to distribute interactions and simulation updates.
- Optimize simulation calculations, network traffic, memory usage, and browser rendering.
- Establish measurable performance benchmarks and scale the simulation incrementally.
- Deploy the finished application, with AWS hosting as a stretch goal.

## Architecture

### Client — Blazor Web Application

The client will provide the interactive browser experience.

Responsibilities include:

- Rendering the simulation efficiently.
- Displaying the visible portion of the world.
- Handling zooming, panning, and mouse input.
- Applying user-specific visual preferences, such as boid color and trails.
- Sending authenticated interaction events to the server.
- Receiving simulation updates and reflecting shared changes.
- Providing account, preference, and administrator interfaces.

Canvas rendering through JavaScript interop is the leading approach for drawing large numbers of boids without creating a separate Blazor component for each one.

### Server — ASP.NET Core

The server will coordinate the application and maintain the authoritative simulation.

Responsibilities include:

- Hosting the web application and API endpoints.
- Authenticating users and enforcing authorization.
- Managing active client connections.
- Validating and processing interaction events.
- Running and updating the shared simulation.
- Applying global simulation settings.
- Distributing simulation updates to connected clients.
- Persisting user accounts, preferences, and global configuration.

SignalR will be evaluated for real-time communication between the server and clients.

### Simulation Engine — C#

The simulation engine will contain the core Boids algorithm and operate independently of the user interface.

Responsibilities include:

- Maintaining boid positions and velocities.
- Calculating separation, alignment, and cohesion.
- Applying attraction and repulsion forces.
- Updating the simulation at a controlled timestep.
- Managing simulation-wide parameters and population size.
- Efficiently locating nearby boids through spatial partitioning.
- Exposing simulation state in a format suitable for efficient distribution.

The simulation should be designed so its computational logic can be tested and profiled without requiring a running browser.

### Persistence and User Preferences

Different types of data will have different lifetimes and storage requirements.

- **Live simulation state:** Initially maintained in server memory.
- **User accounts and roles:** Managed through an appropriate authentication and identity system.
- **User visual preferences:** Stored in the browser for guests and synchronized to persistent account storage for signed-in users.
- **Global simulation settings:** Persisted so administrator changes survive application restarts.
- **Live boid positions and velocities:** Initially reset when the server restarts rather than persisted.

A user's visual preferences affect only their own presentation. Simulation-wide settings, such as the number of boids, affect everyone.

## Access and Interaction Model

### Guests

Guests can view the shared simulation, navigate the world, and change basic visual preferences. Their preferences are stored locally in the browser.

Guests cannot apply forces to the simulation or change global settings.

### Signed-In Users

Signed-in users receive the guest viewing capabilities and can interact with the shared simulation.

Their visual preferences are saved to their accounts so they can follow them across devices and sessions. When a guest signs in, their existing browser preferences should be used as the starting point for their account preferences.

### Administrator

The administrator has the normal user capabilities plus access to simulation-wide controls.

The administrator can change settings such as the boid population and other supported simulation parameters. Changes are applied on the server and reflected for all connected users.

Administrative permissions must be enforced on the server rather than relying on hidden or disabled client-side controls.

### Mouse Interactions

- Holding the left mouse button attracts nearby boids toward the cursor.
- Holding the right mouse button repels nearby boids away from the cursor.
- Releasing the button ends the interaction.
- Moving the cursor while holding a button moves the active force.
- Interactions from multiple users contribute to the same simulation.
- Abandoned interactions must expire if a release event is lost or a user disconnects.

Interaction messages will be rate-limited and validated on the server.

## Performance Strategy

Performance is a core design requirement rather than a final polish step.

The initial target is 10,000 boids and 10 concurrent users. These are starting benchmarks, not guaranteed performance limits.

Planned optimization areas include:

- **Spatial partitioning:** Reduce unnecessary neighbor checks instead of comparing every boid against every other boid.
- **Simulation timing:** Use a controlled simulation timestep independent of each client's rendering frame rate.
- **Memory management:** Minimize unnecessary allocations and repeated work in the simulation's update loop.
- **Rendering:** Use an efficient drawing approach, avoid per-boid component overhead, and render only the relevant visible region where practical.
- **Network efficiency:** Avoid transmitting unnecessarily large or frequent state updates; measure and optimize serialization and update frequency.
- **World navigation:** Keep world coordinates independent of screen coordinates so zooming and panning do not alter the simulation.
- **Profiling:** Measure server simulation time, client frame rate, memory consumption, and network traffic.

The simulation will be scaled incrementally. Boid count will be increased while concurrent users remain constant, then concurrent users will be increased while boid count remains constant. This will help identify whether computation, rendering, networking, or another resource is the limiting factor.

## Implementation Roadmap

### Stage 1 — Simulation Fundamentals

Implement the core Boids algorithm in C#.

- Create boid and simulation models.
- Implement separation, alignment, and cohesion.
- Introduce simulation parameters and controlled updates.
- Build a small, testable simulation before increasing the population.
- Add automated tests for core behavior and boundary conditions.

### Stage 2 — Browser Visualization

Build a single-client visualization.

- Render boids in a large world.
- Implement zooming and panning.
- Add visual preferences, including boid color and trails.
- Implement mouse interactions locally to validate the experience.
- Evaluate Canvas and JavaScript interop for efficient rendering.

### Stage 3 — Server-Authoritative Simulation

Move simulation ownership to the server.

- Run the simulation independently of individual clients.
- Establish an update model and simulation timestep.
- Define the state and event data exchanged with clients.
- Connect the browser visualization to server-provided state.
- Confirm that clients observe the same simulation.

### Stage 4 — Real-Time Communication

Introduce real-time updates and shared interactions.

- Establish client connections using SignalR.
- Broadcast simulation updates to connected users.
- Send attraction and repulsion events to the server.
- Support simultaneous interactions from multiple users.
- Handle disconnects, stale interactions, validation, and rate limits.
- Measure network traffic and update latency.

### Stage 5 — Authentication and Preferences

Implement guest and signed-in experiences.

- Add user registration and login.
- Enforce authentication and authorization.
- Store guest preferences in browser storage.
- Persist signed-in preferences to user accounts.
- Synchronize preferences across sessions and devices.
- Define how browser preferences are reconciled during login.

### Stage 6 — Administrator Controls

Add secure simulation-wide management.

- Introduce an administrator role.
- Provide controls for the boid population and supported global parameters.
- Validate parameter changes on the server.
- Broadcast applicable changes to connected clients.
- Persist global settings across application restarts.
- Verify that ordinary users cannot invoke administrative operations.

### Stage 7 — Performance and Reliability

Establish a repeatable performance-testing process.

- Benchmark the initial 10,000-boid, 10-user target.
- Profile simulation calculations and neighbor searches.
- Optimize spatial partitioning and hot-path operations.
- Measure browser rendering performance.
- Measure serialization costs, bandwidth, and update latency.
- Test disconnects, reconnects, invalid events, and sustained operation.
- Increase boid count and concurrent users independently to locate bottlenecks.

### Stage 8 — Deployment (Stretch Goal)

Prepare the application for public access.

- Configure production settings and secrets.
- Set up persistent storage and appropriate database migrations.
- Configure secure authentication and transport.
- Deploy the application to a hosting environment.
- Evaluate AWS hosting as a stretch goal.
- Test performance and reliability in the deployed environment.
- Evaluate feasibility of larger boid and concurent user counts. 

## Completion Criteria

The project is complete when:

- Guests can access and explore the shared simulation without an account.
- Guests can customize and retain browser-local visual preferences.
- Signed-in users can interact with the simulation and synchronize preferences across devices.
- Left-click attraction and right-click repulsion operate only while their respective buttons are held.
- Multiple users can interact simultaneously and see the shared effects.
- The administrator can change simulation-wide settings securely.
- Global settings persist across server restarts.
- The initial 10,000-boid, 10-user benchmark has been tested and documented.
- Performance bottlenecks can be identified through measurements rather than guesswork.
- The codebase separates simulation logic, web application responsibilities, client rendering, and persistence.
- Automated tests cover critical simulation and application behavior.
- The application can be deployed to a production-like environment.

## Final Objective

Build a technically substantial, interactive simulation that demonstrates practical knowledge of C#, .NET, ASP.NET Core, Blazor, authentication, real-time communication, testing, and performance engineering. The emphasis is not only on making the simulation work, but on understanding how its architecture and measurable performance evolve as the number of boids and connected users increases.