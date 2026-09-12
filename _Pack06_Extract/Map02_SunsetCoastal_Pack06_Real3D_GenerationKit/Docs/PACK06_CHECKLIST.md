# Pack 06 — Real 3D Generation Checklist

## Goal
Generate only the depth-critical assets that justify real 3D for `Map 02 — Sunset Coastal`.

## Priority queue
- [ ] 01 HeroCoastalRock_01
- [ ] 02 BoundaryLog_Module_01
- [ ] 03 WoodenPost_Rope_01
- [ ] 04 BeachShack_Hero_01
- [ ] 05 SmallFishingBoat_01
- [ ] 06 DockModule_01
- [ ] 07 Surfboard_01
- [ ] 08 Crate_01
- [ ] 09 WoodenBarrel_01
- [ ] 10 ClayPot_01
- [ ] 11 DriftwoodObstacle_01
- [ ] 12 HeroPalm_01

## Do not convert to 3D by default
Keep these as 2.5D/impostors unless camera testing proves otherwise:
- distant lighthouse island
- far cliffs
- mountains
- far palm forests
- distant huts
- far boats
- haze/cloud layers
- distant vegetation masses

## After each model is generated
1. Download/export GLB or GLTF.
2. Verify mesh opens correctly.
3. Check scale and orientation.
4. Move pivot to a sensible Unity origin.
5. Check material count.
6. Check texture sizes.
7. Simplify topology if needed.
8. Create collider only if gameplay/near-contact requires it.
9. Test from Camera A and Camera B.
10. Mark checklist complete only after Unity validation.
