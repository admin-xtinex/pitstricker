using UnityEngine;

namespace PitStriker.Core
{
    /// <summary>
    /// Single source of truth for arena layout in Unity world space.
    ///
    /// Unity: Y-up, Z-forward along the pit line.
    /// Blender authoring: Z-up, Y-forward along the pit line.
    /// FBX export uses axis_forward=-Z, axis_up=Y. VillageGraphicsIntegration then
    /// rotates the graphics root by GraphicsRootEulerY (180) so Blender +Y becomes Unity +Z
    /// and Blender -X hero props land on Unity +X (right bank). Left (-X) stays open for camera.
    ///
    /// Do not hardcode pit Z values in dressers, mesh generators, or fallbacks.
    /// Read them from here.
    /// </summary>
    public static class ArenaFrame
    {
        public const float PitSpacing = 12f;
        public const float PitRadius = 0.18f;
        public const float PitDepth = 0.12f;
        public const float MarbleRadius = 0.16f;
        public const float LaunchDistance = 4f;

        public const float PlayLaneWidth = 14f;
        public const float SideBandWidth = 3f;
        public const float ArenaWidth = PlayLaneWidth + SideBandWidth * 2f;
        public const float ArenaLength = 34f;

        public const float GraphicsRootEulerY = 180f;

        public static readonly float[] PitCentersZ = { 0f, 12f, 24f };

        public static float LaneHalf => PlayLaneWidth * 0.5f;
        public static float ArenaHalf => ArenaWidth * 0.5f;
        public static float MidZ => PitSpacing;
        public static float LaunchZ => -LaunchDistance;
        public static float DressMinZ => LaunchZ - 1.2f;
        public static float DressMaxZ => PitCentersZ[2] + 4.4f;
        public static float GroundMinZ => -10f;
        public static float GroundMaxZ => 34f;

        public static Vector3 PitPosition(int pitNumber)
        {
            int i = Mathf.Clamp(pitNumber, 1, 3) - 1;
            return new Vector3(0f, 0f, PitCentersZ[i]);
        }

        public static Vector3 LaunchPosition(float y = 0.3f)
        {
            return new Vector3(0f, y, LaunchZ);
        }
    }
}
