# Map 02 — Pack 04 Checklist

## Pack 04 goal
Create the reusable **materials + decals** package for `Map 02 — Sunset Coastal`.

## PBR-style material sets
- [x] Sand_Dry — Albedo / Height / Normal / Roughness
- [x] Sand_Packed — Albedo / Height / Normal / Roughness
- [x] Sand_Wet — Albedo / Height / Normal / Roughness
- [x] Wood_Weathered — Albedo / Height / Normal / Roughness
- [x] Rock_Coastal — Albedo / Height / Normal / Roughness
- [x] Rope_Coastal — Albedo / Height / Normal / Roughness

## Transparent decals
- [x] Footprints_Decal
- [x] Shells_Starfish_Decal
- [x] Sand_Scratch_Decal
- [x] PitEdge_Wear_Decal
- [x] Moss_Patch_Decal
- [x] Shoreline_Foam_Decal

## Unity usage
- Use Albedo as Base Map.
- Import Normal files as Normal Map.
- Roughness may need inversion if the shader expects Smoothness.
- Height is optional and should be used sparingly on mobile.
- Decals are RGBA PNGs and should be used with a decal/projector or transparent quad depending on render pipeline.

## Optimization guidance
- Start with 1024 resolution in mobile builds where possible.
- Keep 2048 only for hero surfaces if testing proves it is needed.
- Atlas small decals where appropriate.
- Do not enable parallax/height on every material.
