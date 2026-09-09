# Pit Striker — Beach Graphics Assembly (Packs + Our FBX)

**Goal:** Beach Buggy–level polish matching concept art.  
**Approach:** Unity Asset Store packs + our generated Beach FBX.  
**Freeze:** No gameplay / rules changes until Beach look is in.

## Buy first (priority order)

### 1) Marbles (must)
**Ultimate Glass Marbles** (URP)  
https://assetstore.unity.com/packages/vfx/shaders/ultimate-glass-marbles-321831  
- Glass swirl marbles, shader graphs, prefabs  
- Maps to Blue / Red / Green / Amber player marbles  
- Keep your Rigidbody / SphereCollider / MarbleController — visuals only

### 2) Beach environment (must — pick ONE)

**Option A — Best stylized tropical kit (recommended start)**  
**KEKOS Tropical Beach** (URP + Built-in)  
https://assetstore.unity.com/packages/3d/environments/fantasy/kekos-tropical-beach-stylized-3d-art-assets-by-mameshiba-238512  
- Palms, rocks, sand dunes, shells, water/sand shaders, wind  
- Closest “fun premium beach” kit for mobile-ish budgets

**Option B — Max mobile optimization**  
**Tropical Island Hideout (URP/Mobile)**  
https://assetstore.unity.com/packages/3d/environments/fantasy/tropical-island-hideout-interior-exterior-vr-mobile-urp-124915  
- Atlas-heavy, many prefabs, hut/bar/props, foam shader  
- Great if Android performance is the top worry

**Avoid for phone (too heavy):** Caribbean Islands (~4.5M tris demo) unless heavily LODed later.

### 3) Juice / animation feel (must for “Buggy race” vibe)
**Sand VFX - URP**  
https://assetstore.unity.com/packages/vfx/sand-vfx-urp-265902  
- Sand impacts, trails, bursts → marble land / strike / pit sink

### 4) Optional upgrades
- **PRISM 2 Glass (URP)** — if marble pack glass isn’t enough on device  
  https://assetstore.unity.com/packages/vfx/shaders/prism-2-ultimate-advanced-beautiful-glass-urp-369172  
- **Stylized Beach Pack 2** — cheap extra boats/palms fillers  
  https://assetstore.unity.com/packages/3d/environments/stylized-beach-pack-2-285784

## Already in project (ours)
- `Art/Generated/pit_striker_beach.fbx` (+ `.blend` / `.glb`)  
- Copied to `pitstricker/Assets/_Project/Art/Environments/Beach/`  
- Use for: pit spacing reference, fairway scale, temporary props until packs replace them

## Assembly plan (after imports)

1. Import packs into `Assets/ThirdParty/` (keep quarantined).  
2. Menu later: **Pit Striker → Apply Beach Visuals** (I will add once packs are in):  
   - Hide greybox mesh renderers (keep colliders / PitZone)  
   - Spawn beach props around fairway (palms, hut, boat, ocean)  
   - Swap marble meshes/materials to Glass Marbles look  
   - Hook Sand VFX to existing VFXManager events (hit / sink / launch)  
3. Lighting: warm sun + beach sky from pack; no gameplay changes.  
4. Soft motion: palm wind (KEKOS), water foam, ambient dust.  
5. UI polish pass last (logo, glowing shoot button) — still graphics track.

## Performance guardrails (Android)
- Cap on-screen tris; use LODs / disable distant props  
- One ocean plane, not full island open-world  
- Glass: start with pack’s mobile-friendly settings; test on device early  
- Fairway collision stays your current PhysX slabs

## Done when
- Working arena looks like a tropical beach match (not grey sand box)  
- Marbles read as premium glass  
- Strike / land / pit-sink have sand juice  
- Still playable with current rules frozen