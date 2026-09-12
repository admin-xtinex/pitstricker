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
        // Set to Hard profile by default to fulfill +50% difficulty requirement
        public static DifficultyMode CurrentMode { get; set; } = DifficultyMode.Hard;

        /// <summary>
        /// Tightens the capture trigger zone inside the pit (requires genuine precision).
        /// </summary>
        public static float PitCatchRadiusMultiplier => CurrentMode switch
        {
            DifficultyMode.Easy => 1.35f,
            DifficultyMode.Medium => 1.0f,
            DifficultyMode.Hard => 0.72f, // 50% tighter than Easy mode
            _ => 0.72f
        };

        /// <summary>
        /// Inward gravitational vortex pulling near-rim marbles down into the cup.
        /// </summary>
        public static float PitVortexStrength => CurrentMode switch
        {
            DifficultyMode.Easy => 9.5f,
            DifficultyMode.Medium => 5.0f,
            DifficultyMode.Hard => 2.5f,
            _ => 2.5f
        };

        /// <summary>
        /// Maximum allowed entry speed for a marble to count as sunk.
        /// Reduced from 0.90 m/s down to 0.48 m/s so fast marbles lip out,
        /// demanding proper pacing and touch from both player and bot.
        /// </summary>
        public static float MaxCaptureSpeed => CurrentMode switch
        {
            DifficultyMode.Easy => 0.90f,
            DifficultyMode.Medium => 0.65f,
            DifficultyMode.Hard => 0.48f, // 50% tighter speed tolerance
            _ => 0.48f
        };

        /// <summary>
        /// Aim assist trajectory line guide length.
        /// 50% shorter guide line for player (requires judging angles and power by eye).
        /// </summary>
        public static float TrajectoryLengthMultiplier => CurrentMode switch
        {
            DifficultyMode.Easy => 1.25f,
            DifficultyMode.Medium => 0.85f,
            DifficultyMode.Hard => 0.50f, // 50% shorter trajectory guide
            _ => 0.50f
        };

        /// <summary>
        /// Launch force boost factor.
        /// </summary>
        public static float LaunchForceMultiplier => CurrentMode switch
        {
            DifficultyMode.Easy => 1.15f,
            DifficultyMode.Medium => 1.0f,
            DifficultyMode.Hard => 1.0f,
            _ => 1.0f
        };

        /// <summary>
        /// Multiplier applied to bot aiming and force variance (+50% more difficulty/spread for bot).
        /// </summary>
        public static float BotVarianceMultiplier => CurrentMode switch
        {
            DifficultyMode.Easy => 0.75f,
            DifficultyMode.Medium => 1.0f,
            DifficultyMode.Hard => 1.50f, // +50% difficulty for bot
            _ => 1.50f
        };
    }
}
