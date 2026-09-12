namespace PitStriker.Networking.Shared
{
    public static class NetworkProtocol
    {
        public const int Version = 1;
        public const uint MagicHeader = 0x50495453; // "PITS" in ASCII
        public const int DefaultPort = 7777;
        public const float DefaultTurnDuration = 30.0f;
        public const float DisconnectGracePeriod = 20.0f;
        public const float PingInterval = 5.0f;
        public const float ConnectionTimeout = 10.0f;

        // Force and aim validation constants
        public const float MinAllowedForce = 0.5f;
        public const float MaxAllowedForce = 45.0f;
        public const float MaxAllowedPitch = 0.15f; // Max vertical component of impulse
    }

    public enum NetworkDelivery : byte
    {
        Unreliable = 0,
        Reliable = 1
    }

    public enum NetworkConnectionState : byte
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Failed
    }

    public enum CloudMatchPhase : byte
    {
        WaitingForPlayers = 0,
        LobbyCountdown = 1,
        TossPhase = 2,
        ReadyToAim = 3,
        Rolling = 4,
        Evaluating = 5,
        MatchCompleted = 6,
        Abandoned = 7
    }

    public enum NetworkOpCode : byte
    {
        // Handshake & Heartbeat
        ConnectRequest = 1,
        ConnectResponse = 2,
        Ping = 3,
        Pong = 4,

        // Lobby & Matchmaking
        CreateRoomRequest = 10,
        CreateRoomResponse = 11,
        JoinRoomRequest = 12,
        JoinRoomResponse = 13,
        QuickMatchRequest = 14,
        MatchFound = 15,
        LeaveRoomRequest = 16,
        PlayerJoined = 17,
        PlayerLeft = 18,
        LobbyCountdown = 19,

        // Gameplay Lifecycle & Turn State
        MatchStarted = 20,
        SubmitShotIntent = 21,
        ShotBroadcast = 22,
        TurnChanged = 23,
        WorldSnapshot = 24,
        PitConquered = 25,
        MatchCompleted = 26,
        RematchRequest = 27,
        RematchConfirmed = 28,

        // Reliability, Reconnection & Disconnects
        ReconnectRequest = 30,
        ReconnectResponse = 31,
        OpponentDisconnected = 32,
        OpponentReconnected = 33,
        MatchAbandoned = 34,

        // Diagnostics / Errors
        ErrorMessage = 99
    }
}
