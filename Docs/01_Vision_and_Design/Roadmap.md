# Pit Striker — Master Roadmap

## Development Principles
* **Milestone Gating:** Never start Phase N+1 until Phase N exit criteria are met.
* **Just-In-Time Learning:** Learn only the technical skills needed for the active phase.
* **Playable Trunks:** The `main` branch must always produce a functioning, crash-free build.

---

## Phases Overview

```
Phase 0: Project Setup & Repo Architecture (Current)
  ↓
Phase 1: Unity Basics & Core Concepts
  ↓
Phase 2: Physics Sandbox (Rolling & Friction)
  ↓
Phase 3: Core Prototype (Pits, Aiming, Camera)
  ↓
Phase 4: Core Gameplay (Turns, Rules, Win/Loss)
  ↓
Phase 5: Local Pass-and-Play Multiplayer (2-4 Players)
  ↓
Phase 6: Visual Identity & Environments (Beach, Clay, Soil)
  ↓
Phase 7: Game Feel, Audio & Juice
  ↓
Phase 8: QA, Performance Profiling & Bug Squashing
  ↓
Phase 9: Google Play Store Release
```

---

## Detailed Milestone Specifications

### Phase 0: Project Setup & Foundation
* **Goal:** Create repository, folder scaffolding, Git LFS rules, and architectural documentation.
* **Deliverables:** Verified repository structure, Git LFS configuration, complete `Docs/` directory, `.gitignore`.
* **Exit Criteria:** Repository clones cleanly, Git LFS tracks binaries, Unity 6 LTS initializes cleanly.

### Phase 1: Unity Learning & Environment Setup
* **Goal:** Master core Unity editor operations, MonoBehaviour lifecycle, and GameObject composition.
* **Deliverables:** Scratch project demonstrating sphere creation, basic script attachment, and console output.
* **Exit Criteria:** Can independently build a scene with interactive primitive shapes without copy-pasting unfamiliar code.

### Phase 2: Physics Sandbox
* **Goal:** Create a playground testing marble rolling physics, impulse launching, and natural deceleration.
* **Deliverables:** `SC_Sandbox_Physics` scene with arena floor, boundary rails, and a draggable sphere.
* **Exit Criteria:** Touch swipe imparts directional impulse; marble rolls realistically and decelerates smoothly without endless sliding.

### Phase 3: Core Prototype
* **Goal:** Implement ground pits, aim trajectory prediction, and camera framing.
* **Deliverables:** Greybox arena with 3 pits, aim visualizer (dots/line), dynamic camera follow.
* **Exit Criteria:** Marble can be aimed, shot, and captured by trigger zones inside pits consistently.

### Phase 4: Core Gameplay Loop
* **Goal:** Implement full single-player turn rules, foul detection, and scoring.
* **Deliverables:** Game state machine (`TurnManager`), opening break shot, win condition, restart mechanism.
* **Exit Criteria:** Complete match can be played from start to game-over and replayed cleanly.

### Phase 5: Local Multiplayer
* **Goal:** Enable 2 to 4 player pass-and-play on a single Android device.
* **Deliverables:** Turn order rotation, distinct player marble colors, persistent round scoreboard.
* **Exit Criteria:** Four players can play an entire match handing the device back and forth without desync.

### Phase 6: Art & Environments
* **Goal:** Replace greybox geometry with production-ready 3D models, PBR materials, and mobile lighting.
* **Deliverables:** 3 themed arenas (Sunny Beach, Hard Baked Clay, Garden Soil), custom marble materials.
* **Exit Criteria:** Zero `TEMP_` assets remaining; visuals run at 60 FPS on Android device.

### Phase 7: Polish & Juice
* **Goal:** Maximize tactile satisfaction through audio, haptics, and particle effects.
* **Deliverables:** Impact SFX variations, rolling audio loop, dust burst VFX, Android haptic kick on impact.
* **Exit Criteria:** Blind playtesters describe hitting marbles as "juicy", "meaty", and "fun".

### Phase 8: Testing, QA & Profiling
* **Goal:** Eradicate bugs, optimize memory, and ensure frame rate stability.
* **Deliverables:** Android profiling logs, zero memory leaks, test matrix across 3+ distinct Android devices.
* **Exit Criteria:** Sustained 60 FPS; zero GC spikes over 2ms during gameplay.

### Phase 9: Google Play Release
* **Goal:** Launch publicly on the Google Play Store.
* **Deliverables:** Signed `.aab` bundle, Privacy Policy page, store screenshots, promotional graphics.
* **Exit Criteria:** Production release live on Google Play Console.
