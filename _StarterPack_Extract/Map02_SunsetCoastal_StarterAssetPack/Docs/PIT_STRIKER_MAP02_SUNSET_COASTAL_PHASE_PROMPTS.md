# Pit Striker — Map 02 "Sunset Coastal"
## Phased Implementation Prompts

> **Primary goal:** Build the new **Map 02 — Sunset Coastal** as an additional map using the project's existing map-slot / map-loading system.
>
> **Highest priority:** Match the approved visual direction — cinematic coastal artwork, evening/sunset atmosphere, strong framing, premium environment presentation — while keeping the implementation efficient for mobile.
>
> **Do NOT replace or delete the current map yet.** The existing map must remain fully functional as the reference/fallback map until Map 02 is approved.
>
> **Gameplay is NOT the first priority in these phases.** Reuse existing gameplay systems and avoid rewriting physics, strike logic, turn logic, pits, multiplayer, scoring, or UI unless a minimal compatibility change is absolutely required.

---

# Global Rules for Every Phase

Use these rules in every phase unless the user explicitly changes them later.

1. **Work inside the existing Pit Striker project and existing map-slot architecture.**
2. Create **Map 02 — Sunset Coastal** as a new map/scene/prefab according to the architecture already present in the project.
3. **Do not modify or remove the current production/reference map.**
4. Reuse existing:
   - marble prefab and physics settings
   - strike / toss system
   - power system
   - pit detection
   - turn/match systems
   - multiplayer/network logic
   - existing UI
   - existing map-slot contract
5. Prioritize **visual quality and framing first**, gameplay tuning later.
6. The visible world must be optimized as a **hybrid 3D + 2.5D environment**:
   - true 3D for the playable lane and near-camera hero assets
   - lightweight 3D for near scenic assets
   - impostors / cards / background layers / very-low-poly silhouettes for far scenery
7. Do **not** build a complete open-world beach environment.
8. Do **not** make the camera freely rotatable.
9. Design around two controlled camera sides:
   - **Camera A:** start-side view
   - **Camera B:** opposite-side view
10. Keep the center/blend corridor visually safe from both cameras.
11. Distant scenery should be designed specifically for the controlled camera views instead of being fully modeled in 360 degrees.
12. Prefer:
   - baked lighting
   - light probes
   - GPU instancing
   - texture atlases
   - shared materials
   - modular reusable props
   - impostors/billboards for distance
13. Avoid:
   - unnecessary colliders
   - rigidbodies on decorative assets
   - dynamic lights everywhere
   - expensive transparency layers
   - dense unique meshes
   - cloth/hair simulation
   - full real-time shadows on distant objects
14. Keep implementation clean and reversible.
15. After each phase:
   - run the project
   - verify current map still works
   - verify Map 02 loads
   - capture screenshots from Camera A and Camera B
   - record any errors/warnings introduced
   - do not proceed by hiding errors.

---

# Target Visual Direction

Map 02 should feel like a premium stylized/semi-realistic coastal arena at **early sunset / evening**.

The sunset warmth must be **controlled**, not fully orange.

Target balance:

- approximately 50% of the extreme warm/orange treatment from the earlier sunset preview
- natural beige/golden sand
- blue/cyan ocean retained
- green vegetation retained
- warm sun highlights
- soft amber lanterns
- slightly cooler shadows
- cinematic atmospheric depth

The visual priority order is:

1. Composition / camera framing
2. Lighting
3. Sky + ocean + distant scenic layers
4. Playable lane presentation
5. Hero props
6. Vegetation and environmental dressing
7. Minor decorative details

---

# Asset Complexity Strategy

## High-complexity / hero assets

These receive the most modeling/material effort:

- playable sand lane
- side boundary system
- three pit visuals
- one hero coastal hut / shack
- 3–5 hero palm trees
- near shoreline / visible ocean edge
- near coastal rocks
- sunset lighting and exposure setup
- selected foreground framing foliage

## Medium-complexity assets

These should be modular and reusable:

- fences / rope railings
- dock / jetty pieces
- small fishing boat
- surfboards
- barrels
- crates
- clay pots
- baskets
- lanterns / torches
- vegetation clusters
- small rocks
- logs / driftwood
- benches / simple seating
- modular wooden posts

## Low-complexity assets

Use very cheap geometry, decals, cards, or instancing:

- shells
- starfish
- small pebbles
- footprints
- sand marks
- flowers
- grass tufts
- small debris
- signs
- rope segments

## 2.5D / impostor / background assets

Do not make these expensive full 3D unless Camera A/B proves it necessary:

- distant island
- lighthouse island
- far cliffs
- distant palm forest
- far huts/buildings
- far boats
- mountain silhouettes
- horizon
- cloud layers
- distant coastline

---

# PHASE 0 — Audit, Safety, and Map-Slot Integration

## Prompt

You are working on the existing **Pit Striker** Unity project.

Before implementing Map 02, inspect the project and identify the current map architecture.

### Objectives

1. Find:
   - current map scenes
   - map-slot definitions
   - map loader / map manager
   - scene loading flow
   - spawn point definitions
   - pit slot / pit references
   - camera controllers
   - gameplay root objects
   - shared gameplay prefabs
2. Determine exactly how a new map should be registered without modifying the current map.
3. Identify which objects belong to:
   - core gameplay
   - current environment only
   - shared UI
   - shared cameras
4. Document dependencies that Map 02 must provide.
5. Add a safe placeholder entry for **Map 02 — Sunset Coastal** only if the existing architecture makes this safe.

### Strict restrictions

- Do not delete or rename the existing map.
- Do not change marble physics.
- Do not change strike behavior.
- Do not change multiplayer logic.
- Do not refactor unrelated systems.
- Do not replace the existing map loader.
- Do not create a separate repository.

### Deliverables

Create/update project documentation containing:

- existing map architecture summary
- exact Map 02 integration points
- required scene/prefab contract
- reusable gameplay objects
- current-map-only objects
- risks/dependencies
- proposed file/folder locations for Map 02

### Acceptance criteria

- Current map still loads and plays exactly as before.
- No new Unity console errors.
- Map 02 can exist as an additional slot without breaking current map selection.
- No gameplay rewrite has been performed.

---

# PHASE 1 — Visual Blockout and Camera Composition

## Prompt

Implement the **visual blockout** for **Map 02 — Sunset Coastal**.

This phase is about **framing and artwork**, not gameplay polish.

### Build only the minimum geometry required to judge the final composition

Create:

- long central playable sand lane
- three pit placeholders in a straight line
- simple side boundaries
- basic near shoreline indication
- basic hut placeholder
- basic palm placeholders
- basic rocks/vegetation masses
- distant island/lighthouse placeholder
- ocean/horizon placeholder

### Camera A

Create or configure the start-side camera.

Target:

- vertical/mobile-friendly composition
- long readable lane
- marble/gameplay region clearly visible
- rich coastal scenery framing the lane
- elevated three-quarter/isometric-like presentation
- no free rotation

Suggested starting range:

- downward tilt: approximately 45–55 degrees
- use perspective, not orthographic, unless the existing camera system strongly favors otherwise
- maintain enough perspective depth to preserve the premium 3D feel

### Camera B

Create an opposite-side camera anchor.

Do **not** make it a mechanically exact mirror.

Target:

- opposite-side gameplay readability
- slightly tighter framing if needed
- slightly higher angle if it improves visibility
- preserve the scenic composition
- ensure the central lane and blend corridor look valid from both sides

### Do not yet implement

- detailed cinematic transitions
- character avatars
- advanced gameplay obstacles
- final physics tuning
- final materials
- full environment details

### Deliverables

- Map 02 blockout scene/prefab
- Camera A
- Camera B
- screenshots from both cameras
- notes on visibility problems and objects that require true 3D

### Acceptance criteria

- The map clearly reads as the approved long Sunset Coastal arena.
- All 3 pits are readable.
- Camera A and Camera B both work visually.
- The environment does not require a full 360-degree world.
- Existing gameplay systems remain untouched.

---

# PHASE 2 — Core Artwork: High-Complexity Assets

## Prompt

Upgrade the Map 02 blockout into the first serious visual-quality pass.

This phase must prioritize the **approved artwork feel**.

### Build/refine the following true-3D hero assets

1. Playable sand lane
   - premium stylized/semi-realistic sand
   - subtle roughness variation
   - small surface variation
   - avoid overly noisy texture
   - ensure mobile-friendly shader complexity

2. Side boundary system
   - modular logs / low stone / rope elements as appropriate
   - visually rich but reusable
   - no unnecessary physics components

3. Three pit visuals
   - small and clean
   - visually integrated with sand
   - exact gameplay collider tuning can remain for later
   - use the existing pit logic/reference where possible

4. Hero coastal hut
   - optimize only the sides visible to Camera A/B
   - no detailed interior unless visible
   - shared materials where possible

5. Hero palm set
   - create/reuse approximately 3–5 hero variants
   - lightweight wind animation or vertex animation
   - avoid expensive per-tree animation systems

6. Near coastal rocks
   - modular set
   - rotate/scale variants rather than many unique meshes

7. Near ocean/shoreline visual
   - mobile-friendly water
   - cinematic reflection without expensive full-scene simulation

### Lighting direction

Target early sunset, not extreme orange.

Use:

- warm sun key light
- cooler/neutral shadow balance
- controlled bloom
- atmospheric depth
- retained ocean blues
- retained vegetation greens
- natural sand color
- mild golden highlights

### Optimization rules

- bake what can be baked
- use probes where needed
- minimize real-time shadow casters
- atlas materials where reasonable
- avoid duplicate large textures

### Deliverables

- high-complexity asset pass
- lighting pass v1
- Camera A screenshot
- Camera B screenshot
- performance notes

### Acceptance criteria

- The map already communicates the target visual identity before small props are added.
- Sunset warmth is approximately half of the overly warm reference.
- Lane remains readable.
- Scene still performs acceptably on the target mobile rendering path.
- No unrelated gameplay changes.

---

# PHASE 3 — Scenic World Illusion: 2.5D and Low-Cost Backgrounds

## Prompt

Create the premium surrounding coastal world **without** building a full expensive 3D environment.

The goal is to achieve approximately **90–95% of the desired scenic visual effect** from the controlled cameras while minimizing geometry and runtime complexity.

### Build far scenery primarily using

- impostors
- camera-facing cards
- layered scenic planes
- extremely low-poly silhouettes
- baked textures
- skybox/HDRI/procedural sky where suitable

### Required scenic layers

1. ocean horizon
2. distant island
3. lighthouse island
4. distant mountain/cliff silhouettes
5. far palm forest
6. far coastal huts/buildings
7. far boats where visually useful
8. sky/cloud layers
9. atmospheric haze

### Camera-specific optimization

Because Camera A and Camera B are controlled:

- scenery may use different optimized arrangements for each side
- do not expose fake scenery during the A↔B transition
- ensure the center/blend corridor has sufficient genuine 3D coverage
- do not build unseen backsides of distant assets

### Blend-corridor rule

The area visible during camera transition must contain mostly:

- playable lane
- boundaries
- selected rocks
- selected vegetation
- limited hero palms/props

Avoid placing expensive unique hero geometry throughout the whole transition.

### Deliverables

- complete far-scenery setup
- Camera A scenic composition
- Camera B scenic composition
- transition-safe scenic layout
- list of fake/2.5D assets vs true-3D assets

### Acceptance criteria

- From Camera A/B, the environment feels rich and deep.
- Far scenery does not obviously appear flat during normal play.
- No full open-world geometry has been built.
- Performance and memory remain substantially lower than an all-3D equivalent.

---

# PHASE 4 — Medium/Low Complexity Props and Environmental Dressing

## Prompt

Dress Map 02 with reusable medium- and low-complexity assets.

The purpose is to increase visual richness without increasing scene cost excessively.

### Add selectively

- fence/rope modules
- barrels
- crates
- clay pots
- baskets
- surfboards
- bench/seating
- small dock/jetty pieces
- one small fishing boat if it improves framing
- lanterns/torches
- driftwood
- pebbles
- shells
- starfish
- footprints
- grass tufts
- flowers
- small beach plants
- sign boards
- small debris

### Placement rules

- prioritize the edge of the frame
- use foreground foliage to create depth
- do not clutter the center gameplay lane
- preserve clear visibility of all three pits
- avoid repeating identical props obviously
- use rotation/scale/material variation instead of many unique models

### Lighting props

Lanterns/torches should mainly use:

- emissive materials
- baked contribution
- very limited real-time lights

### Deliverables

- dressing pass
- modular prop prefabs
- Camera A/B screenshots
- object/material count notes

### Acceptance criteria

- Scene feels rich and authored.
- Gameplay lane remains visually clean.
- Props are reusable for future maps where appropriate.
- No significant performance regression.

---

# PHASE 5 — Camera A ↔ Camera B Cinematic System

## Prompt

Implement the controlled cinematic camera system for Map 02 while preserving all existing gameplay physics.

### Camera states

Use one rendered main camera driven by virtual cameras/anchors, not two full-time rendering cameras.

Provide:

- Camera A — start side
- Camera B — opposite side
- temporary Follow/Blend state
- optional Aim state if the current project architecture supports it cleanly

### Behavior

Target flow:

1. player aims from the active side
2. strike/toss occurs
3. camera keeps marble readable
4. when marble reaches the transition region, start smooth blend
5. camera slightly zooms/follows if useful
6. settle to the opposite-side camera when appropriate
7. never change marble physics because of camera movement

### Important requirements

- avoid sudden 180-degree hard cuts during active aiming
- prevent control inversion
- aiming must remain camera-relative if required
- preserve the current strike force/physics system
- transition must not reveal missing environment geometry
- camera movement should feel cinematic, not like a free camera

### Triggering

Use robust conditions such as:

- marble position relative to map midpoint
- active gameplay segment
- marble motion state

Do not switch simply on a timer.

### Deliverables

- working A/B cinematic camera system
- configurable blend duration
- configurable zoom/follow parameters
- map-specific camera anchors
- transition trigger debug visualization
- before/after screenshots or short capture if project tooling allows

### Acceptance criteria

- marble remains easy to track
- camera transition is smooth
- environment illusion remains intact
- controls remain intuitive
- no physics changes
- no duplicate full-time camera rendering cost

---

# PHASE 6 — Visual Polish, Sunset Finalization, and Mobile Optimization

## Prompt

Finalize the Map 02 visual presentation and optimize it for the target mobile build.

### Final visual polish

Tune:

- sunset intensity
- exposure
- sky color
- ocean color
- sand color
- lantern warmth
- shadow temperature
- bloom
- atmospheric haze
- subtle depth-of-field only where appropriate
- foreground framing
- composition from Camera A/B

### Sunset target

Avoid excessive orange wash.

Maintain:

- recognizably sunset/evening mood
- blue/cyan water
- green vegetation
- warm sun highlights
- natural beige/golden sand
- readable marble/pits
- warm but not orange gameplay lane

### Mobile optimization

Review:

- draw calls
- batches
- active lights
- real-time shadow casters
- transparent materials
- texture sizes
- duplicated materials
- mesh complexity
- overdraw
- particles
- far-scene geometry
- LOD/impostor switching
- memory usage

Use platform-appropriate optimization while preserving the approved visual frame.

### Do not optimize by destroying the artwork

The priority is:

1. preserve the approved Camera A/B visual result
2. remove hidden/unnecessary cost
3. simplify distant/unimportant elements
4. only reduce hero visual quality as a last resort

### Deliverables

- final visual pass
- optimization report
- Camera A/B final screenshots
- list of remaining expensive assets
- recommended quality tiers if needed

### Acceptance criteria

- Map 02 matches the approved art direction closely.
- Both cameras look intentionally composed.
- No obvious fake/background failures.
- Current/reference map still works.
- No new critical errors.
- Performance is acceptable for the intended mobile target.

---

# PHASE 7 — Gameplay Integration and Map Finalization (Do Only After Visual Approval)

## Prompt

Only begin this phase after the user approves the Map 02 visual direction.

Now integrate and validate the existing gameplay inside the finalized Sunset Coastal map.

### Reuse existing systems

Preserve:

- marble physics
- strike/toss behavior
- power calculation
- pit detection
- turns
- scoring
- multiplayer
- existing UI

### Validate

- player/marble spawn
- three pit positions
- gameplay bounds
- side borders
- obstacle collision
- camera-relative aiming
- Camera A/B switching
- shot readability
- pit entry
- marble-to-marble collisions
- resets
- multiplayer synchronization

### Small obstacles

Only add gameplay obstacles after visual approval.

Use a few environment-suitable obstacles such as:

- small rock
- wooden stump
- small sand mound
- driftwood

Keep them tactical, not mini-golf-like.

### Do not

- rewrite the core game
- rebalance physics without evidence
- change current map behavior
- delete the reference map yet

### Acceptance criteria

- same core gameplay feel as existing map
- no regression in physics/turn logic
- Map 02 is fully playable
- Camera A/B improves readability instead of harming it

---

# PHASE 8 — Optional Avatar Presentation Layer

> Do not implement unless the user explicitly approves avatars after reviewing the environment.

## Prompt

Add optional 3D player avatars as a presentation layer only.

### Rules

- avatars do not participate in gameplay physics
- no Rigidbody interaction with marble
- no free-roaming AI required
- use predefined anchor points beside the lane
- active player may move to the nearest valid anchor only between safe gameplay states
- inactive players remain in idle/reaction states

### Suggested animations

- idle
- walk
- ready
- toss/strike
- watch
- celebrate
- disappointed

### Performance

- optimized stylized models
- simple rigs
- no cloth simulation
- no advanced hair physics
- minimal materials
- reduced shadows for inactive players

### Acceptance criteria

- avatars improve presentation
- gameplay remains untouched
- no meaningful performance regression
- avatar movement never blocks the lane or interferes with physics

---

# Final Migration Rule

Do **not** remove the current environment immediately after Map 02 works.

Only retire the old map after:

- Map 02 visual approval
- gameplay parity testing
- multiplayer testing
- mobile performance validation
- final user approval

Recommended migration:

1. Keep existing map as reference.
2. Add Map 02 as separate slot.
3. Complete all visual phases.
4. Complete gameplay validation.
5. Make Map 02 the preferred/default map if approved.
6. Mark old environment as deprecated/reference.
7. Remove unused heavy assets only after confirming no remaining dependencies.

---

# Recommended Implementation Order

```text
Phase 0  Audit / integration contract
   ↓
Phase 1  Blockout + Camera A/B composition
   ↓
Phase 2  Hero 3D assets + lighting
   ↓
Phase 3  2.5D scenic world illusion
   ↓
Phase 4  Props + environmental dressing
   ↓
Phase 5  Cinematic camera blend
   ↓
Phase 6  Visual polish + optimization
   ↓
USER VISUAL APPROVAL
   ↓
Phase 7  Gameplay integration / validation
   ↓
Phase 8  Optional avatars
```

## Core principle

**Spend real 3D complexity only where the player can inspect it or gameplay needs it.  
Use camera-controlled scenic illusion everywhere else.  
Preserve the artwork first; optimize intelligently rather than flattening the visual quality.**
