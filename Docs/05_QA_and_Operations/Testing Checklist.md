# Pit Striker — Testing & QA Checklist

*Run this checklist prior to merging any feature branch into `develop` or creating a test release.*

## 1. Physics & Simulation Checks
- [ ] Marble launches smoothly along expected inverse vector upon finger release.
- [ ] Marble decelerates naturally without endless sliding or abrupt stopping.
- [ ] Marble-to-marble impacts conserve momentum realistically without overlap.
- [ ] High-speed strikes do not tunnel or clip through arena walls or pit boundaries.
- [ ] Sinking marble into pit registers trigger capture only when below rim line.

## 2. Input & UI Checks
- [ ] Dragging outside the screen edges clamps force instead of causing null reference errors.
- [ ] Returning finger to deadzone cancels the strike safely.
- [ ] UI buttons respond crisply without swallowing game field touches.
- [ ] Score increments properly on valid pit capture.

## 3. Device & Stability Checks
- [ ] Device sleep is prevented during active match (`Screen.sleepTimeout = SleepTimeout.NeverSleep`).
- [ ] App pauses and resumes smoothly when Android home button or phone call interrupts.
- [ ] Sustained 60 FPS verified via Android frame overlay or Unity Profiler.
