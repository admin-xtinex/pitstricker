# Village graphics test

For the current full upgrade, see [Reference Graphics Upgrade](../03_Art_and_Audio/Reference%20Graphics%20Upgrade.md). The verification below records the earlier September 8 scene only and does not validate the new upgrade.


Open the Unity project at `C:/Users/Tisan/Documents/pitstricker/pitstricker`
with Unity 6000.6.0f1. Open
`Assets/_Project/Scenes/SC_Village_Graphics_Test.unity` and press Play.

This scene copies the current `SC_Sandbox_Learning` gameplay and replaces
the old village dressing visually with the supplied Blender village environment.
The original scene remains available. Gameplay scripts, pit positions, physics,
marbles, input, HUD and rules are retained. The new graphics pass adjusts camera framing; orbit and follow behavior remain unchanged. The new scenery has no
colliders or gameplay scripts. Build Settings remain unchanged; this is an
Editor test scene, not a packaged Android build.

The graphics source is copied from the supplied download into
`Tools/Blender/VillageSource`. Run `Tools/Blender/export_village_graphics.py`
with Blender in background mode to regenerate the graphics-only FBX. It does
not import the download's gameplay dimensions, pits, marbles or cameras.
The generated Blender file is under `Art/Generated/VillageIntegration`.
The supplied palette initializes missing materials. Existing authored source materials are preserved. The current integration command then generates and applies the full reference upgrade.

`Pit Striker > Create Village Graphics Test Scene` creates or updates this
test scene while preserving its gameplay. Close the test scene before using
the command. The previous automatic arena rebuild on editor load is disabled;
its explicit rebuild menu still exists and should not be used for graphics updates.

Manual checks:

- Launch a marble using the existing controls and confirm camera tracking.
- Check pit capture, turn switching, marble collisions and course completion.
- Inspect the village from gameplay camera angles for obscured shots.
- Check performance and HUD on the intended phone before release.

Integration verification compares the existing scene's gameplay component,
transform, collider and rigidbody data before and after adding graphics.
The Play Mode smoke check covers startup only; it is not a full match test.

Verified on 2026-09-08: Unity import and graphics integration passed; all 11
runtime script hashes remained unchanged. The eight-second Play Mode startup
check passed with four marbles, three pits and no captured runtime errors.
The preview and check results are saved in `Art/Generated/VillageIntegration`.
