# Pit Striker — Phase 0: Multiplayer Readiness Audit Report

## 1. Executive Summary
This document is the technical readiness audit for adding online multiplayer to **Pit Striker** (Unity 6 LTS, Android mobile game) per `MASTER PROJECT PLAN PROMPT.md`.

---

## 2. Unity Environment Audit (Task 1)

| Parameter | Current Configuration |
| :--- | :--- |
| **Unity Engine Version** | `6000.6.0f1` (Unity 6 LTS, revision f7f8ed4d1e24) |
| **Render Pipeline** | Universal Render Pipeline (URP `17.6.0`), using custom `MobileURPAsset` |
| **Target Platform** | Android (ARM64), Min SDK: API 26 (Android 8.0), Target SDK: Auto. Landscape. Package: `com.xtinex.pitstriker` |
| **Installed Packages** | `Input System 1.20.0`, `URP 17.6.0`, `uGUI 2.6.0`, `Visual Scripting 1.9.12`, `Test Framework 1.8.0`, `Timeline 6.6.0` |
| **Multiplayer / Networking** | None currently installed (`MultiplayerManager.asset` has `m_EnableMultiplayerRoles: 0`) |
| **Input System** | Unity New Input System (`com.unity.inputsystem` 1.20.0), dynamic `InputSystemUIInputModule` replacement |
| **Physics Timestep** | Fixed Timestep: `0.02s` (50 Hz), configured in `TimeManager.asset` |
| **Physics Configuration** | PhysX, Solver Iterations: 12, Velocity Iterations: 6, `ContinuousDynamic` collision, `Interpolate` Rigidbody, Enhanced Determinism: Disabled (`0`) |
| **Build Configuration** | Automated APK pipeline at `Assets/_Project/Scripts/Core/Editor/BuildAPK.cs`, signed with release keystore `Keystore/pitstriker.keystore` |

---

## 3. Project Architecture Audit (Task 2)
- **Active Production Scene:** `Assets/_Project/Scenes/SC_Village_Graphics_Test.unity` (contains village fairway, 3 shallow saucer pits, Cinemachine follow camera, lighting, audio, and runtime canvas).
- **Navigation & UI:** `MenuManager.cs` / `MenuManager.Layout.cs` builds all UI procedurally (`Home`, `ChoosePlayers`, `InGame`, `Rules`, `Pause`, `Maps`, `Settings`).
- **Match & Turn Coordination:** `TurnManager.cs` tracks state machine (`Menu`, `Paused`, `TossPhase`, `ReadyToAim`, `Rolling`, `Evaluating`, `MatchVictory`), active player, course par (Par 8: 2+3+3), current pit (1→2→3), stroke counters, and podium.
- **Physics Controller:** `MarbleController.cs` governs impulse application, continuous collision detection, rest detection, and boundary safety.
- **Pit Zones:** `PitZone.cs` manages shallow saucer pits with trigger stay and post-settle geometric auditing (`AuditPitsPostSettle`).

---

## 4. Gameplay Flow (Task 3)
1. **Home Screen:** User clicks PLAY MATCH.
2. **Match Setup:** Select player count (1-4) and AI bot toggles.
3. **Opening Toss Phase:** Players throw forward towards Pit 3. Shortest distance to Pit 3 wins 1st turn.
4. **Turn Execution:** Active player pulls back (slingshot aim) -> `marble.ApplyImpulse(dir, force)` -> State becomes `Rolling`.
5. **Physics Settle:** Marbles roll and settle (< 0.10 m/s for 8 physics steps).
6. **Turn Evaluation:**
   - Active target pit conquered: Advance target pit, relocate to next tee, bonus play awarded (max 3 shots/turn).
   - Opponent marble hit: Direct strike bonus play awarded.
   - Standard fairway roll: Turn passes to next player.
7. **Match Victory:** When players sink Pit 3, podium rankings are determined, and victory screen opens.

---

## 5. Physics Analysis & Determinism (Task 4)
- **Determinism Check:** PhysX is **not lockstep-deterministic** across different platforms (Android ARM64 vs PC x86_64). Floating-point discrepancies and compiler optimizations mean identical impulse forces will diverge over time.
- **Recommended Approach:** **Hybrid Authoritative Simulation (Option D)**:
  - Active client transmits `ShotCommand(direction, force)` via ServerRpc to the Host.
  - Host validates and applies impulse authoritatively.
  - Host broadcasts shot execution to both clients to initiate immediate visual rolling and sound.
  - During the roll, Host streams periodic snapshot updates (15-20 Hz) for visual continuity.
  - When physics settles on Host, Host audits pits/strokes and broadcasts the authoritative final outcome (resting positions, scores, next turn).
  - Clients snap strictly to the Host's resting state.

---

## 6. Turn & State Ownership (Task 5)
`TurnManager.cs` currently manages all local state. In online multiplayer:
- The Host is the authoritative state machine.
- Clients send input intent (`ServerRpc`) and reflect state updates (`ClientRpc` / `NetworkVariable`).

---

## 7. Reconnection State Dependencies (Task 6)
To restore a match after a network drop or app backgrounding:
- `MatchId`, `MapId` ("village_lane").
- `PlayerSlots[2]`: Client IDs, display names, stroke counts, current target pits, finish status, connection flags.
- `CurrentState`, `ActivePlayerIndex`, `ShotsTakenThisTurn`, `TurnTimer`.
- `MarblePositions[2]` (resting coordinates).

---

## 8. Multiplayer Architecture Recommendation (Task 7)
- **Engine Stack:** Netcode for GameObjects (NGO) + Unity Transport (UTP).
- **Services:** Unity Gaming Services (UGS) — Anonymous Authentication, Relay (NAT traversal), Lobby (Quick Match & 6-character Join Codes).
- **Match Size:** 2 Players, single map (`village_lane`).

---

## 9. Risk Assessment (Task 8)
- **High Risk:** Cross-device physics divergence (mitigated by Host authority + settle position snapping).
- **Medium Risk:** Android app lifecycle / backgrounding drops (mitigated by 20-second reconnection window and session state rehydration).
- **Low Risk:** Single-player regression (mitigated by keeping offline `TurnManager` and offline UI pathways separate and unedited).

---

## 10. Phase 1 Implementation Roadmap
1. Install verified Unity 6 LTS networking packages (`com.unity.netcode.gameobjects`, `com.unity.services.authentication`, `com.unity.services.relay`, `com.unity.services.lobby`).
2. Implement `NetworkBootstrap` and `NetworkSessionManager` for anonymous login and Relay allocation.
3. Build the Online Multiplayer UI within `MenuManager` (Quick Match, Create Room with Join Code, Join Room with Code).
4. Implement authoritative 2-player turn and physics synchronization.
5. Validate via Editor + Android device testing.
