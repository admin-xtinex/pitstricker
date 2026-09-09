# Pit Striker — Beach Map + Gameplay Kit (Blender Brief)

**Scope:** Beach map only + gameplay kit. Village deferred.
**Unity match:** sizes from `SandboxBuilder.cs` (do not freestyle scale).
**Style:** Stylized-premium mobile (see Art Style Guide). Concepts: beach mockup + village reference (village for mood only).

---

## 1. Unity scale lock (metres, Y-up)

| Item | Value |
|---|---|
| Marble diameter | **0.50 m** (Unity sphere scale 0.5) |
| Pit rim diameter | **1.00 m** (rim radius 0.50) |
| Pit floor radius | **0.38 m** |
| Pit depth | **~0.28 m** |
| Pit centres (Z) | **3.0 / 16.5 / 31.0** |
| Launch chalk centre | **Z = -6.0**, ring radius **1.0 m** |
| Play strip width | **~2.8 m** |
| Outer banks / walls | ~**16 m** wide playfield, ~**48 m** long fairway |
| Marble colours | Blue, Red, Green, Amber |

**Export:** FBX, Apply Transform, Forward **-Z**, Up **Y**, metres, scale **1.0**.
**Pivot rules:** marble = centre; pit = rim centre at ground Y=0; props = ground contact.

---

## 2. Deliverables checklist

### A) Gameplay kit (ship first)

- [ ] `SM_Marble_Glass` base mesh (~250–400 tris)
- [ ] 4 materials / swirl variants: Blue, Red, Green, Amber
- [ ] `SM_Pit_Beach` visual bowl (rim + basin; **no collider** — Unity keeps `PitZone`)
- [ ] `SM_PitFlag_01` / `_02` / `_03` (red flag + thin pole + number)
- [ ] `SM_ChalkRing` (dashed launch circle) — optional if Unity LineRenderer stays
- [ ] Aim chevrons — **skip in Blender** (Unity trajectory line already works)

### B) Beach map (blockout → polish)

**Must**
- [ ] `SM_Beach_Terrain` — sand path with 3 pit holes cut at correct Z
- [ ] Soft sand banks left/right (visual; physics can stay Unity slabs for now)
- [ ] Driftwood / log boundary rails (replace grey walls)
- [ ] Ocean plane / card at far end (shader later in Unity)
- [ ] Sky props: palms (LOD), stilt hut, fishing boat, net

**Nice**
- [ ] Distant island / lighthouse card
- [ ] Small rocks, shells, beach grass clumps (atlas)
- [ ] Soft foam strip where sand meets water

**Not in Blender**
- HUD, logo, power bar, shoot button, settings gear
- Final glass transmission shader (URP Lit high smoothness first)
- Map-specific friction tuning

---

## 3. Folder layout

```
Art/3DSource/
  GameplayKit/     # .blend for marbles, pits, flags
  Maps/Beach/      # .blend for beach scene
  Exports/FBX/
  Exports/Textures/
```

Suggested Blender files:
- `PS_GameplayKit.blend`
- `PS_Map_Beach.blend`

Suggested FBX names:
- `SM_Marble_Glass.fbx`
- `SM_Pit_Beach.fbx`
- `SM_PitFlag_01.fbx` … `_03.fbx`
- `SM_Beach_Terrain.fbx`
- `SM_Beach_Props.fbx` (hut, boat, palms grouped or separate)

Unity import target (later):
`pitstricker/Assets/_Project/Art/`

---

## 4. Poly / mobile budgets

| Asset | Tris target |
|---|---|
| Marble | 250–400 |
| Pit bowl | &lt; 800 |
| Flag | &lt; 200 |
| Beach terrain (playable strip) | &lt; 3–5k |
| Hero prop (hut / boat) | &lt; 1.5k each |
| Palm (near) | &lt; 800; far LOD &lt; 200 |
| Whole beach scene on screen | keep draw calls lean; atlas props |

---

## 5. Session plan

### Session 1 — Gameplay kit blockout
1. New file `PS_GameplayKit.blend`, units = Metres.
2. Add UV sphere / icosphere → diameter **0.5 m**.
3. Duplicate ×4, name by colour; assign bright temp materials.
4. Model pit bowl: rim R=0.5, floor R=0.38, depth 0.28.
5. Simple flag poles at rim +0.35 m offset (match greybox markers).
6. Place 3 pits at Z 3 / 16.5 / 31 and 4 marbles at launch Z -6 to verify spacing.
7. Save. Do **not** UV/texture yet if blockout looks right.

### Session 2 — Export kit → Unity smoke test
1. Export marble + pit + flags FBX.
2. Drop into sandbox scene; keep existing Rigidbody / PitZone / colliders.
3. Confirm roll into pit, scale, camera framing.

### Session 3 — Beach terrain blockout
1. New `PS_Map_Beach.blend`.
2. Build sand strip ~2.8 m wide, ~48 m long; cut 3 holes.
3. Add rough banks, ocean plane, placeholder hut/boat/palms.
4. Align to same pit Z coords.
5. Export terrain + props; swap greybox ground visuals only.

### Session 4 — Materials + LODs
1. Sand albedo/normal (tiled).
2. Marble swirl textures / URP Lit smoothness 0.85–0.95.
3. Palm/hut LODs; bake atlases for clutter.

---

## 6. Concept pull list (from beach mockup)

| Seen in concept | Blender? |
|---|---|
| Glass marbles ×4 colours | Yes — kit |
| 3 sand pits | Yes — kit + terrain holes |
| Pit labels "Pit 1/2/3" | Flags in 3D; floating text can stay Unity |
| Dashed aim arrow | Unity only |
| Selection ring | Unity chalk / UI |
| Palms, stilt hut, boat, net | Yes — beach props |
| Ocean + island/lighthouse | Yes — simple cards OK |
| HUD / logo / power / shoot | No — UI later |

---

## 7. Done when

- [ ] Kit FBXs in Unity, scale correct, physics still works
- [ ] Beach terrain replaces grey sand look in one playable scene
- [ ] Props dress the sides without blocking shots on the 2.8 m strip
- [ ] Village assets **not** started yet