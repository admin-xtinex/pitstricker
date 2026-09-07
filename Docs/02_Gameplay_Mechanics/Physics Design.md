# Pit Striker — Physics Design Specification

## 1. Physical Units & Scale Standard
In Unity, 1 unit = 1 meter. To avoid floating-point inaccuracies and strange physics behavior, marbles must NOT be modeled at microscopic scales (e.g. 0.01 units).
* **Marble Diameter:** Standardized at `0.2 units` (20cm equivalent in Unity physics scale).
* **Arena Diameter:** Standardized at `4.0 to 6.0 units`.
* **Pit Rim Diameter:** Standardized at `0.45 units` (allowing ~2.25x marble diameter clearance).

## 2. Rigidbody Configuration
* **Mass:** `1.0 kg` (Standardized baseline across all marbles for predictable collisions).
* **Linear Drag:** `0.3` (Simulates air resistance and mild ground resistance).
* **Angular Drag:** `0.8` (Crucial: prevents eternal rolling/ice-skating without unnatural deceleration).
* **Interpolation:** `Interpolate` (Eliminates visual jitter when tracked by camera).
* **Collision Detection:** `Continuous Dynamic` (Guarantees fast-moving marbles do not tunnel through thin arena banks or pit rims).

## 3. Physic Materials Matrix

| Material Name | Dynamic Friction | Static Friction | Bounciness | Friction Combine | Bounce Combine |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `PM_Marble_Glass` | 0.20 | 0.25 | 0.70 | Multiply | Maximum |
| `PM_Surface_BeachSand` | 0.65 | 0.75 | 0.15 | Average | Minimum |
| `PM_Surface_HardClay` | 0.35 | 0.40 | 0.40 | Average | Average |
| `PM_Surface_GardenSoil` | 0.50 | 0.60 | 0.25 | Average | Minimum |
| `PM_Arena_BoundaryRail` | 0.10 | 0.15 | 0.85 | Minimum | Maximum |

## 4. Pit Capture Trigger Logic
A pit is modeled with two colliders:
1. **Physical Mesh / Beveled Lip:** Allows the marble to roll over the rim and tip downward naturally under gravity.
2. **Trigger Zone (`OnTriggerStay`):** Positioned at the bottom of the pit cup.
   * A capture is valid **ONLY IF**:
     * Marble center `transform.position.y` is lower than the rim lip height threshold.
     * Marble linear velocity `magnitude < 0.2 m/s`.
   * This prevents high-speed marbles skimming over a pit from registering as a sink.
