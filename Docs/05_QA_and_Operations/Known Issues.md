# Pit Striker — Known Issues & Technical Limitations

*This document notes non-blocking technical quirks or known engine behaviors that do not require an immediate fix but should be kept in mind during development.*

* **Unity Physics Timestep Sensitivity:** If device drops below 30 FPS, physics simulation may display micro-stutter unless maximum allowed timestep is clamped.
* **Continuous Collision Detection Cost:** Using `Continuous Dynamic` on all marbles increases physics CPU overhead slightly; marble count per match is limited to <= 12 to maintain 60 FPS on budget devices.
