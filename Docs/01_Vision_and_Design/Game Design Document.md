# Pit Striker — Game Design Document (GDD)

## 1. Core Game Loop

```mermaid
graph TD
    Start([Match Start]) --> Break[Opening Break / Placement]
    Break --> Turn[Active Player Turn]
    Turn --> Aim[Player Aims & Drags]
    Aim --> Strike[Impulse Released & Marble Rolls]
    Strike --> PhysWait{Are All Marbles<br/>Completely Stopped?}
    PhysWait -- No --> PhysWait
    PhysWait -- Yes --> Eval[Evaluate Pits & Collisions]
    Eval --> CheckWin{Has a Player Reached<br/>Target Score / Sunk All Target Marbles?}
    CheckWin -- Yes --> End([Match Over / Winner Declared])
    CheckWin -- No --> NextTurn[Advance to Next Player]
    NextTurn --> Turn
```

## 2. Arena Layout & Objectives
* **The Ring:** A circular or rounded-rectangular play field with slight boundary lips (banks).
* **The Pits:** 3 to 5 shallow circular depressions in the terrain.
  * **Center Pit (The Crown Pit):** Worth highest points or acts as instant win if achieved under special conditions.
  * **Perimeter Pits:** Accessible with direct bank shots; lower point yields.
* **Marbles:**
  * **Striker Marbles:** Owned by active players. Used to strike target marbles or shove rival strikers into pits/out of bounds.
  * **Neutral / Target Marbles:** Placed in the center at round start. Sinking these grants bonus points.

## 3. Player Turn Flow
1. **Camera Focus:** Camera centers on the active player's striker marble.
2. **Aiming Phase:**
   * Player touches the striker marble, pulls backward (slingshot style) or drags forward (billiards cue style).
   * Visual trajectory line shows projected direction and initial strike strength.
3. **Release & Simulation:**
   * Force impulse applied to `Rigidbody`.
   * Input is locked until all active rigidbodies drop below linear velocity threshold (`0.05 m/s`) and angular velocity threshold (`0.05 rad/s`).
4. **Evaluation:**
   * Any marble that falls into a pit trigger zone is validated (must rest below rim level).
   * Points are awarded. Sunk marbles are removed or placed in the gutter rack.
   * If a foul occurs (e.g. striker marble sunk without hitting a target), a penalty or turn forfeiture is assessed.

## 4. Victory Conditions
* **Mode A: Target Score Race:** First player to reach 50 points by sinking neutral marbles and banking opponent marbles into hazard pits.
* **Mode B: King of the Pit (Elimination):** Each player has 3 striker marbles. Last player with marbles remaining in the arena wins.
