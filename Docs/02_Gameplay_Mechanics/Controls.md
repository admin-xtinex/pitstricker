# Pit Striker — Mobile Touch Controls Specification

## 1. Interaction Paradigm: Slingshot Pull-and-Release

The primary interaction utilizes a **Pull-Back Slingshot** mechanic:
* **Touch Down:** Touch anywhere near the active striker marble (radius `0.5 units`).
* **Drag:** Pulling the finger away from the intended target stretches a virtual tension spring.
  * Drag Vector $\vec{D} = \text{TouchStart} - \text{TouchCurrent}$
  * Strike Direction = Normalized $\vec{D}$
  * Strike Magnitude = Clamped $\min(|\vec{D}|, \text{MaxDrag})$
* **Release:** Finger lift applies an instantaneous physical impulse along the strike vector.
* **Cancellation:** Dragging back inside the deadzone radius (< 15 pixels) cancels the shot without firing.

## 2. Visual Feedback
* **Trajectory Line:** A dotted projection line extending forward from the marble indicating initial direction.
* **Power Arc / Ring:** An expanding circular reticle around the marble indicating force percentage (0% to 100%).
* **Color Tint:** Shifts from green (gentle nudge) to yellow (moderate strike) to vibrant orange/red (maximum power).

## 3. Touch Handling Safeguards
* **Palm / Accidental Edge Rejection:** Ignore multi-touch inputs when one finger is already tracking a drag vector.
* **Off-Screen Drag:** If finger drags outside the screen boundary, preserve the clamped maximum force rather than dropping the stroke.
