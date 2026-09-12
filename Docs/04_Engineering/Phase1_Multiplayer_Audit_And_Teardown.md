# Pit Striker — Phase 1: Existing Multiplayer Audit & Teardown Report

## 1. Executive Summary
This document provides the formal audit and teardown specification for Pit Striker's online multiplayer system, fulfilling Phase 1 of the [Online Multiplayer Cloud Restructure Master Plan](file:///C:/Users/Tisan/Documents/pitstricker/online_multiplayer_cloud_restructure_master_plan.md).

The existing implementation was an experimental host-based prototype utilizing Unity Netcode for GameObjects (NGO) 2.13.2, Unity Transport (UTP), and Unity Gaming Services (UGS Relay/Lobby/Authentication). This audit identifies the architectural bottlenecks, traffic hotspots, and creates a concrete migration map to transition to a lightweight, cloud-hosted, server-authoritative model on Google Cloud.

---

## 2. Existing Architecture Audit

### 2.1 Networking Library & Framework Stack
| Layer | Current Implementation | Issues Identified |
| :--- | :--- | :--- |
| **Framework** | Unity Netcode for GameObjects (`com.unity.netcode.gameobjects` 2.13.2) | Heavyweight runtime overhead, GameObject reflection, `NetworkObject` lifecycle overhead on Android. |
| **Transport** | Unity Transport Package (UTP) via DTLS Relay | NAT traversal latency, packet fragmentation, Relay allocation quotas. |
| **Cloud Services** | Unity Gaming Services (UGS Authentication, Relay, Lobby) | Requires cloud credentials, external project linking, unpredictable cold-start allocation delays. |
| **Topology** | Listen Server / Host-Client (One player's mobile phone runs server logic) | Severe phone overheating, thermal throttling, battery drain, dropped matches when host backgrounds app. |

### 2.2 Inventory of Multiplayer Scripts
1. `Assets/_Project/Scripts/Networking/NetworkSessionManager.cs` (628 lines):
   - Handles Relay host allocation, join codes, and NGO `NetworkManager` lifecycle.
   - **Status:** **REPLACE** with lightweight `CloudNetworkClient`.
2. `Assets/_Project/Scripts/Networking/NetworkMatchState.cs` (775 lines):
   - `NetworkBehaviour` holding 10+ `NetworkVariable`s (`MatchId`, `ActivePlayerIndex`, `CurrentPhase`, `TurnTimer`, `Player1`, `Player2`, etc.).
   - Uses `ServerRpc` and `ClientRpc` for shot dispatch and rest position sync.
   - **Status:** **REPLACE** with client `CloudMatchState` and cloud `AuthoritativeMatchEngine`.
3. `Assets/_Project/Scripts/Networking/NetworkBootstrap.cs`:
   - UGS anonymous sign-in wrapper.
   - **Status:** **REMOVE** runtime dependency on UGS.
4. `Assets/_Project/Scripts/Networking/QuickMatchManager.cs`:
   - UGS Lobby polling and quick-match matchmaking.
   - **Status:** **REPLACE** with cloud server room matchmaking.
5. `Assets/_Project/Scripts/Networking/DisconnectGracePeriodManager.cs`:
   - Host-side 20-second timer pausing on client disconnect.
   - **Status:** **REPLACE** with server-side reconnect token and grace session manager.
6. `Assets/_Project/Scripts/Networking/NetworkManagerInitializer.cs`:
   - Instantiates `NetworkManager` and `UnityTransport` dynamically.
   - **Status:** **REMOVE** in favor of transport abstraction.
7. `Assets/_Project/Scripts/Networking/MultiplayerAnalytics.cs` & `MultiplayerSecurityLogger.cs`:
   - Validation and logging utilities.
   - **Status:** **MODIFY / ADAPT** into core security validator.
8. `Assets/_Project/Scripts/UI/MenuManager.Online.cs` (1289 lines):
   - Complete procedural UI for Play Mode, Create Match, Join Match, Online Lobby, Result Screen, and Disconnect Overlay.
   - **Status:** **MODIFY** (Preserve 100% of UI layout, re-bind listeners to new cloud network events).
9. `Assets/_Project/Scripts/Gameplay/TurnManager.cs`:
   - Local match manager that had ~15 scattered `if (Unity.Netcode.NetworkManager.Singleton != null)` conditional checks.
   - **Status:** **MODIFY** to completely decouple offline modes from online multiplayer.

---

## 3. Performance & Thermal Hotspot Analysis

1. **Host-Role Thermal Overload:**
   - In 2-player matches, Player 1's Android phone ran PhysX simulation, turn evaluation, timer coroutines, and NGO serialization while also rendering at 60 FPS.
   - This caused thermal throttling on mid-to-low end Android devices within 3–5 minutes of play.
2. **NetworkVariable Dirty Checking:**
   - NGO ticks `NetworkVariable` checks every network tick. Struct serialization (`NetworkPlayerData`) caused per-tick GC allocations.
3. **Cellular NAT Punch-Through & CGNAT Failures:**
   - Peer-to-peer and Relay connections frequently experienced handshake drops on mobile carriers when crossing towers or experiencing high RTT.
4. **App Backgrounding (Android Lifecycle):**
   - If the host received a phone call or minimized the app, the connection died immediately, breaking the match for Player 2.

---

## 4. Migration Map

| Component | Action | Destination / Replacement |
| :--- | :--- | :--- |
| `TurnManager.cs` | **MODIFY** | Completely isolate local/AI gameplay from networking; remove Netcode singletons. |
| `HUDManager.cs` | **MODIFY** | Bind HUD turn timers and active player prompts to new `CloudMatchState` events. |
| `SwipeLaunchController.cs` | **MODIFY** | Dispatch shot intent to `CloudMatchManager` in online mode. |
| `MenuManager.Online.cs` | **MODIFY** | Rebind UI buttons and lobby views to `CloudNetworkClient`. |
| `NetworkSessionManager.cs` | **REPLACE** | Replaced by `CloudNetworkClient` and `WebSocketNetworkTransport`. |
| `NetworkMatchState.cs` | **REPLACE** | Replaced by `CloudMatchState` (client) and `AuthoritativeMatchEngine` (server). |
| `DisconnectGracePeriodManager.cs` | **REPLACE** | Handled natively by cloud server room session state. |
| `QuickMatchManager.cs` | **REPLACE** | Handled natively by cloud server matchmaking queue. |
| `NetworkBootstrap.cs` | **REPLACE** | Direct lightweight handshake with cloud server. |
| Google Cloud Dedicated Host | **NEW** | Standalone .NET 10 `PitStrikerServer` with Docker containerization. |
| Transport Abstraction | **NEW** | `INetworkTransport` with `WebSocketNetworkTransport` implementation. |

---

## 5. Offline Gameplay Preservation Strategy
- All offline game modes (`PassAndPlay` and `PlayerVsAI`) run strictly through `TurnManager.cs` and `MarbleController.cs`.
- Offline gameplay will execute with zero network calls, zero transport allocation, and zero background network tasks.
- If no online session is active, `TurnManager` and `HUDManager` execute locally with full physics determinism and par scoring.

---

## 6. Phase 1 Acceptance Criteria Status
- [x] Existing multiplayer architecture documented.
- [x] Network traffic sources and bottlenecks identified.
- [x] Existing dependencies cataloged.
- [x] High-frequency operations and thermal hotspots identified.
- [x] Reusable and obsolete components clearly mapped.
- [x] Offline gameplay preservation strategy defined.
- [x] Detailed audit report generated.
