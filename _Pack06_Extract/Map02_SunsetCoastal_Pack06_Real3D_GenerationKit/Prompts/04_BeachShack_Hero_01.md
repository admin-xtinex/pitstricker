# BeachShack_Hero_01

**Priority:** 4

**Target output:** GLB/GLTF

**Topology target:** 20k-45k tris

## Generation Prompt

Create a compact hero beach shack for a stylized semi-realistic tropical sunset game environment. Weathered wood walls, small sloped thatch or corrugated coastal roof, simple open porch, subtle nautical details, one or two hanging lantern points, no interior complexity beyond what is visible from outside, no text/signage. It must look premium from two elevated three-quarter camera angles while remaining mobile-game friendly. PBR textures, efficient topology, simplified unseen backside/interior, clean pivot at ground center.

## Post-generation checks

- Check silhouette from Camera A and Camera B.
- Fix pivot/origin.
- Verify Unity scale.
- Remove unnecessary materials.
- Reduce topology if above target.
- Add simple collider only if required.
- Generate LOD only if justified by size/distance.
