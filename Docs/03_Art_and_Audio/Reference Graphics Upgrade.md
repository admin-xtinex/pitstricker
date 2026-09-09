# Village graphics upgrade

## Baseline and target

The supplied current-game screenshot shows a working scene, but its foreground vegetation has broad angular blades, the vegetation border forms a uniform strip, buildings have flat surfaces, and the mountains form a sparse backdrop. The supplied target has fine natural grass, uneven dirt/verge transitions, weathered props, layered vegetation and warm directional light. Shader changes alone cannot bridge this asset gap.

## Rendering corrections in this change

- Remove forced ambient brightness floors from ground, foliage and surface shaders so scene lighting can produce shaded areas.
- Transform surface normal maps using all three components; preserve curved pit normals with neutral ground normal maps. Remove ground relief inferred from albedo brightness (painted color is not height).
- Add URP soft-shadow quality variants and per-vertex/per-pixel additional-light variants.
- Use the same wind displacement for foliage color, depth and shadow passes. Replace constant leaf emission with shadow-aware sunlight transmission.
- Render marbles as polished dielectric surfaces instead of metallic, emissive balls. This remains an opaque approximation, not refractive glass.
- Preserve existing authored materials when importing village graphics. Stop the graphics importer from resizing/repositioning gameplay marbles.

## Review in Unity

1. Pull develop and open the inner `pitstricker/` Unity project.
2. Open `Assets/_Project/Scenes/SC_Village_Graphics_Test.unity`. Existing materials pick up shader edits automatically; regeneration is unnecessary for this shader review.
3. Let shader import finish and check Console errors. Compare the same gameplay camera, quality setting and time of day against the supplied current-game screenshot.
4. Inspect foreground ground/pit lighting, foliage backlighting and moving shadows, and marble highlights. Check both mobile and desktop quality levels.
5. Run the existing `VillageGraphicsSmokeCheck.RunBatch` through Unity batch mode for shader support, gameplay and collision checks. Capture the actual Game view and profile an Android build before accepting the visual result.

Local validation: diff whitespace and static source checks only. Unity/Android compilation, rendered appearance and performance are not verified in the editing environment.

## Asset work still required to approach the reference

| Area | Required change | Acceptance view |
| --- | --- | --- |
| Foreground grass | Narrow curved blades, multiple clump shapes and lengths; textured foliage with controlled color variation | Low gameplay camera, no broad ribbon-like foreground leaves |
| Verge | Irregular clusters, sparse seedlings and gravel crossing the boundary | No continuous straight green wall |
| Ground | Matching soil albedo/normal/roughness maps at consistent physical scale; small scattered stones | Fine relief visible without noisy sparkling or stretched pit walls |
| House/walls | Weathered plaster, roof tiles, stone and wood assets with bevels and PBR textures | Material detail remains readable at gameplay distance |
| Backdrop | Layered palms and broadleaf trees; natural terrain silhouette and atmospheric separation | No exposed empty plane around a row of identical trees |
| Marbles | Environment reflection capture and designed internal ribbons; evaluate mobile-friendly glass approximation | Bright reflections and convincing volume without self-illumination |
| Lighting | Tune golden-hour sun against sky fill after asset replacement; bake static indirect light where supported | Warm lit ground, cooler shaded areas and grounded props |

Preserve pit coordinates, colliders and gameplay rules while replacing decorative assets. Profile vegetation overdraw and draw calls on the target phone. Do not treat an offline reference image as proof of achievable real-time mobile performance.

URP lighting reference: https://docs.unity3d.com/6000.0/Documentation/Manual/urp/use-built-in-shader-methods-additional-lights-fplus.html
