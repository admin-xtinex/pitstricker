using UnityEngine;

namespace PitStriker.Gameplay
{
    public enum DifficultyMode
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Configuration profiles for course difficulty (Easy, Medium, Hard).
    /// Defaults to Easy mode per design request.
    /// </summary>
    public static class GameDifficulty
    {
        public static DifficultyMode CurrentMode { get; set; } = DifficultyMode.Easy;

        /// <summary>
        /// Expands the capture trigger zone inside the pit for Easy mode.
        /// </summary>
        public static float PitCatchRadiusMultiplier => CurrentMode switch
        {
            DifficultyMode.Easy => 1.35f,
            DifficultyMode.Medium => 1.0f,
            DifficultyMode.Hard => 0.85f,
            _ => 1.0f
        };

        /// <summary>
        /// Inward gravitational vortex pulling near-rim marbles down into the cup.
        /// </summary>
        public static float PitVortexStrength => CurrentMode switch
        {
            DifficultyMode.Easy => 9.5f,
            DifficultyMode.Medium => 6.0f,
            DifficultyMode.Hard => 3.5f,
            _ => 6.0f
        };

        /// <summary>
        /// Aim assist trajectory line guide length.
        /// </summary>
        public static float TrajectoryLengthMultiplier => CurrentMode switch
        {
            DifficultyMode.Easy => 1.25f,
            DifficultyMode.Medium => 1.0f,
            DifficultyMode.Hard => 0.65f,
            _ => 1.0f
        };

        /// <summary>
        /// Launch force boost for comfortable long fairway drives.
        /// </summary>
        public static float LaunchForceMultiplier => CurrentMode switch
        {
            DifficultyMode.Easy => 1.15f,
            DifficultyMode.Medium => 1.0f,
            DifficultyMode.Hard => 0.95f,
            _ => 1.0f
        };
    }
}
