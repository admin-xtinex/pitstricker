# Working project and cleanup

Open `pitstricker/` in Unity Hub. The outer folder is the repository.

- `pitstricker/Assets/_Project/Scenes/SC_Village_Graphics_Test.unity`: current playable village and Android build scene.
- `SC_Sandbox_Learning.unity`: gameplay source required by the village integration/rebuild tool; intentionally retained.
- `pitstricker/Assets/Scenes/SampleScene.unity`: Unity template scene referenced by project settings; not enabled in the Android build.
- `pitstricker/Assets/_Project/Art/Environments/Village/`: current exported scenery, materials and pit meshes.
- `Art/Generated/VillageIntegration/village_graphics.blend`: editable Blender environment source.
- `Tools/Blender/`: source generation/export scripts.
- `pitstricker/Assets/ThirdParty/CrashBash/Fbx/`: assets still referenced by the sandbox and its builder.
- `pitstricker/Assets/ThirdParty/DeadTrees/textures/`: bark textures used by village materials.
- `Builds/Android/`: APK export menu output. The graphics repair build command writes `Builds/PitStriker-graphics-physics.apk`.
- `pitstricker/Library/` and `Logs/`: Unity-generated cache and diagnostics.

Unused beach exports, canyon/dead-tree model imports, downloaded test archives,
third-party demo scenes, the obsolete `vill` scene, scratch editor capture tools,
temporary screenshots, Blender autosaves and old integration backups were removed.
Gameplay scripts and the current village scene were not changed by cleanup.

Recovery archive: `C:/Users/Tisan/Documents/PitStriker-Cleanup-Backups/cleanup-20260909-094518.zip`.
Archive entries use paths relative to the repository root.
