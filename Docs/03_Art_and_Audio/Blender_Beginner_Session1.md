# Pit Striker — Blender Beginner Guide (Session 1)

Follow in order. One step at a time. If something looks wrong, stop and ask before continuing.

**Goal today:** one `.blend` file with 4 coloured marbles (0.5 m), 3 pit bowls, 3 flags, placed at Unity spacing.

**File to save:** `Art/3DSource/GameplayKit/PS_GameplayKit.blend`

---

## Before you start

1. Install Blender 4.x from https://www.blender.org/download/ if needed.
2. Open Blender.
3. On the splash screen, click **General** (new scene).
4. You should see a default cube in the middle.

### Mouse basics (Blender)

| Action | How |
|---|---|
| Orbit view | Middle-mouse drag |
| Pan | Shift + Middle-mouse drag |
| Zoom | Scroll wheel |
| Select | Left-click |
| Confirm | Left-click or Enter |
| Cancel | Right-click or Esc |
| Undo | Ctrl + Z |

If you have a laptop trackpad only: Edit → Preferences → Input → enable **Emulate 3 Button Mouse**, then Alt + Left-drag = orbit.

---

## Part A — Save the project + metres

1. File → Save As…
2. Go to: `C:\Users\Tisan\Documents\pitstricker\Art\3DSource\GameplayKit\`
3. Name: `PS_GameplayKit.blend`
4. Click Save As.
5. Top menu: Scene Properties (icon looks like a cone/sphere stack on the right properties panel — or press the Scene tab).
6. Open **Units**.
7. Unit System: **Metric**.
8. Unit Scale: **1.0000**.
9. Length: **Meters**.
10. Ctrl + S to save again.

---

## Part B — Delete the default cube

1. Left-click the cube so it is outlined orange/yellow.
2. Press **X** (or Delete).
3. Click **Delete**.
4. Only the camera and light should remain (fine for now).

---

## Part C — Make one marble (0.5 m diameter)

1. Press **Shift + A**.
2. Mesh → **UV Sphere**.
3. Bottom-left, a small panel “Add UV Sphere” appears. Click it to expand if collapsed.
4. Set:
   - Segments: **32**
   - Rings: **16**
5. The sphere is **2 m** diameter by default in Blender (radius 1). We need **0.5 m** diameter.
6. With the sphere selected, press **S** (scale), type **0.25**, press **Enter**.
   - Why 0.25? Default diameter 2 × 0.25 = **0.5 m**. Good.
7. Press **N** to open the side panel (if not open).
8. Item tab → Dimensions should read roughly **X 0.5, Y 0.5, Z 0.5**.
9. If not, set Scale X/Y/Z back and use Dimensions fields carefully, or undo and repeat step 6.
10. Important: with sphere selected, press **Ctrl + A** → **Scale** (Apply Scale). Dimensions stay 0.5; scale becomes 1,1,1.
11. Bottom-left Outliner: rename `Sphere` to `SM_Marble_Blue`.
    - Double-click the name, type, Enter.
12. Ctrl + S.

### Move marble to launch position (Unity match)

1. Select `SM_Marble_Blue`.
2. Press **G** then **Z**, type **0.25**, Enter (lift so it sits on the ground; centre was at 0, radius 0.25).
3. Press **G** then **Y**, type **-6**, Enter.
   - Blender’s Y often maps to Unity’s Z on FBX export with correct settings — for **layout check inside Blender** we will use:
   - Put pits along **+Y** in Blender for this file (we’ll document export axis later).
   - Launch at Y = **-6**, pits at Y = **3 / 16.5 / 31**.
4. Location should be about: X 0, Y -6, Z 0.25.
5. Ctrl + S.

---

## Part D — Colour the blue marble (temporary)

1. Select `SM_Marble_Blue`.
2. Properties panel → Material Properties (red checker / sphere icon).
3. Click **New**.
4. Name the material `M_Marble_Blue`.
5. Base Color: pick a bright blue.
6. Roughness: slide down to about **0.1** (shinier).
7. Ctrl + S.

---

## Part E — Make Red, Green, Amber marbles

1. Select `SM_Marble_Blue`.
2. Press **Shift + D** (duplicate), then press **X**, type **-0.6**, Enter.
3. Rename to `SM_Marble_Red`.
4. Material Properties → click the material name dropdown → **Add Material** slot or click the **2** (user count) to make it unique → rename `M_Marble_Red` → red colour.
   - Easier path: click the small **number** next to material name (if shown) to “make single-user”, then rename + recolour.
   - Or: New material `M_Marble_Red`, assign red.
5. Duplicate again from blue (or from red):
   - Green at X **+0.2** relative or place at X = **0.2**, Y = -6, Z = 0.25 → `SM_Marble_Green`
   - Amber at X = **0.6** → `SM_Marble_Amber`
6. Unity greybox X offsets were: -0.6, -0.2, 0.2, 0.6. Match those.
7. Each gets its own material colour.
8. Ctrl + S.

You should see 4 shiny coloured spheres in a row near Y = -6.

---

## Part F — Model one pit bowl

We build a simple bowl: outer rim radius 0.5 m, inner floor radius 0.38 m, depth 0.28 m.

1. Press **Shift + A** → Mesh → **Cylinder**.
2. Expand Add Cylinder settings:
   - Vertices: **32**
   - Radius: **0.50**
   - Depth: **0.05** (thin disc for the rim start)
3. Rename to `SM_Pit_Beach`.
4. Move to first pit: Location X 0, Y **3**, Z 0.
5. Tab → **Edit Mode**.
6. Press **A** (select all).
7. Press **S** then **Shift + Z** (scale in X/Y only) — leave for now; radius already 0.5.
8. Easier beginner bowl method (recommended):

### Beginner pit method (Boolean-friendly)

1. Tab back to **Object Mode** if needed.
2. Delete `SM_Pit_Beach` if it feels messy (X → Delete). We’ll rebuild cleanly:

**Step F1 — Outer sand collar (optional later). For now: the cup only.**

1. Shift + A → Mesh → **UV Sphere**.
2. Rename `SM_Pit_Beach`.
3. S → type **0.5** → Enter (sphere diameter becomes 1.0 m — matches pit rim).
4. Ctrl + A → Apply Scale.
5. Tab → Edit Mode.
6. Numpad **1** (front view) or View → Viewpoint → Front.
7. Press **Alt + Z** for X-ray (optional).
8. Press **B** (box select) and select the **top half** vertices (everything above the equator).
9. Press **X** → **Vertices**.
10. You now have a hemisphere bowl opening upward… wait: we need opening **up**. Default sphere cut may open wrong way.
    - If the bowl opens downward, select all (A) → R X 180 → Enter.
11. Select the top rim circle (Alt + click an edge loop on the rim).
12. Press **S** to ensure rim radius is **0.5** (check Item dimensions later in Object Mode).
13. Select the bottom-ish area and move down so depth is about **0.28 m** from rim to bottom.
    - Practical: with bowl selected in Object Mode, rim at Z=0, lowest point around Z= **-0.28**.

**Cleaner beginner alternative (Cylinder + inset):**

1. Shift + A → Cylinder, Vertices 32, Radius **0.50**, Depth **0.28**.
2. Location: X0 Y3 Z **-0.14** (so top rim sits at Z=0).
3. Tab Edit Mode → face select (press **3**).
4. Select **top** face → press **I** (inset) → type **0.12** → Enter  
   (0.50 − 0.38 = 0.12 inset ≈ floor radius 0.38).
5. With inset face still selected, press **S** slightly if needed so inner radius ≈ 0.38.
6. Press **G** then **Z**, type **-0.02** or so to push floor down a little for a flatter basin (optional).
7. Delete the **bottom** outer face if you want an open underside (for a cup): select bottom face → X → Faces. For a solid pit visual, keep it.
8. Tab Object Mode → Ctrl + A → Rotation & Scale.
9. Ctrl + S.

Place duplicates:
1. Select `SM_Pit_Beach` → Shift + D → Y → type **13.5** → Enter (lands near Y 16.5). Rename `SM_Pit_Beach_02` or keep linked and rename.
2. Another duplicate to Y **31**.
3. Or set Location Y manually: 3, 16.5, 31.

---

## Part G — Flags

1. Shift + A → Mesh → **Cylinder**, Radius **0.02**, Depth **0.70**.
2. Rename `SM_PitFlag_01_Pole`.
3. Place at first pit: X **0.85**, Y **3**, Z **0.35** (beside rim).
4. Shift + A → Mesh → **Plane**. Scale to a small flag (~0.25 × 0.15).
5. Rotate: R Y 90, move to top of pole.
6. Join pole + flag: select both → Ctrl + J. Rename `SM_PitFlag_01`.
7. Material: red for flag cloth.
8. Duplicate for pits 2 and 3 (Y 16.5 and 31).
9. Ctrl + S.

---

## Part H — Quick check

In top view (Numpad 7):
- Marbles near Y -6
- Pits at Y 3, 16.5, 31 in a straight line on X=0
- Flags beside pits

If yes → Session 1 blockout is done. Next session: export FBX + Unity smoke test.

---

## Stuck?

Tell me which Part letter you’re on and what you see (or send a screenshot). We’ll fix that step only.