# Pit Striker — Future Features (Post-Launch & Scalability)

This document tracks high-value features deferred to post-Phase 9 to preserve core development focus.

## 1. Online Multiplayer
* **Protocol:** Authoritative server or Relay-based peer-to-peer (Unity Netcode for GameObjects or Photon Fusion).
* **Architecture Impact:** Since input and turn logic are decoupled via commands (`TurnManager`), network serialization only needs to transmit `(Vector3 direction, float force, uint seed)`.
* **Matchmaking:** Simple Elo-based rank matchmaker with regional lobbies.

## 2. Dynamic Arenas & Hazards
* **Moving Hazards:** Wind gusts on cliffside arenas, water puddles adding hydroplaning friction, magnetic rocks.
* **Destructible Terrain:** Pits that widen as marbles strike their edges.

## 3. Cosmetics & Customization
* **Marble Skins:** Galaxy glass, polished brass, obsidian swirl, translucent iridescent glass.
* **Trail Effects:** Subtle dust trails, mystical glow streaks, spark sparks on high-velocity strikes.
* **Arena Themes:** Desert Oasis, Ancient Roman Marble Forum, Mountain Garden.

## 4. Single-Player Challenge Campaign
* **Trick Shot Puzzles:** Sinking 3 marbles in 1 stroke using bank ricochets.
* **Obstacle Courses:** Maneuvering around pillars and bumps within strict stroke limits.
