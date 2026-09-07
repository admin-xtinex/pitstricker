# Pit Striker — AI Architecture (Phase 5/Future Spike)

## 1. Role of AI
To allow solo practice without a second human player, a simple heuristic AI can replace Player 2.

## 2. Heuristic Raycast Evaluation
Instead of complex neural networks, the AI evaluates candidate strike vectors using raycasts:
1. **Target Identification:** Identify candidate lines between AI Striker and all Neutral Marbles.
2. **Trajectory Raycast:** Raycast from Neutral Marble to nearest open Pit.
3. **Angle Feasibility:** Calculate required angle of strike.
4. **Shot Selection:**
   * *Easy Difficulty:* Pick random target, apply ±15° angle jitter, random force ±25%.
   * *Medium Difficulty:* Select easiest straight-line sink, apply ±5° angle jitter.
   * *Hard Difficulty:* Considers bank shots and deliberate opponent knockouts.

*Implementation is scheduled after local pass-and-play validation (Phase 5).*
