# Pit Striker — Cloud Multiplayer Architecture Guide

## 1. Architectural Philosophy

Pit Striker's online multiplayer system is built as a **lightweight, Google Cloud-hosted, server-authoritative dedicated architecture** optimized for low-end Android mobile hardware and cellular networks.

### Core Pillars
1. **Server Authority**: The cloud server (`PitStrikerServer`) is the single source of truth for room management, turn orchestration, impulse validation, physics settle evaluation, pit conquests, and match victory.
2. **Dedicated Cloud Server over Host-Client**: Neither mobile device runs as the host. This eliminates phone battery drain, thermal throttling, and cellular NAT punch-through/Relay failures.
3. **Decoupled Offline Play**: Single-player and local pass-and-play game modes in `TurnManager` and `MarbleController` run completely isolated from network classes.
4. **Client Prediction & Remote Interpolation**: The active local player experiences zero-latency launch responses via local impulse prediction. Remote marbles interpolate smoothly between snapshot states, absorbing network jitter.
5. **Adaptive Networking & Low GC**: Hot network paths use preallocated reusable byte buffers (`NetworkByteWriter`/`NetworkByteReader`) with zero per-frame garbage generation.

---

## 2. Component Topology

```
+-------------------------------------------------------------------------+
|                        Google Cloud Platform                            |
|  - Compute Engine e2-micro / Cloud Run Container                        |
|  - PitStrikerServer (.NET 10 standalone server, ~30MB RAM)              |
|                                                                         |
|    +--------------------+  +------------------+  +------------------+   |
|    | WebSocketServer    |  | RoomManager      |  | Authoritative    |   |
|    | (Port 7777 / 8080) |  | Matchmaking &    |  | MatchEngine      |   |
|    | Handshake & Ping   |  | Room Codes       |  | Physics & Rules  |   |
|    +--------------------+  +------------------+  +------------------+   |
+------------------------------------+------------------------------------+
                                     |
                         WebSocket (Binary Stream)
                                     |
               +---------------------+---------------------+
               |                                           |
+--------------v---------------+           +---------------v--------------+
|     Android Client 1         |           |       Android Client 2       |
|  - INetworkTransport         |           |  - INetworkTransport         |
|  - WebSocketNetworkTransport |           |  - WebSocketNetworkTransport |
|  - CloudNetworkClient        |           |  - CloudNetworkClient        |
|  - CloudMatchManager         |           |  - CloudMatchManager         |
|  - PredictionController      |           |  - PredictionController      |
|  - AdaptiveOptimizer         |           |  - AdaptiveOptimizer         |
+------------------------------+           +------------------------------+
```

---

## 3. Data Flow & Turn Lifecycle

1. **Room Formation**:
   - Player 1 creates a room (gets 6-character room code) or queues for Quick Match.
   - Player 2 joins with the room code or matches via Quick Match.
   - Server pairs players, broadcasts `MatchFound`, and begins a 3-second countdown (`LobbyCountdown`).
2. **Match Initiation**:
   - Server initializes starting tee coordinates and sends `MatchStarted` + initial `WorldSnapshot`.
   - Client switches to `InGame` HUD.
3. **Turn Execution**:
   - Active player swipes and releases. Local marble immediately responds via client-side prediction (`ApplyImpulse`).
   - Client sends `SubmitShotIntent(seq, timestamp, direction, force)` to server.
   - Server validates force `[0.5N, 45.0N]`, clamps upward pitch, and sets server marble velocity.
   - Server broadcasts `ShotBroadcast` to notify opponent.
4. **Rolling & Settle**:
   - Server integrates physics (damping, boundary reflection, marble collisions) and streams `WorldSnapshot` at 15-20 Hz.
   - Remote client buffers snapshots and smoothly interpolates remote marble position.
   - Local client applies exponential error decay reconciliation.
   - When velocities fall below `0.10 m/s`, server settles physics and evaluates pit captures.
5. **Evaluation**:
   - If target pit was conquered: server advances pit (1 -> 2 -> 3), grants bonus play (up to max 3 shots/turn).
   - If Pit 3 conquered: server marks player as finished, records podium rank, and broadcasts `MatchCompleted`.
   - If no pit or opponent struck: server alternates active turn.
6. **Rematch**:
   - Both players can request a rematch on the results screen.
   - When both confirm, server resets the course and begins the next match.

---

## 4. Mobile Lifecycle & Cellular Reliability

- **Grace Period (20 Seconds)**:
  - If a player backgrounds the app or briefly loses cellular coverage, the server retains the session in a grace period (`DisconnectGracePeriod = 20.0s`).
  - The opponent sees an "Opponent Reconnecting... (20s)" overlay without match interruption.
  - Upon reconnection with the session's `ReconnectToken`, the server restores the socket, cancels the grace timer, toasts "Opponent Reconnected!", and rehydrates the full match state.
- **Forfeit Protection**:
  - If the player fails to reconnect within 20 seconds, the server awards the win to the connected player.
