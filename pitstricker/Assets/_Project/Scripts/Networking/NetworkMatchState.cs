using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using PitStriker.Gameplay;

namespace PitStriker.Networking
{
    public enum NetworkMatchPhase
    {
        Uninitialized,
        WaitingForPlayers,
        TossPhase,
        ReadyToAim,
        Rolling,
        Evaluating,
        MatchCompleted
    }

    [System.Serializable]
    public struct NetworkPlayerData : INetworkSerializable, IEquatable<NetworkPlayerData>
    {
        public ulong ClientId;
        public FixedString32Bytes Name;
        public int TotalStrokes;
        public int CurrentPit;
        public bool IsFinished;

        public NetworkPlayerData(ulong clientId, string name, int strokes = 0, int currentPit = 1, bool isFinished = false)
        {
            ClientId = clientId;
            Name = new FixedString32Bytes(name ?? "Player");
            TotalStrokes = strokes;
            CurrentPit = currentPit;
            IsFinished = isFinished;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref TotalStrokes);
            serializer.SerializeValue(ref CurrentPit);
            serializer.SerializeValue(ref IsFinished);
        }

        public bool Equals(NetworkPlayerData other)
        {
            return ClientId == other.ClientId &&
                   Name.Equals(other.Name) &&
                   TotalStrokes == other.TotalStrokes &&
                   CurrentPit == other.CurrentPit &&
                   IsFinished == other.IsFinished;
        }

        public override bool Equals(object obj) => obj is NetworkPlayerData other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ClientId, Name.ToString(), TotalStrokes, CurrentPit, IsFinished);
    }

    /// <summary>
    /// Phase 3 Networked Match State & Authoritative Turn System:
    /// Defines a single server-authoritative source of truth for:
    /// - Active player turn
    /// - Turn number
    /// - Turn timer countdown (server-synchronized)
    /// - Player scores (current pit target)
    /// - Player strokes
    /// - Match phase & winner
    /// Clients observe and synchronize via NetworkVariables with zero independent decision-making.
    /// </summary>
    public class NetworkMatchState : NetworkBehaviour
    {
        public static NetworkMatchState Instance { get; private set; }

        public const float DefaultTurnDuration = 30.0f;

        // Authoritative Network Variables (Server Write, Everyone Read)
        public NetworkVariable<FixedString64Bytes> MatchId = new NetworkVariable<FixedString64Bytes>(
            new FixedString64Bytes(""), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<FixedString32Bytes> MapId = new NetworkVariable<FixedString32Bytes>(
            new FixedString32Bytes("sunset_coastal"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> ActivePlayerIndex = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> TurnNumber = new NetworkVariable<int>(
            1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<NetworkMatchPhase> CurrentPhase = new NetworkVariable<NetworkMatchPhase>(
            NetworkMatchPhase.Uninitialized, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> TurnTimerRemaining = new NetworkVariable<float>(
            DefaultTurnDuration, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsTimerRunning = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsMatchCompleted = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> WinnerPlayerIndex = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<NetworkPlayerData> Player1 = new NetworkVariable<NetworkPlayerData>(
            new NetworkPlayerData(0, "Host (Player 1)"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<NetworkPlayerData> Player2 = new NetworkVariable<NetworkPlayerData>(
            new NetworkPlayerData(1, "Guest (Player 2)"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Local Client Events for UI / HUD
        public static event Action<int> OnActivePlayerChangedEvent;
        public static event Action<int> OnTurnNumberChangedEvent;
        public static event Action<NetworkMatchPhase> OnPhaseChangedEvent;
        public static event Action<float> OnTimerTickEvent;
        public static event Action<int, int, int> OnPlayerStatsChangedEvent; // playerIdx, strokes, pit
        public static event Action<int> OnMatchCompletedNetworkEvent; // winnerIdx
        public static event Action OnRematchReadyEvent; // Both players want rematch
        public static event Action<string, string, string> OnMatchStartedClientEvent; // (matchId, hostName, guestName)
        public static event Action<int, Vector3, float> OnNetworkShotExecutedEvent; // (playerIdx, direction, force)
        public static event Action<Vector3, Vector3> OnRestPositionsSynchronizedEvent; // (p1Pos, p2Pos) authoritatively synchronized

        // Phase 8 Validation & Security Constants
        public const float MinAllowedForce = 0.5f;
        public const float MaxAllowedForce = 45f;
        public const float MaxAllowedVerticalRatio = 0.25f;

        // Sequence Tracking (Replay / Duplicate Protection)
        private int _lastSequenceP1 = 0;
        private int _lastSequenceP2 = 0;
        private int _localShotSequence = 0;

        // Rematch handshake flags (Server write, Everyone read)
        public NetworkVariable<bool> Player1WantsRematch = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> Player2WantsRematch = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            ActivePlayerIndex.OnValueChanged += HandleActivePlayerChanged;
            TurnNumber.OnValueChanged += HandleTurnNumberChanged;
            CurrentPhase.OnValueChanged += HandlePhaseChanged;
            TurnTimerRemaining.OnValueChanged += HandleTimerRemainingChanged;
            Player1.OnValueChanged += HandlePlayer1Changed;
            Player2.OnValueChanged += HandlePlayer2Changed;
            IsMatchCompleted.OnValueChanged += HandleMatchCompletedChanged;
            Player1WantsRematch.OnValueChanged += HandleRematchFlagsChanged;
            Player2WantsRematch.OnValueChanged += HandleRematchFlagsChanged;

            Debug.Log($"<color=#00FFAA><b>[NETWORK MATCH STATE]</b> Spawned! IsServer={IsServer}, IsClient={IsClient}, LocalId={NetworkManager.Singleton?.LocalClientId}</color>");
        }

        public override void OnNetworkDespawn()
        {
            ActivePlayerIndex.OnValueChanged -= HandleActivePlayerChanged;
            TurnNumber.OnValueChanged -= HandleTurnNumberChanged;
            CurrentPhase.OnValueChanged -= HandlePhaseChanged;
            TurnTimerRemaining.OnValueChanged -= HandleTimerRemainingChanged;
            Player1.OnValueChanged -= HandlePlayer1Changed;
            Player2.OnValueChanged -= HandlePlayer2Changed;
            IsMatchCompleted.OnValueChanged -= HandleMatchCompletedChanged;
            Player1WantsRematch.OnValueChanged -= HandleRematchFlagsChanged;
            Player2WantsRematch.OnValueChanged -= HandleRematchFlagsChanged;

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned) return;

            // Authoritative server-side turn timer ticking
            if (IsTimerRunning.Value && !IsMatchCompleted.Value && CurrentPhase.Value == NetworkMatchPhase.ReadyToAim)
            {
                float newRemaining = Mathf.Max(0f, TurnTimerRemaining.Value - Time.deltaTime);
                TurnTimerRemaining.Value = newRemaining;

                if (newRemaining <= 0f)
                {
                    HandleTimerExpiredServer();
                }
            }
        }

        // =========================================================================
        // SERVER AUTHORITATIVE MUTATORS (Called ONLY on Server / Host)
        // =========================================================================

        public void ServerInitializeMatch(string matchId, string mapId, ulong hostId, ulong guestId, string hostName, string guestName)
        {
            if (!IsServer)
            {
                Debug.LogError("[NETWORK MATCH STATE] Only the Server/Host can initialize match state!");
                return;
            }

            MatchId.Value = new FixedString64Bytes(matchId ?? "MATCH_01");
            MapId.Value = new FixedString32Bytes(mapId ?? "sunset_coastal");
            ActivePlayerIndex.Value = 0; // Host plays first or toss decided
            TurnNumber.Value = 1;
            CurrentPhase.Value = NetworkMatchPhase.ReadyToAim;
            TurnTimerRemaining.Value = DefaultTurnDuration;
            IsTimerRunning.Value = true;
            IsMatchCompleted.Value = false;
            WinnerPlayerIndex.Value = -1;

            _lastSequenceP1 = 0;
            _lastSequenceP2 = 0;
            _localShotSequence = 0;

            Player1.Value = new NetworkPlayerData(hostId, string.IsNullOrEmpty(hostName) ? "Player 1" : hostName);
            Player2.Value = new NetworkPlayerData(guestId, string.IsNullOrEmpty(guestName) ? "Player 2" : guestName);

            MultiplayerSecurityLogger.LogConnectionEvent(MatchId.Value.ToString(), hostId, $"Host Joined ({Player1.Value.Name})");
            MultiplayerSecurityLogger.LogConnectionEvent(MatchId.Value.ToString(), guestId, $"Guest Joined ({Player2.Value.Name})");
            Debug.Log($"<color=#00FF88><b>[NETWORK MATCH STATE]</b> Initialized match {MatchId.Value}. P1: {Player1.Value.Name} ({hostId}) vs P2: {Player2.Value.Name} ({guestId})</color>");

            NotifyMatchStartedClientRpc(MatchId.Value.ToString(), Player1.Value.Name.ToString(), Player2.Value.Name.ToString());
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void NotifyMatchStartedClientRpc(string matchId, string hostName, string guestName)
        {
            Debug.Log($"<color=#00FFAA><b>[NETWORK MATCH RPC]</b> Match Started Notification received! MatchId={matchId}</color>");
            OnMatchStartedClientEvent?.Invoke(matchId, hostName, guestName);
        }

        public void ServerSetPhase(NetworkMatchPhase phase)
        {
            if (!IsServer) return;
            CurrentPhase.Value = phase;
            IsTimerRunning.Value = (phase == NetworkMatchPhase.ReadyToAim && !IsMatchCompleted.Value);
            if (phase == NetworkMatchPhase.ReadyToAim)
            {
                TurnTimerRemaining.Value = DefaultTurnDuration;
            }
        }

        /// <summary>
        /// Freezes the turn timer during a disconnect grace period without changing the match phase.
        /// </summary>
        public void ServerPauseTimer()
        {
            if (!IsServer) return;
            IsTimerRunning.Value = false;
            Debug.Log("<color=#FFAA00>[NETWORK] Turn timer paused (disconnect grace period).</color>");
        }

        /// <summary>
        /// Resumes the turn timer after a successful reconnect.
        /// </summary>
        public void ServerResumeTimer()
        {
            if (!IsServer) return;
            if (!IsMatchCompleted.Value && CurrentPhase.Value == NetworkMatchPhase.ReadyToAim)
            {
                IsTimerRunning.Value = true;
                Debug.Log("<color=#00FFAA>[NETWORK] Turn timer resumed after reconnect.</color>");
            }
        }

        public void ServerRecordStroke(int playerIndex)
        {
            if (!IsServer) return;

            if (playerIndex == 0)
            {
                var p = Player1.Value;
                p.TotalStrokes++;
                Player1.Value = p;
                MultiplayerSecurityLogger.LogScoreUpdate(MatchId.Value.ToString(), 0, p.TotalStrokes, p.CurrentPit);
                Debug.Log($"<color=#00FFAA>[NETWORK] Player 1 stroke recorded: {p.TotalStrokes}</color>");
            }
            else if (playerIndex == 1)
            {
                var p = Player2.Value;
                p.TotalStrokes++;
                Player2.Value = p;
                MultiplayerSecurityLogger.LogScoreUpdate(MatchId.Value.ToString(), 1, p.TotalStrokes, p.CurrentPit);
                Debug.Log($"<color=#00FFAA>[NETWORK] Player 2 stroke recorded: {p.TotalStrokes}</color>");
            }
        }

        public void ServerRecordPitConquered(int playerIndex, int pitNumber)
        {
            if (!IsServer) return;

            if (playerIndex == 0)
            {
                var p = Player1.Value;
                if (pitNumber >= 3)
                {
                    p.CurrentPit = 3;
                    p.IsFinished = true;
                }
                else
                {
                    p.CurrentPit = pitNumber + 1;
                }
                Player1.Value = p;
                MultiplayerSecurityLogger.LogScoreUpdate(MatchId.Value.ToString(), 0, p.TotalStrokes, p.CurrentPit);
            }
            else if (playerIndex == 1)
            {
                var p = Player2.Value;
                if (pitNumber >= 3)
                {
                    p.CurrentPit = 3;
                    p.IsFinished = true;
                }
                else
                {
                    p.CurrentPit = pitNumber + 1;
                }
                Player2.Value = p;
                MultiplayerSecurityLogger.LogScoreUpdate(MatchId.Value.ToString(), 1, p.TotalStrokes, p.CurrentPit);
            }
        }

        public void ServerAdvanceTurn(bool earnedBonusStrike = false)
        {
            if (!IsServer) return;

            if (IsMatchCompleted.Value) return;

            int prevIndex = ActivePlayerIndex.Value;

            if (!earnedBonusStrike)
            {
                // Switch turn to other player (0 -> 1 or 1 -> 0)
                int nextIndex = (ActivePlayerIndex.Value == 0) ? 1 : 0;

                // Check if other player is already finished
                if (nextIndex == 1 && Player2.Value.IsFinished && !Player1.Value.IsFinished)
                {
                    nextIndex = 0;
                }
                else if (nextIndex == 0 && Player1.Value.IsFinished && !Player2.Value.IsFinished)
                {
                    nextIndex = 1;
                }

                ActivePlayerIndex.Value = nextIndex;
                TurnNumber.Value++;
                MultiplayerSecurityLogger.LogTurnTransition(MatchId.Value.ToString(), prevIndex, nextIndex, TurnNumber.Value);
            }

            TurnTimerRemaining.Value = DefaultTurnDuration;
            CurrentPhase.Value = NetworkMatchPhase.ReadyToAim;
            IsTimerRunning.Value = true;

            Debug.Log($"<color=#00FF88><b>[NETWORK TURN]</b> Turn {TurnNumber.Value}: Active Player is Index {ActivePlayerIndex.Value} ({GetPlayerName(ActivePlayerIndex.Value)})</color>");
        }

        public void ServerEndMatch(int winnerIndex)
        {
            if (!IsServer) return;

            WinnerPlayerIndex.Value = winnerIndex;
            IsMatchCompleted.Value = true;
            IsTimerRunning.Value = false;
            CurrentPhase.Value = NetworkMatchPhase.MatchCompleted;

            // Reset rematch flags for the new result cycle
            Player1WantsRematch.Value = false;
            Player2WantsRematch.Value = false;

            MultiplayerSecurityLogger.LogMatchResult(MatchId.Value.ToString(), winnerIndex, Player1.Value.TotalStrokes, Player2.Value.TotalStrokes, isAbandoned: winnerIndex < 0);
            Debug.Log($"<color=#FFD700><b>[NETWORK MATCH VICTORY]</b> Match finished! Winner is Player {winnerIndex + 1} ({GetPlayerName(winnerIndex)})!</color>");
        }

        // =========================================================================
        // PHASE 8 — AUTHORITATIVE SHOT COMMAND & VALIDATION
        // =========================================================================

        /// <summary>
        /// Phase 8 — Comprehensive gameplay request validation on the authority (Host/Server).
        /// Returns true if valid, or false with an explanatory rejection reason.
        /// </summary>
        public bool ValidateShotCommand(ulong requestingClientId, int sequenceNumber, Vector3 direction, float forceMagnitude, out string rejectionReason)
        {
            // 1. Identity & Membership
            bool isP1 = (requestingClientId == Player1.Value.ClientId);
            bool isP2 = (requestingClientId == Player2.Value.ClientId);
            if (!isP1 && !isP2)
            {
                rejectionReason = $"Client {requestingClientId} is not a member of this match.";
                return false;
            }

            int playerIndex = isP1 ? 0 : 1;

            // 2. Turn Ownership
            if (playerIndex != ActivePlayerIndex.Value)
            {
                rejectionReason = $"Client {requestingClientId} (P{playerIndex + 1}) attempted shot out of turn. Active player is P{ActivePlayerIndex.Value + 1}.";
                return false;
            }

            // 3. Match Phase & Completion
            if (IsMatchCompleted.Value)
            {
                rejectionReason = "Shot rejected: Match is already completed.";
                return false;
            }

            if (CurrentPhase.Value != NetworkMatchPhase.ReadyToAim)
            {
                rejectionReason = $"Shot rejected: Invalid phase {CurrentPhase.Value}. Must be ReadyToAim.";
                return false;
            }

            // 4. Turn Timer
            if (TurnTimerRemaining.Value <= 0f || !IsTimerRunning.Value)
            {
                rejectionReason = $"Shot rejected: Turn timer expired or paused (remaining: {TurnTimerRemaining.Value:F1}s).";
                return false;
            }

            // 5. Sequence Number (Replay / Duplicate Protection)
            int lastSeq = isP1 ? _lastSequenceP1 : _lastSequenceP2;
            if (sequenceNumber <= lastSeq)
            {
                rejectionReason = $"Duplicate or replayed shot sequence {sequenceNumber} (last processed was {lastSeq}).";
                return false;
            }

            // 6. Force Magnitude Bounds
            if (float.IsNaN(forceMagnitude) || float.IsInfinity(forceMagnitude))
            {
                rejectionReason = "Shot rejected: Invalid force magnitude (NaN or Infinity).";
                return false;
            }

            if (forceMagnitude < MinAllowedForce || forceMagnitude > MaxAllowedForce)
            {
                rejectionReason = $"Shot rejected: Force {forceMagnitude:F1}N is outside allowed range [{MinAllowedForce}, {MaxAllowedForce}].";
                return false;
            }

            // 7. Direction Vector Bounds
            if (float.IsNaN(direction.x) || float.IsNaN(direction.y) || float.IsNaN(direction.z) ||
                float.IsInfinity(direction.x) || float.IsInfinity(direction.y) || float.IsInfinity(direction.z))
            {
                rejectionReason = "Shot rejected: Invalid direction coordinates (NaN or Infinity).";
                return false;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                rejectionReason = "Shot rejected: Direction vector is zero.";
                return false;
            }

            Vector3 normDir = direction.normalized;
            if (normDir.y > MaxAllowedVerticalRatio)
            {
                rejectionReason = $"Shot rejected: Vertical pitch ratio {normDir.y:F2} exceeds maximum {MaxAllowedVerticalRatio:F2}.";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        /// <summary>
        /// Entry point called by SwipeLaunchController when the local player executes a shot.
        /// In online mode, routes via ServerRpc if guest client, or directly validates on Host.
        /// </summary>
        public void SubmitLocalShot(Vector3 direction, float forceMagnitude)
        {
            if (NetworkSessionManager.Instance == null || NetworkSessionManager.Instance.ActiveNetworkMode == NetworkSessionManager.NetworkMode.None)
            {
                return;
            }

            _localShotSequence++;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                ProcessShotCommandServer(NetworkManager.Singleton.LocalClientId, _localShotSequence, direction, forceMagnitude);
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                SubmitShotCommandServerRpc(NetworkManager.Singleton.LocalClientId, _localShotSequence, direction, forceMagnitude);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SubmitShotCommandServerRpc(ulong requestingClientId, int sequenceNumber, Vector3 direction, float forceMagnitude)
        {
            ProcessShotCommandServer(requestingClientId, sequenceNumber, direction, forceMagnitude);
        }

        private void ProcessShotCommandServer(ulong requestingClientId, int sequenceNumber, Vector3 direction, float forceMagnitude)
        {
            if (!IsServer) return;

            string matchId = MatchId.Value.ToString();
            int playerIndex = (requestingClientId == Player1.Value.ClientId) ? 0 : 1;

            if (!ValidateShotCommand(requestingClientId, sequenceNumber, direction, forceMagnitude, out string rejectionReason))
            {
                MultiplayerSecurityLogger.LogShotRejected(matchId, playerIndex, TurnNumber.Value, sequenceNumber, rejectionReason, requestingClientId);
                return;
            }

            if (playerIndex == 0) _lastSequenceP1 = sequenceNumber;
            else _lastSequenceP2 = sequenceNumber;

            MultiplayerSecurityLogger.LogShotAccepted(matchId, playerIndex, TurnNumber.Value, sequenceNumber, forceMagnitude, direction);

            // Execute shot on server authoritative physics marble
            ExecuteShotPhysical(playerIndex, direction, forceMagnitude);

            // Replicate verified shot event to clients
            ExecuteShotClientRpc(playerIndex, sequenceNumber, direction, forceMagnitude);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ExecuteShotClientRpc(int playerIndex, int sequenceNumber, Vector3 direction, float forceMagnitude)
        {
            OnNetworkShotExecutedEvent?.Invoke(playerIndex, direction, forceMagnitude);

            // Guest client executes physical impulse on its local visual marble
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                ExecuteShotPhysical(playerIndex, direction, forceMagnitude);
            }
        }

        private void ExecuteShotPhysical(int playerIndex, Vector3 direction, float forceMagnitude)
        {
            if (TurnManager.Instance == null) return;

            var players = TurnManager.Instance.Players;
            if (players != null && playerIndex >= 0 && playerIndex < players.Count)
            {
                var player = players[playerIndex];
                if (player != null && player.marble != null)
                {
                    player.marble.Halt();
                    player.marble.ApplyImpulse(direction, forceMagnitude);
                }
            }
        }

        /// <summary>
        /// Phase 4 — Authoritative Resting Position Synchronization.
        /// Called by TurnManager on the Server after marbles come to a stable stop.
        /// Replicates exact resting coordinates to all clients to eliminate physics divergence.
        /// </summary>
        public void ServerSyncRestPositions(Vector3 p1Pos, Vector3 p2Pos)
        {
            if (!IsServer) return;
            SyncRestPositionsClientRpc(p1Pos, p2Pos);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SyncRestPositionsClientRpc(Vector3 p1Pos, Vector3 p2Pos)
        {
            OnRestPositionsSynchronizedEvent?.Invoke(p1Pos, p2Pos);

            // On non-server clients, smoothly clamp physical marbles to the exact authoritative coordinates
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                if (TurnManager.Instance != null && TurnManager.Instance.Players != null)
                {
                    var players = TurnManager.Instance.Players;
                    if (players.Count > 0 && players[0] != null && players[0].marble != null)
                    {
                        players[0].marble.Halt();
                        players[0].marble.ResetPosition(p1Pos);
                    }
                    if (players.Count > 1 && players[1] != null && players[1].marble != null)
                    {
                        players[1].marble.Halt();
                        players[1].marble.ResetPosition(p2Pos);
                    }
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void InvokeRestPositionsSynchronizedForTesting(Vector3 p1Pos, Vector3 p2Pos)
        {
            OnRestPositionsSynchronizedEvent?.Invoke(p1Pos, p2Pos);
        }
#endif

        /// <summary>
        /// Called by either client to signal they want a rematch.
        /// Server sets the flag; when both are true, OnRematchReadyEvent fires on all clients.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestRematchServerRpc(ulong requestingClientId)
        {
            if (!IsMatchCompleted.Value)
            {
                Debug.LogWarning("[NETWORK REMATCH] Rematch requested but match not completed.");
                return;
            }

            int playerIdx = GetPlayerIndexForClient(requestingClientId);
            if (playerIdx == 0)
            {
                Player1WantsRematch.Value = true;
                Debug.Log($"<color=#00FFAA>[NETWORK REMATCH] Player 1 ({Player1.Value.Name}) wants a rematch.</color>");
            }
            else if (playerIdx == 1)
            {
                Player2WantsRematch.Value = true;
                Debug.Log($"<color=#00FFAA>[NETWORK REMATCH] Player 2 ({Player2.Value.Name}) wants a rematch.</color>");
            }
        }

        /// <summary>
        /// Resets rematch request flags; call at the start of each new match.
        /// </summary>
        public void ServerResetRematchFlags()
        {
            if (!IsServer) return;
            Player1WantsRematch.Value = false;
            Player2WantsRematch.Value = false;
            _lastSequenceP1 = 0;
            _lastSequenceP2 = 0;
            _localShotSequence = 0;
        }

        private int GetPlayerIndexForClient(ulong clientId)
        {
            if (clientId == Player1.Value.ClientId) return 0;
            if (clientId == Player2.Value.ClientId) return 1;
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost ? 0 : 1;
        }

        private void HandleRematchFlagsChanged(bool oldVal, bool newVal)
        {
            if (Player1WantsRematch.Value && Player2WantsRematch.Value)
            {
                Debug.Log("<color=#00FF88><b>[NETWORK REMATCH]</b> Both players agreed! Firing OnRematchReadyEvent.</color>");
                OnRematchReadyEvent?.Invoke();
            }
        }

        private void HandleTimerExpiredServer()
        {
            Debug.LogWarning($"<color=#FFAA00>[NETWORK TIMER] Turn timer expired for player {ActivePlayerIndex.Value} ({GetPlayerName(ActivePlayerIndex.Value)})! Auto-advancing turn...</color>");
            ServerAdvanceTurn(false);
        }

        // =========================================================================
        // CLIENT ACCESSORS & VALIDATION (Task 2, 4, 6)
        // =========================================================================

        public int GetLocalPlayerIndex()
        {
            if (NetworkManager.Singleton == null) return 0;
            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (localId == Player1.Value.ClientId) return 0;
            if (localId == Player2.Value.ClientId) return 1;
            return NetworkManager.Singleton.IsHost ? 0 : 1;
        }

        public bool IsMyTurn()
        {
            if (NetworkSessionManager.Instance == null || NetworkSessionManager.Instance.ActiveNetworkMode == NetworkSessionManager.NetworkMode.None)
            {
                return true; // Local mode: always allow input
            }

            if (!IsSpawned || IsMatchCompleted.Value) return false;
            if (CurrentPhase.Value != NetworkMatchPhase.ReadyToAim) return false;
            if (TurnTimerRemaining.Value <= 0f) return false;

            return GetLocalPlayerIndex() == ActivePlayerIndex.Value;
        }

        public bool IsActionAllowed(ulong requestingClientId)
        {
            if (IsMatchCompleted.Value) return false;
            if (CurrentPhase.Value != NetworkMatchPhase.ReadyToAim) return false;
            if (TurnTimerRemaining.Value <= 0f) return false;

            int activeIdx = ActivePlayerIndex.Value;
            ulong allowedClientId = (activeIdx == 0) ? Player1.Value.ClientId : Player2.Value.ClientId;
            return requestingClientId == allowedClientId;
        }

        public string GetPlayerName(int index)
        {
            if (index == 0) return Player1.Value.Name.ToString();
            if (index == 1) return Player2.Value.Name.ToString();
            return "Player";
        }

        public int GetPlayerStrokes(int index)
        {
            if (index == 0) return Player1.Value.TotalStrokes;
            if (index == 1) return Player2.Value.TotalStrokes;
            return 0;
        }

        public int GetPlayerCurrentPit(int index)
        {
            if (index == 0) return Player1.Value.CurrentPit;
            if (index == 1) return Player2.Value.CurrentPit;
            return 1;
        }

        // =========================================================================
        // NETWORK VARIABLE CHANGE HANDLERS
        // =========================================================================

        private void HandleActivePlayerChanged(int oldIdx, int newIdx)
        {
            OnActivePlayerChangedEvent?.Invoke(newIdx);
        }

        private void HandleTurnNumberChanged(int oldTurn, int newTurn)
        {
            OnTurnNumberChangedEvent?.Invoke(newTurn);
        }

        private void HandlePhaseChanged(NetworkMatchPhase oldPhase, NetworkMatchPhase newPhase)
        {
            OnPhaseChangedEvent?.Invoke(newPhase);
        }

        private void HandleTimerRemainingChanged(float oldTime, float newTime)
        {
            OnTimerTickEvent?.Invoke(newTime);
        }

        private void HandlePlayer1Changed(NetworkPlayerData oldData, NetworkPlayerData newData)
        {
            OnPlayerStatsChangedEvent?.Invoke(0, newData.TotalStrokes, newData.CurrentPit);
        }

        private void HandlePlayer2Changed(NetworkPlayerData oldData, NetworkPlayerData newData)
        {
            OnPlayerStatsChangedEvent?.Invoke(1, newData.TotalStrokes, newData.CurrentPit);
        }

        private void HandleMatchCompletedChanged(bool wasCompleted, bool isCompleted)
        {
            if (isCompleted)
            {
                OnMatchCompletedNetworkEvent?.Invoke(WinnerPlayerIndex.Value);
            }
        }
    }
}
