# Pit Striker Blender Arena Generator

Procedural Blender tooling for generating gameplay-ready Pit Striker environment blockouts.

## What it generates

- 3 pits in one exact straight line
- Configurable pit radius, depth, and spacing
- Launch/spawn position behind Pit 1
- Beach, Grass Field, Rough Soil, and Smooth Clay presets
- Stylized terrain materials and procedural edge props
- Premium glass-style player marbles
- Pit markers and gameplay guides
- Sun/sky lighting
- Gameplay and overview cameras
- Optional `.blend` save and `.glb` export

## Recommended Blender version

Blender 4.x.

## Run from Blender UI

Open `generate_arena.py` in Blender's Scripting workspace and run it.

## Run headless

```bash
blender --background --python Tools/Blender/generate_arena.py -- --map beach --output Art/Generated/pit_striker_beach.blend
```

Generate a GLB as well:

```bash
blender --background --python Tools/Blender/generate_arena.py -- --map grass --output Art/Generated/pit_striker_grass.blend --export-glb Art/Generated/pit_striker_grass.glb
```

Override gameplay dimensions:

```bash
blender --background --python Tools/Blender/generate_arena.py -- --map rough_soil --pit-spacing 12 --pit-radius 0.18
```

## Coordinate convention

- X = arena width
- Y = forward direction / pit progression
- Z = up

Pit centers are generated at:

```text
Pit 1: (0, 0, 0)
Pit 2: (0, spacing, 0)
Pit 3: (0, spacing * 2, 0)
```

This guarantees all three pits remain perfectly collinear regardless of spacing.

## Notes

This is intended as a procedural blockout/foundation. Replace generated primitive props with production assets later while retaining the same gameplay coordinates and dimensions.
