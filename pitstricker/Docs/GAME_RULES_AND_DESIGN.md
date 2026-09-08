# Pit Striker

## Game Rules & Gameplay Design

**Version:** 0.1 (Prototype Draft)

---

# 1. Objective

Pit Striker is a competitive multiplayer marble game where players compete to complete the three pits before their opponents while strategically attacking other players to slow their progress.

Victory depends on:

* Precision
* Strategy
* Timing
* Physics
* Decision making

---

# 2. Players

Supported Modes

* 2 Players
* 3 Players
* 4 Players

Each player owns:

* One marble
* One color
* One turn

---

# 3. Match Duration

Target Match Time

**3–8 Minutes**

Matches should always feel quick enough that players immediately want another round.

---

# 4. Arena Layout

The arena contains:

* One Launch Area
* Three pits
* Open play field

Layout

```text
Launch Area

     O  (Pit 1)

----------------------

     O  (Pit 2)

----------------------

     O  (Pit 3)
```

Each pit is separated by a noticeable distance to encourage strategic long-range shots.

---

# 5. Match Start

Every player begins behind the Launch Line.

Each player gets one Opening Shot.

Objective:

Shoot as close as possible to Pit 3.

Turn order is determined by:

Closest marble to Pit 3

↓

Second closest

↓

Third

↓

Fourth

---

# 6. Player Turn

Each turn consists of:

1. Camera adjustment (optional)
2. Aim
3. Set power
4. Shoot
5. Physics simulation
6. Turn evaluation

After the marble completely stops moving,

the turn ends.

---

# 7. Swipe Controls

Player

Touches marble

↓

Drags backward

↓

Aim indicator appears

↓

Power indicator fills

↓

Release finger

↓

Marble shoots

The longer the swipe,

the stronger the shot.

---

# 8. Pit Progression

Every player must complete:

Pit 1

↓

Pit 2

↓

Pit 3

Players cannot skip pits.

Example

Incorrect

Pit 1

↓

Pit 3

Correct

Pit 1

↓

Pit 2

↓

Pit 3

---

# 9. Successful Pit Entry

When a marble enters the correct pit:

* Progress updates
* Pit is marked complete
* Player's next target changes

(Extra-turn rules will be finalized after playtesting.)

---

# 10. Missed Shot

If the player misses:

The marble remains exactly where physics stops it.

No repositioning.

No penalties.

The next player's turn begins.

---

# 11. Marble Collision

Marbles obey physics.

They may:

* Push
* Bounce
* Block
* Redirect
* Stop each other

No scripted collision.

Everything should feel natural.

---

# 12. Strike (Attack)

Instead of aiming for the next pit,

a player may attack an opponent.

Successful Strike

* Moves opponent away
* Changes field positioning
* Creates tactical opportunities

The final displacement depends entirely on collision physics.

---

# 13. Defensive Play

Players may intentionally:

* Block paths
* Protect angles
* Force difficult shots
* Control important positions

Strategy should naturally emerge from the physics system.

---

# 14. Terrain Effects

Each map slightly changes marble behavior.

Beach

* Higher rolling resistance

Grass

* Balanced

Rough Soil

* More resistance

Smooth Clay

* Long rolling distance

The rules stay identical.

Only terrain feel changes.

---

# 15. Camera

Players may:

* Rotate camera
* Zoom
* View target pit
* Follow marble automatically

Camera should never obstruct gameplay.

---

# 16. Turn End

A turn ends only when:

* Every marble has completely stopped moving.

No player may shoot while marbles are still rolling.

---

# 17. Winning (Prototype Rule)

Prototype Version

The first player to successfully complete:

Pit 1

↓

Pit 2

↓

Pit 3

wins the match.

---

# 18. Winner Mode (Future Rule)

Alternative game mode.

Player completes all three pits.

↓

Becomes Winner.

↓

May hunt remaining players.

↓

Match ends after Winner eliminates all remaining opponents.

This mode will be balanced after prototype testing.

---

# 19. Tie Situations

If multiple marbles appear equally close during the opening shot,

distance is calculated mathematically.

No manual judgement.

---

# 20. Physics Principles

The game should reward:

Good aim

Correct power

Correct angle

Good positioning

Prediction

No random outcomes.

---

# 21. Player Skills

A skilled player learns:

* Power control
* Shot angles
* Bank shots
* Collision prediction
* Terrain differences
* Risk assessment

The skill ceiling should remain high.

---

# 22. Match Flow

```text
Lobby

↓

Choose Players

↓

Choose Map

↓

Opening Shot

↓

Turn Order

↓

Pit Progression

↓

Strike Battles

↓

Final Pit

↓

Winner

↓

Results

↓

Rematch
```

---

# 23. Future Game Modes

* Classic Mode
* Winner Mode
* Quick Match
* Tournament
* Ranked
* Custom Rules
* Online Multiplayer

---

# 24. Rule Design Principles

Every rule in Pit Striker should follow these principles:

* Easy to understand.
* Fair for every player.
* Physics-driven rather than scripted.
* Reward skill over luck.
* Encourage player interaction.
* Create memorable moments.
* Keep matches short and replayable.
* Preserve the spirit of the traditional game while adapting it for modern mobile play.
