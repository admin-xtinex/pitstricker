using System;
using UnityEngine;

namespace PitStriker.Networking
{
    /// <summary>
    /// Phase 10 — Privacy-Safe Multiplayer Analytics & Telemetry.
    ///
    /// Dispatches telemetry events for key multiplayer lifecycle milestones.
    /// Does not collect sensitive personal data, device identifiers, or auth tokens.
    /// </summary>
    public static class MultiplayerAnalytics
    {
        public static event Action OnOnlineMenuOpenedEvent;
        public static event Action OnMatchmakingStartedEvent;
        public static event Action OnMatchFoundEvent;
        public static event Action<string> OnPrivateRoomCreatedEvent; // joinCode
        public static event Action OnPrivateRoomJoinedEvent;
        public static event Action<string, string> OnMatchStartedEvent; // matchId, mapId
        public static event Action<string, int, int, int> OnMatchCompletedEvent; // matchId, winnerIdx, strokesP1, strokesP2
        public static event Action<string> OnOpponentDisconnectedEvent;
        public static event Action<string> OnOpponentReconnectedEvent;
        public static event Action<string> OnMatchAbandonedEvent;

        public static void TrackOnlineMenuOpened()
        {
            OnOnlineMenuOpenedEvent?.Invoke();
            LogTelemetry("online_menu_opened");
        }

        public static void TrackMatchmakingStarted()
        {
            OnMatchmakingStartedEvent?.Invoke();
            LogTelemetry("matchmaking_started");
        }

        public static void TrackMatchFound()
        {
            OnMatchFoundEvent?.Invoke();
            LogTelemetry("match_found");
        }

        public static void TrackPrivateRoomCreated(string joinCode)
        {
            OnPrivateRoomCreatedEvent?.Invoke(joinCode);
            LogTelemetry("private_room_created", $"code: {joinCode}");
        }

        public static void TrackPrivateRoomJoined()
        {
            OnPrivateRoomJoinedEvent?.Invoke();
            LogTelemetry("private_room_joined");
        }

        public static void TrackMatchStarted(string matchId, string mapId)
        {
            OnMatchStartedEvent?.Invoke(matchId, mapId);
            LogTelemetry("match_started", $"matchId: {matchId}, map: {mapId}");
        }

        public static void TrackMatchCompleted(string matchId, int winnerIndex, int strokesP1, int strokesP2)
        {
            OnMatchCompletedEvent?.Invoke(matchId, winnerIndex, strokesP1, strokesP2);
            LogTelemetry("match_completed", $"matchId: {matchId}, winner: P{winnerIndex + 1}, P1: {strokesP1}, P2: {strokesP2}");
        }

        public static void TrackOpponentDisconnected(string matchId)
        {
            OnOpponentDisconnectedEvent?.Invoke(matchId);
            LogTelemetry("opponent_disconnected", $"matchId: {matchId}");
        }

        public static void TrackOpponentReconnected(string matchId)
        {
            OnOpponentReconnectedEvent?.Invoke(matchId);
            LogTelemetry("opponent_reconnected", $"matchId: {matchId}");
        }

        public static void TrackMatchAbandoned(string matchId)
        {
            OnMatchAbandonedEvent?.Invoke(matchId);
            LogTelemetry("match_abandoned", $"matchId: {matchId}");
        }

        private static void LogTelemetry(string eventName, string details = null)
        {
            string detailStr = string.IsNullOrEmpty(details) ? "" : $" | {details}";
            Debug.Log($"<color=#70C0FF><b>[ANALYTICS]</b></color> {eventName}{detailStr}");
        }
    }
}
