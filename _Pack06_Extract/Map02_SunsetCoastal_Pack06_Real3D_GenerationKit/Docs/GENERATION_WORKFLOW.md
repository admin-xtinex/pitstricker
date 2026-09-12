# Pack 06 — Recommended Generation Workflow

## Preferred generation route
Use a text-to-3D or image-to-3D model that can export GLB/GLTF.

Recommended process for each asset:
1. Generate one asset at a time.
2. Prefer game-ready / smart-topology modes when available.
3. Export GLB/GLTF.
4. Do a quick Blender cleanup pass.
5. Import into Unity.
6. Test in the actual Map 02 scene before generating variants.

## Blender cleanup
For every generated asset:
- Apply transforms.
- Fix normals.
- Remove hidden/internal geometry if excessive.
- Merge unnecessary material slots.
- Reduce polygons to the stated target range.
- Ensure UVs are usable.
- Pack or relink textures.
- Set pivot/origin.
- Export final GLB/FBX according to project convention.

## Unity import
- Set scale consistently.
- Use URP-compatible materials.
- Compress textures appropriately.
- Avoid MeshCollider unless necessary.
- Use simple Box/Capsule/Convex colliders where possible.
- Disable shadows for assets that do not need them.
- Use LODs only on genuinely large/visible assets.

## Naming
Use:
`M02_<AssetName>_v01`

Example:
`M02_HeroCoastalRock_01_v01.glb`
