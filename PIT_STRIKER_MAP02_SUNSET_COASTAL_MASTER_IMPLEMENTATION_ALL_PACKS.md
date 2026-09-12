# Pit Striker — Map 02 "Sunset Coastal"
## Master Implementation Prompt — Combined Pack 01–05

> **Primary goal:** Implement `Map 02 — Sunset Coastal` as a new map slot in the existing Pit Striker Unity project.
>
> **Highest priority:** Match the approved artwork, framing, sunset-coastal atmosphere, and overall visual effect before doing deeper gameplay tuning.
>
> **Do not remove the current map yet.** Keep it as the reference/fallback until Map 02 passes visual, gameplay, multiplayer, and mobile-performance validation.

---

# REQUIRED ASSET SOURCE

Use this combined ZIP as the single source package:

`PitStriker_Map02_SunsetCoastal_AllPacks_v1.zip`

After extraction, use this structure:

```text
Packs/
├── Pack01_Starter_Impostors_And_Props/
├── Pack02_Core_Environment_Assets/
├── Pack03_Props_And_Scenery/
├── Pack04_Materials_And_Decals/
└── Pack05_Expanded_Impostors/
```

## Mandatory first action

Before creating or regenerating any Map 02 asset:

1. Extract the combined ZIP.
2. Read `Docs/PACK_INDEX.md`.
3. Inspect all five pack folders.
4. Import/copy the extracted source assets into the project’s Map 02 art/source area.
5. Keep the current production/reference map unchanged.
6. Reuse the supplied assets first.
7. Create a new asset only when:
   - no suitable source already exists, or
   - the supplied source fails the technical/visual requirement.

---

# IMPORTANT ASSET STATUS

## Pack 01
Contains ready-to-use scenic impostors plus source prop PNGs.

## Pack 02
Contains **transparent source/reference PNG assets** for core environment assets.

These are not yet final Unity-ready GLB/GLTF models.

Use them as:
- visual references,
- temporary 2.5D cards,
- image-to-3D source inputs,
- Blender modeling references.

Depth-critical assets should later become real 3D.

## Pack 03
Contains **transparent source/reference PNG assets** for reusable props and scenic dressing.

Use the same rule as Pack 02:
- keep as 2.5D where depth is not exposed,
- convert to real 3D only where Camera A/B or gameplay requires it.

## Pack 04
Contains generated material maps and transparent decals.

Use these directly as material starting points in Unity and tune them under final map lighting.

## Pack 05
Contains ready-to-use low-cost scenic layers / impostors.

These should stay lightweight wherever possible.

---

# CORE IMPLEMENTATION PRINCIPLE

The map is not a full open-world beach.

Use:

### True 3D
- gameplay lane
- pits
- marble
- gameplay-relevant obstacles
- lane borders
- near-camera depth-critical props
- selected hero assets
- avatars later if approved

### Lightweight 3D
- nearby hut
- selected palms
- selected rocks
- dock edge
- boat if close to camera
- near fence/rope elements

### 2.5D / impostor / scenic cards
- distant islands
- lighthouse island
- far palm forests
- far huts
- far boats
- mountains
- cliffs
- horizon vegetation
- cloud layers
- atmospheric haze

---

# PHASE 0 — Architecture Audit

Inspect the existing Unity project and identify:

- current map slots
- map loader / map manager
- scene loading
- gameplay root
- spawn points
- pit references
- camera system
- existing marble prefab
- existing physics material
- existing strike/toss system
- multiplayer/network dependencies
- UI dependencies

Then create/register Map 02 as a **new map slot** without replacing the current map.

Do not rewrite unrelated systems.

### Acceptance
- Existing map still works.
- Map 02 can load independently.
- No new critical console errors.
- No core physics rewrite.

---

# PHASE 1 — Visual Blockout + Camera A/B

Use Pack 01 and Pack 05 immediately.

Create:

- long gameplay strip
- three pits in a straight line
- simple lane boundaries
- basic coastal framing
- Camera A
- Camera B
- transition-safe middle corridor

### Camera target
Use a controlled elevated perspective/isometric-like view.

Suggested starting point:
- 45–55° downward tilt
- perspective projection
- no free camera rotation
- vertical/mobile-friendly composition

Camera B should not be a mechanically perfect mirror if a slightly different angle improves the artwork.

### Acceptance
- Both cameras show the gameplay lane clearly.
- Scenic background already feels rich.
- Flat impostors are not obvious.

---

# PHASE 2 — Hero 3D Conversion / Modeling

Use Pack 02 as the primary source/reference set.

Prioritize conversion/modeling in this order:

1. `HeroCoastalRock_Set_01`
2. `BoundaryLog_Module_01`
3. `WoodenPost_Rope_01`
4. `BeachShack_Hero_01`
5. `SmallFishingBoat_01`
6. Hero palm(s) only where depth requires it
7. Lantern/torch elements only where close enough to justify it

For each real-3D conversion:
- clean topology
- correct scale
- correct pivot
- sensible UVs
- mobile-friendly polygon count
- shared materials where possible
- simple collider only where needed
- no unnecessary rigidbody
- LOD only if justified

Do not convert everything simply because 3D is possible.

---

# PHASE 3 — Props & Scenic Dressing

Use Pack 03.

Place and evaluate:
- fence variation
- surfboards
- dock module
- crates
- signage
- lanterns
- foliage

Convert only depth-critical props to true 3D.

Keep peripheral decorative elements 2.5D where possible.

Do not clutter the gameplay strip.

---

# PHASE 4 — Materials & Decals

Use Pack 04.

## Material sets
- Sand_Dry
- Sand_Packed
- Sand_Wet
- Wood_Weathered
- Rock_Coastal
- Rope_Coastal

Each contains:
- Albedo
- Height
- Normal
- Roughness

### Unity notes
- Albedo → Base Map
- Normal → import as Normal Map
- Roughness may need inversion if the shader expects Smoothness
- Height is optional; avoid expensive parallax everywhere
- use 1024 on mobile unless hero testing proves 2048 is necessary

## Decals
Use:
- Footprints
- Shells / Starfish
- Sand Scratches
- Pit Edge Wear
- Moss
- Shoreline Foam

Keep decals subtle.

---

# PHASE 5 — Scenic Illusion / Impostor Assembly

Use Pack 05 as the main distant-scenery source.

Assemble:
- lighthouse island
- far palm forests
- foreground foliage framing
- mountains
- distant huts
- far boats
- clouds
- haze
- cliffs
- island vegetation

Rules:
- no colliders
- no rigidbodies
- no real-time shadows for distant cards
- simple shader
- use batching / atlasing when practical
- keep cards far enough that flatness is not visible

Test from:
- Camera A
- Camera B
- Camera A↔B blend

---

# PHASE 6 — Sunset Lighting & Final Artwork

Visual target:
- evening / early sunset
- not excessively orange
- keep ocean blue/cyan
- keep vegetation green
- natural beige/golden sand
- controlled amber highlights
- slightly cooler shadows
- subtle haze
- controlled bloom

Priority:
1. composition
2. lighting
3. scenic depth
4. material response
5. prop dressing
6. small details

Do not reduce artwork quality merely to optimize something that can instead be hidden, baked, instanced, or turned into an impostor.

---

# PHASE 7 — Cinematic Camera System

Use:
- one rendered Main Camera
- Camera A virtual/anchor
- Camera B virtual/anchor
- temporary follow/blend state

Target flow:

```text
Aim
→ Strike
→ Marble moves
→ Camera keeps marble readable
→ Mid-map transition begins
→ Smooth A→B blend
→ Slight follow/zoom if useful
→ Settle into B
```

Do not switch while the player is actively aiming.

Aiming must remain camera-relative if the active camera changes world orientation.

Camera changes must never alter physics.

---

# PHASE 8 — Gameplay Validation

Only after the visual result is approved:

Validate:
- existing marble physics
- strike force
- power system
- pit detection
- turn system
- scoring
- multiplayer
- bounds
- reset logic
- Camera A/B
- camera-relative aim

The goal is to preserve existing gameplay feel.

---

# PHASE 9 — Optional Avatars

Do only if explicitly approved later.

Use 3D avatars as presentation only:
- no gameplay physics
- no collision with marble
- anchor-based movement
- idle/walk/ready/strike/watch/reaction animations

Do not add open-world character AI.

---

# FINAL MIGRATION

Do not delete the old map immediately.

Required sequence:

1. Map 02 visual approval
2. Gameplay parity
3. Multiplayer test
4. Mobile performance test
5. Make Map 02 default/preferred if approved
6. Mark previous map deprecated/reference
7. Remove old heavy assets only after confirming no dependencies remain

---

# SOURCE-OF-TRUTH RULE

The extracted combined Pack 01–05 ZIP is the first asset source for Map 02.

Before making a new asset:
1. search the combined pack,
2. reuse an existing asset if suitable,
3. adapt/convert it only when required,
4. generate/model something new only if the combined pack cannot satisfy the need.
