# Pit Striker

## Gameplay Scenarios & Strike Rules Specification

**Version:** 1.0 (Living Design Document)

---

# Purpose

This document defines every gameplay scenario, edge case, interaction, collision rule, Strike rule, and special gameplay condition.

Unlike the main Game Rules document, this specification focuses on **"What happens if..."** situations.

As development progresses, this document will grow into the single source of truth for gameplay behavior.

---

# Document Sections

## Section A — Match Start Scenarios

Examples:

* All players complete opening shot successfully.
* One marble enters Pit 1 during the opening shot.
* Two marbles stop at nearly the same distance.
* A marble overshoots Pit 1.
* A marble rolls behind the launch area.
* A marble collides with another during the opening shot.
* A marble falls into another pit accidentally.
* Multiple marbles enter the same pit.
* Marble stops on the pit edge.
* Marble oscillates before stopping.

---

## Section B — Pit Progression Scenarios

Examples:

* Player enters Pit 1.
* Player misses Pit 1.
* Player enters Pit 2 without officially clearing Pit 1.
* Player skips Pit 2.
* Player accidentally enters Pit 3.
* Marble rolls across multiple pits.
* Marble bounces out of a pit.
* Marble rests partially inside a pit.
* Marble circles the pit before entering.
* Marble gets pushed into a pit by another marble.

---

## Section C — Strike Rules

This becomes the official specification for every attacking mechanic.

Example topics:

### Direct Strike

Player intentionally hits opponent.

Expected Result

Opponent marble moves according to physics.

---

### Chain Collision

Player hits Marble A.

Marble A hits Marble B.

Who benefits?

How are turns evaluated?

---

### Double Strike

One shot hits two opponents.

Reward?

Penalty?

Extra turn?

---

### Triple Collision

One shot produces multiple impacts.

How is the result calculated?

---

### Friendly Collision

Opponent accidentally helps another opponent.

Should this be allowed?

---

### Simultaneous Strike

Two marbles collide repeatedly before stopping.

How is the final state determined?

---

### Grazing Hit

Very small contact.

Should it count as Strike?

Minimum collision threshold?

---

### Edge Collision

Opponent is standing beside a pit.

Player attacks.

Marble falls into pit.

How is progression evaluated?

---

## Section D — Pit + Strike Combined Cases

Examples

Player enters Pit 2

AND

Hits opponent.

Which event takes priority?

---

Player hits opponent

Opponent enters pit.

Does opponent receive credit?

---

Player is knocked into target pit by opponent.

Should it count?

---

Player knocks opponent away while entering own target pit.

Is this legal?

---

## Section E — Physics Edge Cases

Examples

Marble spins forever.

Marble vibrates.

Marble balances on pit edge.

Marble clips through another marble.

Marble escapes arena.

Marble leaves play area.

Marble gets stuck.

---

## Section F — Turn Management

Examples

Player disconnects (future online mode)

Player quits.

Player times out.

Player never shoots.

Undo?

Pause?

Resume?

Restart?

---

## Section G — Terrain Scenarios

Beach

* Soft stop
* Sand slowdown

Grass

* Consistent friction

Clay

* Long rolling

Rough Soil

* Random micro resistance (if implemented)

---

## Section H — Camera Scenarios

Camera blocked.

Camera inside terrain.

Camera loses marble.

Camera clips through objects.

Camera rotates during shot.

Camera follows wrong marble.

---

## Section I — Winning Scenarios

Player reaches Pit 3.

Two players finish in same turn.

Player wins via Winner Mode.

Opponent quits.

Timer expires.

Draw.

Sudden death (if implemented).

---

## Section J — Rule Exceptions

This section records every approved exception to the standard rules.

Each entry should include:

* Scenario
* Expected behavior
* Reasoning
* Decision date
* Version introduced

---

# Strike Rule Matrix (Approved Living Rules)

| Scenario          | Expected Behaviour | Prototype Decision | Final Decision |
| ----------------- | ------------------ | ------------------ | -------------- |
| Direct Strike     | Striker dumps forward momentum; target blasted forward. | +1 Extra Play awarded to Striker. | Approved |
| Double Strike     | Two opponents hit in a single shot. | +1 Extra Play awarded; "DOUBLE STRIKE" combo banner displayed. | Approved |
| Push into Pit     | Opponent is pushed into any pit. | Opponent relocated 2.5m to fairway rim; pit stays clear; no progress credited. | Approved |
| Knock Out of Pit  | Opponent inside cup is struck. | Vertical pop-up (+3.2m/s) over rim. Opponent retains cleared pit status. | Approved |
| Chain Collision   | Marble A hits Marble B, which hits Marble C. | Pure physics propagation. Extra play awarded to active striker. | Approved |
| Grazing Hit       | Subtle contact (< 0.2 m/s closing speed). | Physical deflection only; minimum velocity threshold required for Extra Play. | Approved |
| Self Collision    | Not applicable (one marble per player). | Single-marble ownership. | Approved |
| Simultaneous Stop | Marbles stop rolling. | Turn evaluates only when all marbles come to a full stop (< 0.02 m/s). | Approved |
| Edge Bounce       | Marbles bounce off earthen arena boundaries. | Elastic deflection retains marble inside the 16.4m fairway. | Approved |
| Blocked Shot      | Opponent sits along fairway line to pit. | Striker may attempt bank shot, direct blast, or pit approach. | Approved |

---

# Scenario Decision Log

Every gameplay discussion should be recorded here instead of changing the core rules document.

**Scenario ID:** SC-001
Situation: Player A has completed Pit 1 and is aiming for Pit 2. Player B performs a Strike and knocks Player A far away.
Decision: Player A retains Pit 1 completion and must continue aiming for Pit 2 from the new position.
Reason: Maintains progression fairness while rewarding tactical direct attacks.
Status: Approved

**Scenario ID:** SC-002
Situation: Player B strikes Player A, and Player A rolls into a pit (Pit 1, 2, or 3).
Decision: Player A is pocketed and automatically relocated to the fairway rim (2.5m outside pit radius). Does not count as an official pit conquest for Player A.
Reason: Prevents accidental/free pit progression via opponent strikes and keeps the pit cup open.
Status: Approved

**Scenario ID:** SC-003
Situation: Opponent is resting inside a completed pit cup; Striker strikes it directly.
Decision: Striker momentum is dumped (carrom dead-stop), and resting opponent is popped up (+3.2m/s) and launched over the rim onto the fairway.
Reason: Prevents marbles from permanently camping inside pit cups and provides high spectator thrill.
Status: Approved

**Scenario ID:** SC-004
Situation: Striker executes a successful Direct Strike against any opponent marble.
Decision: Striker is immediately granted an Extra Play (bonus stroke) upon all marbles settling.
Reason: Preserves the core traditional competitive tactical marble mechanic.
Status: Approved

---

# Design Philosophy

When deciding any scenario, always follow these priorities:

1. Gameplay fun.
2. Fairness.
3. Skill over luck.
4. Physics consistency.
5. Easy-to-understand rules.
6. Competitive balance.
7. Short, replayable matches.

If realism conflicts with fun, choose the option that creates the better player experience while keeping the physics believable.
