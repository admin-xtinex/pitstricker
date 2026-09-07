# Pit Striker — Camera Design Specification

## 1. Camera States & Framing
The game camera operates in three distinct states managed by `CameraManager` (utilizing Unity Cinemachine):

```
┌─────────────────────────────────────────────────────────────┐
│                    Camera State Machine                     │
└─────────────────────────────────────────────────────────────┘
          │
          ▼
   [State: Overview] ──(Turn Starts)──> [State: Aiming]
          ▲                                    │
          │                               (Strike Released)
          │                                    ▼
   (All Stop) ◄─────── [State: Action Tracking]
```

1. **State 1: Overview Framing**
   * *Purpose:* Strategic perspective displaying the entire arena, all marbles, and all pits.
   * *Position:* High angle (60° tilt), centered at world origin `(0, 5, -3)`.
   * *Usage:* Match start, score review, and turn handoff transitions.

2. **State 2: Aiming Framing**
   * *Purpose:* Tight, low-angle perspective behind the active player's striker marble.
   * *Position:* Framed behind the marble looking toward the aim vector.
   * *Usage:* Engaged as soon as the player initiates touch contact with the striker.

3. **State 3: Action Tracking**
   * *Purpose:* Dynamic focus following the fast-moving striker and any marbles it impacts.
   * *Behavior:* Soft damping follow using Cinemachine Virtual Camera with target bounding group.
   * *Usage:* Active from marble launch until all rigidbodies have ceased motion.

## 2. Technical Settings
* **Field of View (FOV):** 55° (Perspective).
* **Damping:** Smooth position damping `(x: 0.5, y: 0.5, z: 0.5)` to eliminate sharp camera snapping.
* **Camera Shake (Polish Phase):** Subtle impulse (magnitude `0.1`, duration `0.15s`) triggered upon high-velocity marble-to-marble impacts.
