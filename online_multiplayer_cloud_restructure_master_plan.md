# Online Multiplayer — Complete Cloud Restructure

## Master Implementation Specification

### Purpose

The current Unity Android multiplayer implementation is an experimental test implementation. It must be completely restructured rather than incrementally patched.

The goal is to build a lightweight, cloud-hosted, server-authoritative multiplayer architecture designed for:

- Low-end Android devices
- Mobile networks with variable latency
- Weak/unstable connections
- Packet loss and jitter
- Minimal client CPU/network usage
- Minimal data transmission
- Smooth gameplay despite moderate latency
- Future scalability

The initial backend target is Google Cloud. Free-tier resources may be used for development/prototyping where technically and regionally eligible, but the architecture must not depend on Free Tier limitations.

---

# GLOBAL DEVELOPMENT RULE

## Complete First, Manually Verify Later

The AI must treat this document as the complete multiplayer restructuring specification.

Do **not** stop after each phase to ask the developer to manually verify it.

Instead:

1. Read and understand the entire project specification before implementation.
2. Inspect the existing Unity project.
3. Execute Phase 1.
4. Validate Phase 1 using automated/project-level checks.
5. Continue automatically to Phase 2.
6. Validate Phase 2.
7. Continue through all remaining phases.
8. Do not wait for manual approval between phases.
9. After all phases are implemented and internally validated, produce a final implementation report.
10. The developer will then manually verify each phase independently using the phase acceptance criteria in this document.

The AI must preserve phase boundaries in the implementation and documentation even though execution proceeds continuously.

If a phase encounters a blocker:

- Investigate it.
- Fix it if the fix is within scope.
- If it depends on an external credential, service, unavailable environment, or human decision, document the exact blocker.
- Continue with all other work that can safely be completed.
- Do not silently skip requirements.

Do not claim a phase is complete if its acceptance criteria have not been met.

---

# 1. ARCHITECTURE PRINCIPLES

## 1.1 Server Authority

The server is authoritative for important multiplayer state.

The client must not be trusted as the final authority for:

- Important player state
- Scores
- Match results
- Damage/results
- Match lifecycle
- Other gameplay-critical values

The client primarily sends player intent/input.

Preferred flow:

```text
Android Client
    |
    | Input / Intent
    | Sequence Number
    | Timestamp
    v
Cloud Server
    |
    | Authoritative Simulation
    | Validation
    | Match State
    v
Compact State Snapshot
    |
    v
Android Clients
```

---

## 1.2 Minimal Data Sharing

Do not synchronize every Unity frame.

Rendering frequency and networking frequency are independent.

For example:

```text
Rendering:       30–60 FPS
Input sending:   adaptive, approximately 10–30 Hz
State snapshots: adaptive, approximately 8–20 Hz
Critical events: reliable/immediate
```

These are starting targets, not mandatory fixed values. Actual rates must be chosen using profiling and testing.

Do not transmit information that can be derived locally.

Avoid unnecessary synchronization of:

- Camera state
- Redundant transforms
- Derived animation values
- Local visual effects
- Data that has no gameplay significance
- Repeated unchanged state

---

## 1.3 Client Prediction

The local player must remain responsive without waiting for a server round trip.

Flow:

```text
Input
  |
  +--> Local prediction immediately
  |
  +--> Server
          |
          v
   Authoritative result
          |
          v
     Reconciliation
```

---

## 1.4 Remote Player Interpolation

Remote players must not visibly teleport between network snapshots.

Use interpolation/snapshot buffering as appropriate.

The implementation should tolerate:

- Jitter
- Missing packets
- Uneven packet arrival
- Moderate latency

---

## 1.5 Reliable vs Unreliable Data

Use unreliable delivery for high-frequency replaceable state where appropriate.

Examples:

- Movement
- Rotation
- Aim
- Velocity
- Other continuously changing state

Use reliable delivery for important events where losing a packet would break match state.

Examples:

- Player joined
- Player left
- Match start
- Match end
- Important gameplay events
- Score/result events
- Required state transitions

Do not make all traffic reliable by default.

---

# 2. ANDROID / LOW-END DEVICE REQUIREMENTS

The multiplayer system must minimize:

- CPU usage
- Memory usage
- Garbage collection
- Allocations
- Serialization overhead
- Network processing
- Thread usage
- Main-thread work
- Excessive logging

Avoid allocations in high-frequency networking paths.

Use reusable buffers/object pooling where appropriate.

Do not perform unnecessary work every Unity frame.

Cloud migration must not be treated as the only solution to overheating.

Profile and optimize:

- Networking
- Serialization/deserialization
- Update loops
- Physics
- Garbage generation
- Main-thread processing
- Background processing

Thermal behavior must be validated on real Android devices.

---

# 3. NETWORK QUALITY STRATEGY

The system must degrade gracefully.

### Good Network

- Higher update frequency
- Normal interpolation
- Normal payload size

### Medium Network

- Reduced update frequency where useful
- Slightly larger interpolation buffer
- Avoid unnecessary retransmission

### Poor Network

- Lower update frequency
- Smaller payloads
- Stronger interpolation/prediction
- Preserve critical events

### Very Poor Network

- Avoid network feedback loops
- Avoid excessive retries
- Preserve critical state
- Maintain the best possible playable local experience
- Reconnect gracefully when possible

The implementation must measure network conditions rather than pretending that a fixed quality exists.

Track where practical:

- RTT
- Packet loss
- Jitter
- Send rate
- Receive rate
- Server tick timing

---

# 4. GOOGLE CLOUD STRATEGY

The initial prototype should use Google Cloud.

Use the smallest technically suitable server for development.

Free Tier may be used where eligible.

Important:

- Do not assume every Google Cloud region qualifies for Free Tier Compute Engine.
- Do not design the architecture around a permanently free production server.
- Keep deployment reproducible.
- Keep server configuration externalized.
- Keep the protocol independent of the VM size.
- Make future scaling possible without rewriting the client.

The first implementation should prioritize correctness and minimal resource usage over premature infrastructure complexity.

---

# 5. PROJECT PHASES

The following phases must be implemented in order.

The AI should complete all phases before requesting manual verification.

---

# PHASE 1 — EXISTING MULTIPLAYER AUDIT AND TEARDOWN

## Objective

Understand the existing multiplayer implementation and determine what can be reused, what must be replaced, and what must be removed.

## Tasks

1. Inspect the complete Unity project structure.
2. Identify:
   - Current networking library/framework
   - Multiplayer scripts
   - Network managers
   - Player synchronization code
   - RPCs/events
   - Serialization
   - Network update loops
   - Connection lifecycle
   - Lobby/match logic
   - Existing server/client code
3. Identify all high-frequency network operations.
4. Identify per-frame network traffic.
5. Identify unnecessary synchronized data.
6. Identify code potentially responsible for excessive CPU/GC usage.
7. Identify network-related background threads/tasks.
8. Identify dependencies that conflict with the new architecture.
9. Preserve offline gameplay functionality.
10. Create a migration map:
    - KEEP
    - MODIFY
    - REPLACE
    - REMOVE
11. Do not implement the new architecture yet unless necessary for a minimal scaffold.
12. Produce a detailed audit report in the project.

## Acceptance Criteria

- Existing multiplayer architecture is documented.
- Network traffic sources are identified.
- Existing multiplayer dependencies are identified.
- High-frequency operations are identified.
- Potential thermal/performance hotspots are identified.
- Reusable and obsolete components are clearly separated.
- Offline gameplay remains intact.
- A concrete migration plan exists.

---

# PHASE 2 — NEW CLIENT/SERVER ARCHITECTURE

## Objective

Create the structural foundation for the new multiplayer system.

## Tasks

1. Define clear client/server responsibilities.
2. Separate:
   - Gameplay
   - Networking
   - Transport
   - Serialization
   - Server logic
   - Match/lobby logic
3. Define authoritative state.
4. Define client input model.
5. Define server snapshot model.
6. Define sequence numbers.
7. Define timestamps.
8. Define protocol versioning.
9. Define connection states.
10. Define match states.
11. Create interfaces/abstractions so transport implementation can be changed later.
12. Prevent gameplay code from becoming tightly coupled to a specific networking implementation.

## Acceptance Criteria

- Client/server responsibilities are explicit.
- Networking is isolated from rendering.
- Networking is isolated from camera logic.
- Protocol version exists.
- Input/state models exist.
- Connection lifecycle is defined.
- Match lifecycle is defined.
- Architecture supports future transport/server changes.

---

# PHASE 3 — MINIMAL NETWORK PROTOCOL AND CLOUD SERVER FOUNDATION

## Objective

Implement the minimum viable network protocol and a lightweight Google Cloud server foundation.

## Tasks

1. Implement client connection.
2. Implement server connection handling.
3. Implement compact message structures.
4. Implement:
   - Input messages
   - State snapshots
   - Reliable events
   - Sequence numbers
   - Timestamps
   - Protocol version
5. Implement basic room/match creation.
6. Implement player join/leave.
7. Implement heartbeat/connection health.
8. Create server build/deployment process.
9. Deploy the development server to Google Cloud.
10. Externalize:
    - Server address
    - Port
    - Environment
    - Logging level
11. Ensure credentials/secrets are not hard-coded.
12. Keep server memory and CPU footprint low.

## Acceptance Criteria

- Android client can connect to cloud server.
- Server can accept multiple clients.
- Players can join and leave a match.
- Messages are versioned.
- High-frequency messages are compact.
- Critical events have appropriate reliability.
- Server can be deployed reproducibly.
- No credentials are committed to source control.

---

# PHASE 4 — SERVER-AUTHORITATIVE MULTIPLAYER GAMEPLAY

## Objective

Move actual multiplayer gameplay state to the authoritative server.

## Tasks

1. Implement authoritative player state.
2. Process player input on the server.
3. Validate important input.
4. Synchronize authoritative state to clients.
5. Implement match state.
6. Implement player join/leave state.
7. Implement match start/end.
8. Synchronize only required gameplay state.
9. Remove unnecessary client-authoritative state.
10. Keep offline mode functioning independently.

## Acceptance Criteria

- Server owns authoritative multiplayer state.
- Client cannot simply declare authoritative position/result.
- Multiple players can participate in one match.
- Match lifecycle works.
- Required gameplay state is synchronized.
- Unnecessary state is not synchronized.
- Offline gameplay remains functional.

---

# PHASE 5 — PREDICTION, INTERPOLATION AND RECONCILIATION

## Objective

Make the game responsive and visually smooth under real-world latency.

## Tasks

### Local Player

Implement:

- Client-side prediction
- Input buffering
- Sequence tracking
- Server reconciliation

### Remote Players

Implement:

- Snapshot buffering
- Interpolation
- Graceful handling of missing snapshots
- Jitter smoothing

### Corrections

Corrections must be visually controlled.

Avoid obvious:

- Teleporting
- Rubber-banding
- Position snapping

unless the difference is large enough that an immediate correction is required.

## Acceptance Criteria

- Local controls do not wait for server response.
- Remote movement is smooth.
- Moderate latency remains playable.
- Jitter does not cause severe visual instability.
- Server corrections work.
- Duplicate/lost/out-of-order packets are handled appropriately.

---

# PHASE 6 — ADAPTIVE NETWORKING AND LOW-END OPTIMIZATION

## Objective

Optimize network and device resource usage.

## Tasks

1. Profile CPU usage.
2. Profile memory.
3. Profile allocations/GC.
4. Profile network traffic.
5. Profile serialization/deserialization.
6. Measure packet rate.
7. Measure payload size.
8. Implement adaptive synchronization.
9. Reduce unnecessary state frequency.
10. Reduce unnecessary payload size.
11. Use quantization/compression only where it provides measurable benefit without unacceptable complexity.
12. Avoid allocations in hot paths.
13. Reuse buffers/objects where appropriate.
14. Reduce unnecessary logging.
15. Minimize main-thread networking work.
16. Test on low-end Android hardware.

## Acceptance Criteria

- Network traffic is measurably reduced from the experimental implementation.
- CPU/network processing is reduced.
- GC pressure from networking is minimized.
- Low-end device remains responsive.
- No significant gameplay regression is introduced.
- Adaptive networking behaves predictably.
- Thermal behavior improves or at minimum does not worsen under comparable gameplay conditions.

---

# PHASE 7 — RELIABILITY, RECONNECT AND EDGE CASES

## Objective

Make the multiplayer system robust to real-world mobile-network failures.

## Tasks

Test and handle:

- Temporary network loss
- Network reconnect
- Server disconnect
- Client disconnect
- App background/foreground where applicable
- Duplicate messages
- Out-of-order messages
- Packet loss
- High latency
- High jitter
- Player leaving unexpectedly
- Server restart
- Match interruption

Implement appropriate:

- Timeouts
- Heartbeats
- Reconnection
- State resynchronization
- Match cleanup

Do not create endless retry loops.

## Acceptance Criteria

- Temporary network interruptions do not permanently break the client.
- Reconnection works where supported by the match design.
- Stale players are removed correctly.
- Match state remains consistent.
- Duplicate/out-of-order data does not corrupt state.
- Server restart behavior is predictable.
- Failure states are visible in logs/debug UI during development.

---

# PHASE 8 — COMPLETE VALIDATION, STRESS, LATENCY AND THERMAL TESTING

## Objective

Validate the entire architecture before production integration.

## Test Matrix

### Devices

- Low-end Android
- Mid-range Android
- High-end Android

### Network

- Good 5G/4G
- Slow mobile network
- High latency
- Packet loss
- Jitter
- Temporary disconnect
- Reconnect

### Player Count

Test the actual intended maximum player count.

Also test:

- 1 player
- 2 players
- 4 players
- Intermediate counts
- Maximum intended count

## Metrics

### Client

Measure:

- FPS
- CPU
- Memory
- GC allocations
- Network RTT
- Packet loss
- Network send rate
- Network receive rate
- Thermal behavior
- Battery/energy impact where practical

### Server

Measure:

- CPU
- RAM
- Bandwidth
- Connected players
- Match count
- Tick duration
- Tick stability
- Packet rate
- Server-side latency
- Error rate

## Acceptance Criteria

The system must:

- Connect reliably.
- Synchronize players correctly.
- Remain playable under moderate latency.
- Remain visually stable under jitter.
- Handle packet loss gracefully.
- Recover from temporary disconnects where supported.
- Support the intended player count.
- Run acceptably on low-end Android hardware.
- Avoid excessive thermal load attributable to networking.
- Maintain low and predictable bandwidth usage.
- Produce useful diagnostic information.

---

# 6. PERFORMANCE TARGETS

These are engineering targets, not promises.

Aim for:

```text
Excellent RTT       < 50 ms
Good RTT            50–100 ms
Acceptable RTT      100–200 ms
Degraded but usable 200–300 ms
Poor                > 300 ms
```

The actual gameplay tolerance must be determined through testing.

Network update rates should be tuned experimentally rather than assumed.

Initial investigation range:

```text
Input:     ~10–30 Hz
Snapshots: ~8–20 Hz
```

The final values must be selected based on:

- Gameplay feel
- Bandwidth
- CPU cost
- Packet loss behavior
- Device performance

---

# 7. CODE QUALITY REQUIREMENTS

The AI must:

- Follow existing project conventions where sensible.
- Avoid unnecessary rewrites outside multiplayer scope.
- Avoid duplicate systems.
- Keep responsibilities separated.
- Use clear naming.
- Add comments only where useful.
- Avoid excessive abstractions.
- Avoid premature optimization.
- Profile before making major performance claims.
- Keep debug instrumentation removable or disableable.
- Keep development configuration separate from production configuration.
- Avoid hard-coded secrets.
- Avoid hard-coded environment-specific addresses in gameplay code.

---

# 8. DOCUMENTATION REQUIREMENTS

Maintain project documentation for:

1. Architecture
2. Protocol
3. Server setup
4. Local development
5. Google Cloud deployment
6. Configuration
7. Debugging
8. Performance metrics
9. Known limitations
10. Validation results

Each phase must have:

- Implementation summary
- Files/components changed
- Automated/internal validation performed
- Known issues
- Acceptance criteria status

---

# 9. FINAL REPORT

After all implementation phases are completed, produce a final report containing:

## Architecture

- Final client architecture
- Final server architecture
- Transport/protocol
- Data flow

## Networking

- Input frequency
- Snapshot frequency
- Reliable events
- Unreliable state
- Payload sizes
- Adaptive behavior

## Performance

- CPU impact
- Memory impact
- GC impact
- Network bandwidth
- Server resource usage

## Cloud

- Google Cloud configuration
- Deployment procedure
- Development/prototype limitations
- Scaling considerations

## Testing

- Device results
- Latency results
- Packet-loss results
- Reconnection results
- Stress results
- Thermal results

## Known Limitations

Clearly list anything that could not be fully validated.

## Manual Verification Checklist

Provide a concise checklist grouped by:

- Phase 1
- Phase 2
- Phase 3
- Phase 4
- Phase 5
- Phase 6
- Phase 7
- Phase 8

The developer will use this checklist to manually verify the finished implementation.

---

# 10. FINAL EXECUTION INSTRUCTION

Treat this entire document as one unified engineering task.

Do not implement Phase 1 in isolation and stop.

First understand the complete target architecture.

Then execute:

```text
PHASE 1
  ↓
Internal validation
  ↓
PHASE 2
  ↓
Internal validation
  ↓
PHASE 3
  ↓
Internal validation
  ↓
PHASE 4
  ↓
Internal validation
  ↓
PHASE 5
  ↓
Internal validation
  ↓
PHASE 6
  ↓
Internal validation
  ↓
PHASE 7
  ↓
Internal validation
  ↓
PHASE 8
  ↓
Final report
  ↓
Manual developer verification
```

Do not ask for manual approval between phases.

Do not declare success based only on compilation.

For each phase, validate the actual intended behavior.

If the existing project architecture differs from assumptions in this specification, adapt the implementation to the actual project while preserving the architectural goals.

The final result must be a production-oriented foundation, not merely a multiplayer connectivity demo.
