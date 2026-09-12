using System;
using UnityEngine;

namespace PitStriker.Networking
{
    /// <summary>
    /// Phase 8 — Multiplayer Security & Audit Logger.
    ///
    /// Provides sanitized, structured logging for multiplayer events (shot commands,
    /// validation rejections, turn transitions, match lifecycle, connection changes).
    ///
    /// Excludes sensitive information (no auth tokens, passwords, or raw IP addresses).
    /// </summary>
    public static class MultiplayerSecurityLogger
    {
        private const string Tag = "<color=#00FFAA><b>[NET-SEC]</b></color>";
        private const string WarnTag = "<color=#FFAA00><b>[NET-SEC WARN]</b></color>";
        private const string ErrTag = "<color=#FF4444><b>[NET-SEC REJECT]</b></color>";

        public static void LogShotAccepted(string matchId, int playerIndex, int turn, int sequence, float force, Vector3 direction)
        {
            Debug.Log($"{Tag} Shot ACCEPTED | Match: {Sanitize(matchId)} | P{playerIndex + 1} | Turn: {turn} | Seq: {sequence} | Force: {force:F1}N | Dir: {direction.normalized}");
        }

        public static void LogShotRejected(string matchId, int playerIndex, int turn, int sequence, string reason, ulong clientId)
        {
            Debug.LogWarning($"{ErrTag} Shot REJECTED | Match: {Sanitize(matchId)} | ClientId: {clientId} | P{playerIndex + 1} | Turn: {turn} | Seq: {sequence} | Reason: {reason}");
        }

        public static void LogTurnTransition(string matchId, int fromPlayer, int toPlayer, int turnNumber)
        {
            Debug.Log($"{Tag} Turn Transition | Match: {Sanitize(matchId)} | P{fromPlayer + 1} -> P{toPlayer + 1} | Turn #{turnNumber}");
        }

        public static void LogScoreUpdate(string matchId, int playerIndex, int strokes, int currentPit)
        {
            Debug.Log($"{Tag} Score Update (Server Authoritative) | Match: {Sanitize(matchId)} | P{playerIndex + 1} | Strokes: {strokes} | Pit: {currentPit}");
        }

        public static void LogMatchResult(string matchId, int winnerIndex, int p1Strokes, int p2Strokes, bool isAbandoned)
        {
            string outcome = isAbandoned ? "MATCH ABANDONED" : $"WINNER: P{winnerIndex + 1}";
            Debug.Log($"{Tag} Match Completed | Match: {Sanitize(matchId)} | {outcome} | P1: {p1Strokes} strokes | P2: {p2Strokes} strokes");
        }

        public static void LogConnectionEvent(string matchId, ulong clientId, string eventType)
        {
            Debug.Log($"{Tag} Connection Event | Match: {Sanitize(matchId)} | ClientId: {clientId} | Event: {eventType}");
        }

        private static string Sanitize(string val)
        {
            if (string.IsNullOrEmpty(val)) return "N/A";
            // Truncate long identifiers to 12 chars to avoid leaking full session tokens
            return val.Length > 12 ? val.Substring(0, 12) + "..." : val;
        }
    }
}
