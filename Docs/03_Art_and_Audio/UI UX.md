# Pit Striker — UI / UX Design Specification

## 1. Design Philosophy
* **Minimal HUD:** The screen belongs to the arena. HUD elements remain transparent or peripheral until interaction occurs.
* **Thumb Zone Optimization:** Essential interactive elements (Strike, camera toggle, menu button) reside within easy reach of the player's primary thumb in lower third of screen.
* **High Contrast Typography:** Large readable numerals for turns and scores readable at a glance outdoors under direct sunlight.

## 2. HUD Screen Layout (In-Game)
* **Top Bar:**
  * Left: Pause / Settings Icon
  * Center: Current Turn Indicator ("Player 1's Turn") with Player Color Accent
  * Right: Match Scoreboard (P1: 20 | P2: 15)
* **Center Playfield:**
  * Trajectory Aim Guide (active only during drag)
  * Power Reticle ring centered on active marble
* **Bottom Bar:**
  * Left: Small swipe-hint card at <= 50% opacity. Not a blocking button.
  * Right: STRIKE action button.
  * Camera View Mode toggle stays with the menu overlay.

## 3. Village Play Surface (dev-isotrophic)
* Dirt play lane target: **20 m wide × 34 m long**.
* Pit spacing stays **12 m**. Marble radius stays **0.16 m**.
* Paddy water is decorative scenery outside the dirt. No extra colliders.

## 4. UI Technology: Unity UI Toolkit
* Leverage Unity 6 UI Toolkit (`.uxml` and `.uss` stylesheets) or optimized Canvas with discrete sorting layers to eliminate UI rebuild batching penalties.
