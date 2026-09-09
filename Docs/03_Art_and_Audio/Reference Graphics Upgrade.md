# Full village graphics upgrade

## Apply it

1. Pull `develop` and open the inner `pitstricker/` folder in Unity (the existing Unity 6 project).
2. Open `Assets/_Project/Scenes/SC_Village_Graphics_Test.unity`.
3. Stop Play Mode. Choose **Pit Striker > Graphics > Apply Full Village Upgrade**. Allow the editor to finish generating meshes, textures and materials.
4. Choose **Pit Striker > Graphics > Validate Village Upgrade**, inspect the Game view, and save the scene with Ctrl+S.
5. Enter Play Mode and check launch, pit capture, turn changes, marble collisions, camera orbit and UI. Build and profile on the target Android phone before release.

The existing **Create Village Graphics Test Scene** command also runs this full pass after importing the village FBX. No Blender run, asset purchase or package installation is required. Nothing rebuilds automatically on project load. Do not run the old Deep Red Pits command afterward: it intentionally restores red pit materials.

## What this implements

- Replaces the imported coarse foliage batches and palm trunks with narrow curved grass blades, irregular grass islands, feathered palms and staggered broadleaf trees. Grass cells and broadleaf trees have two distance-dependent mesh detail levels; small distant vegetation is culled.
- Adds foreground gravel, window mullions, veranda joinery, gable infill and steps. Existing tiled-roof and stone-wall geometry is retained and retextured.
- Fills surrounding terrain and replaces the old mountain batches with a connected, asymmetric ridge behind layered planting.
- Generates matching periodic albedo, normal and AO/roughness maps for soil, bark, wood, plaster, stone and terracotta. Ground maps are 512 pixels; prop maps are 256 pixels, with mipmaps, repeat wrapping and anisotropic filtering. These are procedural materials, not scanned production assets.
- Recolors the red bowl/lip materials to soil while retaining pit meshes, colliders and in-pit number markers.
- Applies a lower gameplay camera (height 1.05 m, follow distance 3.2 m, FOV 55), warm low sun, cooler ambient fill, restrained exposure and depth fog. Camera orbit and tracking logic remain unchanged.
- Aligns the sky sun disk with the directional light. Adds a 128-pixel reflection probe with a single time-sliced capture at scene startup and enables realtime probes in the existing quality setting.
- Adds view-dependent internal marble ribbons using a refracted sampling direction. This remains an opaque mobile approximation, not physical transparent glass.
- Keeps the previous shader corrections: scene-controlled ambient shading, corrected normal transforms, URP lighting variants and wind-consistent foliage shadows/depth. Grass vertex color controls root shading and wind weight.

Generated objects live under `Village_Reference_Upgrade`. Generated assets live under `Assets/_Project/Art/Environments/Village/ReferenceUpgrade`. Repeated runs use fixed seeds and update assets at stable paths without changing GUIDs or accumulating extra scenery roots. Save/commit the generated assets and scene from Unity if you want the applied scene available to other developers; this code change supplies the generator, not pre-generated Unity assets.

## Gameplay protection and failure behavior

The command compares serialized collider, rigidbody, marble controller, pit zone, turn manager and input-controller state and their attached transforms before and after applying graphics. It refuses to replace a generated root that has acquired manually added colliders, rigidbodies or scripts. Decorative meshes contain no physics components.

The open-scene command leaves the scene dirty for review. If an error occurs, do not save the scene; reopen the saved scene before retrying. Generation updates its own shared assets at stable paths, so scene Undo alone is not a full asset rollback. Existing authored source material assets are not overwritten by the new material library; renderer bindings use separate generated materials.

The existing integration command saves only after upgrade validation succeeds. It reads the new root list after regeneration, avoiding references to the previous destroyed scenery root.

## Verification

Implemented Unity validation checks:

- nonempty meshes, finite vertices and nondegenerate triangles;
- valid LOD renderer references;
- supported shaders without reported compilation errors;
- no physics components in generated scenery;
- a 450,000 all-LOD triangle review budget (this is not an FPS guarantee).

Results are written to `Library/VillageUpgradeValidation.txt`. The existing Play Mode smoke check also invokes this validation.

For unattended editor generation:

```text
Unity -batchmode -quit -projectPath <repo>/pitstricker -executeMethod PitStriker.EditorTools.VillageVisualUpgrade.RunBatch -logFile <log-path>
```

Validation performed in the editing environment: C# syntax parsing, shader/source contract checks, Unity GUID checks and `git diff --check`. Unity is not installed here, so Unity compilation, actual generated-mesh validation, rendering and Android performance have NOT been run. Do not treat this change as a verified match to the reference image. Capture the same Game view after applying it and compare vegetation density, material scale, shadow readability, pit visibility and marble reflections.

## Remaining acceptance work

The complete procedural upgrade is implemented, but visual acceptance and device profiling require Unity. The reference is a cinematic image; production scanned/authored assets may still be needed for equivalent close-up detail. Do not substitute a generated concept image for a real Game-view capture. Bake static indirect lighting only after the final art placement is approved; this pass does not fabricate lightmaps or claim baked GI.

API references: [LODGroup.SetLODs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/LODGroup.SetLODs.html), [reflection refresh](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ReflectionProbe-refreshMode.html), [URP additional lighting](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/use-built-in-shader-methods-additional-lights-fplus.html).

## Follow-up from the first Unity screenshot

The initial generated scene was too dark, sparse and blotchy. The follow-up narrows the grass planting band, increases clump density and blade width, fills tree crowns, separates pigment variation from relief and brightens soil/bark/plaster palettes. Explicit material-controlled sky fill keeps the unbaked procedural vegetation readable without relying solely on SH data. It defaults off for other authored materials.

The perimeter now has a continuous underlapping ground skirt extending beyond the fog horizon, a rounded low berm and planted border around all four corners and sides. These are decorative meshes with no colliders. The editor command rejects Play Mode to prevent generated edits being lost when leaving Play. Pull, stop Play Mode, rerun the full upgrade, review the corners using camera orbit, then save before testing. These revisions still need a new Game-view capture and Android profiling.
