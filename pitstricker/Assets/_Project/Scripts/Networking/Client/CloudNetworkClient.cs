using System;
using System.Threading.Tasks;
using UnityEngine;
using PitStriker.Networking.Core;
using PitStriker.Networking.Shared;
using PitStriker.Networking.Transport;

namespace PitStriker.Networking.Client
{
    /// <summary>
    /// Phase 3-7 Client Orchestrator:
    /// High-level client managing connection, room lifecycle, matchmaking, and state syncing
    /// with the Google Cloud dedicated server.
    /// </summary>
    public class CloudNetworkClient : MonoBehaviour
    {
        public static CloudNetworkClient Instance { get; private set; }

        [Header("Server Configuration")]
        [Tooltip("WebSocket endpoint of the cloud server.")]
        [SerializeField] private string _serverUrl = "ws://pitstriker.xtinex.com:7777";
        [SerializeField] private float _pingIntervalSeconds = 5.0f;

        private static readonly string[] CandidateEndpoints = new string[]
        {
            "ws://pitstriker.xtinex.com:7777",
            "ws://34.69.91.177:7777",
            "ws://127.0.0.1:7777"
        };
        private int _candidateIndex = 0;
        private bool _isConnecting = false;

        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                _serverUrl = value;
                PlayerPrefs.SetString("CloudServerUrl", value);
                PlayerPrefs.Save();
            }
        }

        public NetworkConnectionState ConnectionState => _transport?.State ?? NetworkConnectionState.Disconnected;
        public float RttMs => _transport?.RttMilliseconds ?? 0f;
        public bool IsConnected => ConnectionState == NetworkConnectionState.Connected;

        // Active session info
        public string SessionId { get; private set; } = string.Empty;
        public string ReconnectToken { get; private set; } = string.Empty;
        public string ActiveRoomCode { get; private set; } = string.Empty;
        public int LocalPlayerIndex { get; private set; } = -1; // 0 or 1
        public string LocalPlayerName { get; set; } = "Player";
        public string OpponentName { get; private set; } = "Opponent";

        // Underlying transport
        private INetworkTransport _transport;
        private readonly NetworkByteWriter _writer = new NetworkByteWriter(1024);
        private float _pingTimer = 0f;
        private uint _nextPingId = 1;
        private float _lastPingTime = 0f;
        private uint _inputSequence = 0;

        // Events
        public event Action OnConnectedToServer;
        public event Action<string> OnDisconnectedFromServer;
        public event Action<string> OnRoomCreated;
        public event Action<string> OnRoomJoined;
        public event Action<string> OnOpponentJoined;
        public event Action<int> OnLobbyCountdownTick;
        public event Action<string, string, string> OnMatchStarted; // code, p1, p2
        public event Action<WorldSnapshotData> OnSnapshotReceived;
        public event Action<int, ShotIntentData> OnShotBroadcastReceived; // playerIdx, intent
        public event Action<int> OnMatchCompleted;
        public event Action OnRematchConfirmed;
        public event Action<float> OnOpponentDisconnected; // gracePeriodSecs
        public event Action OnOpponentReconnected;
        public event Action<int, string> OnMatchAbandoned; // winnerIdx, reason
        public event Action<string> OnClientError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                string savedUrl = PlayerPrefs.GetString("CloudServerUrl", "");
                if (!string.IsNullOrEmpty(savedUrl) && !savedUrl.Contains("127.0.0.1") && !savedUrl.Contains("localhost"))
                {
                    _serverUrl = savedUrl;
                }
                else
                {
                    _serverUrl = "ws://pitstriker.xtinex.com:7777";
                    PlayerPrefs.SetString("CloudServerUrl", _serverUrl);
                    PlayerPrefs.Save();
                }
                InitializeTransport();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeTransport()
        {
            _transport = new WebSocketNetworkTransport();
            _transport.OnConnected += HandleTransportConnected;
            _transport.OnDisconnected += HandleTransportDisconnected;
            _transport.OnDataReceived += HandleTransportDataReceived;
            _transport.OnError += HandleTransportError;
        }

        private void Update()
        {
            _transport?.Tick();

            if (IsConnected)
            {
                _pingTimer += Time.unscaledDeltaTime;
                if (_pingTimer >= _pingIntervalSeconds)
                {
                    _pingTimer = 0f;
                    SendPing();
                }
            }
        }

        private void OnDestroy()
        {
            Disconnect();
            if (Instance == this) Instance = null;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("[CLOUD CLIENT] App paused (minimized).");
            }
            else
            {
                Debug.Log("[CLOUD CLIENT] App resumed (foreground). Checking connection...");
                if (!IsConnected && !string.IsNullOrEmpty(ReconnectToken))
                {
                    Debug.Log("[CLOUD CLIENT] Auto-reconnecting to cloud session...");
                    Connect();
                    OnConnectedToServer += HandleAutoReconnectOnResume;
                }
            }
        }

        private void HandleAutoReconnectOnResume()
        {
            OnConnectedToServer -= HandleAutoReconnectOnResume;
            if (!string.IsNullOrEmpty(ReconnectToken))
            {
                Reconnect(ReconnectToken);
            }
        }

        public void Connect(string customUrl = null)
        {
            if (!string.IsNullOrEmpty(customUrl))
            {
                _serverUrl = customUrl;
            }
            _isConnecting = true;
            Debug.Log($"[CLOUD CLIENT] Connecting to {_serverUrl}...");
            _transport?.Connect(_serverUrl);
        }

        public void Disconnect()
        {
            _isConnecting = false;
            _transport?.Disconnect();
            ActiveRoomCode = string.Empty;
            LocalPlayerIndex = -1;
        }

        private void HandleTransportConnected()
        {
            _isConnecting = false;
            Debug.Log($"<color=#00FF88>[CLOUD CLIENT] Successfully connected to {_serverUrl}!</color>");
            // Persist the working server URL so future launches connect instantly
            PlayerPrefs.SetString("CloudServerUrl", _serverUrl);
            PlayerPrefs.Save();

            // Send ConnectRequest handshake
            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.ConnectRequest);
                _writer.WriteInt32(NetworkProtocol.Version);
                _writer.WriteString(LocalPlayerName);
                _transport.Send(_writer.Buffer, _writer.Position);
            }
        }

        private void HandleTransportDisconnected(string reason)
        {
            _isConnecting = false;
            Debug.LogWarning($"[CLOUD CLIENT] Disconnected: {reason}");
            OnDisconnectedFromServer?.Invoke(reason);
        }

        private void HandleTransportError(string error)
        {
            Debug.LogError($"[CLOUD CLIENT ERROR] ({_serverUrl}) {error}");
            if (_isConnecting && _candidateIndex < CandidateEndpoints.Length - 1)
            {
                _candidateIndex++;
                string nextCandidate = CandidateEndpoints[_candidateIndex];
                Debug.LogWarning($"[CLOUD CLIENT] Fallback attempt {_candidateIndex + 1}/{CandidateEndpoints.Length} -> {nextCandidate}");
                _serverUrl = nextCandidate;
                Connect(nextCandidate);
                return;
            }
            _isConnecting = false;
            OnClientError?.Invoke(error);
        }

        public void CreateRoom()
        {
            if (!IsConnected)
            {
                OnClientError?.Invoke("Not connected to server.");
                return;
            }

            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.CreateRoomRequest);
                _transport.Send(_writer.Buffer, _writer.Position);
            }
        }

        public void JoinRoom(string code)
        {
            if (!IsConnected)
            {
                OnClientError?.Invoke("Not connected to server.");
                return;
            }

            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.JoinRoomRequest);
                _writer.WriteString(code);
                _transport.Send(_writer.Buffer, _writer.Position);
            }
        }

        public void RequestQuickMatch()
        {
            if (!IsConnected)
            {
                OnClientError?.Invoke("Not connected to server.");
                return;
            }

            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.QuickMatchRequest);
                _transport.Send(_writer.Buffer, _writer.Position);
            }
        }

        public void SubmitShot(Vector3 direction, float force)
        {
            if (!IsConnected || string.IsNullOrEmpty(ActiveRoomCode)) return;

            uint seq = ++_inputSequence;
            double ts = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            var intent = new ShotIntentData(seq, ts, direction, force);

            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.SubmitShotIntent);
                _writer.WriteShotIntent(intent);
                _transport.Send(_writer.Buffer, _writer.Position);
            }
        }

        public void RequestRematch()
        {
            if (!IsConnected || string.IsNullOrEmpty(ActiveRoomCode)) return;

            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.RematchRequest);
                _transport.Send(_writer.Buffer, _writer.Position);
            }
        }

        public void Reconnect(string token)
        {
            if (!IsConnected) return;

            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.ReconnectRequest);
                _writer.WriteString(token);
                _transport.Send(_writer.Buffer, _writer.Position);
            }
        }

        private void SendPing()
        {
            _lastPingTime = Time.realtimeSinceStartup;
            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.Ping);
                _writer.WriteUInt32(_nextPingId++);
                _transport.Send(_writer.Buffer, _writer.Position, NetworkDelivery.Unreliable);
            }
        }

        private void HandleTransportDataReceived(byte[] buffer, int length)
        {
            var reader = new NetworkByteReader(buffer, 0, length);
            NetworkOpCode opCode = (NetworkOpCode)reader.ReadByte();

            switch (opCode)
            {
                case NetworkOpCode.ConnectResponse:
                    bool compatible = reader.ReadBool();
                    SessionId = reader.ReadString();
                    ReconnectToken = reader.ReadString();

                    if (compatible)
                    {
                        Debug.Log($"[CLOUD CLIENT] Handshake complete. Session ID: {SessionId}");
                        OnConnectedToServer?.Invoke();
                    }
                    else
                    {
                        string err = "Protocol version mismatch with cloud server.";
                        Debug.LogError(err);
                        OnClientError?.Invoke(err);
                        Disconnect();
                    }
                    break;

                case NetworkOpCode.Pong:
                    float rtt = (Time.realtimeSinceStartup - _lastPingTime) * 1000f;
                    if (_transport is WebSocketNetworkTransport wsTransport)
                    {
                        wsTransport.SetRtt(rtt);
                    }
                    break;

                case NetworkOpCode.CreateRoomResponse:
                    ActiveRoomCode = reader.ReadString();
                    LocalPlayerIndex = reader.ReadInt32(); // 0
                    Debug.Log($"[CLOUD CLIENT] Room created: {ActiveRoomCode}");
                    OnRoomCreated?.Invoke(ActiveRoomCode);
                    break;

                case NetworkOpCode.JoinRoomResponse:
                    bool success = reader.ReadBool();
                    if (success)
                    {
                        ActiveRoomCode = reader.ReadString();
                        LocalPlayerIndex = reader.ReadInt32();
                        Debug.Log($"[CLOUD CLIENT] Joined room: {ActiveRoomCode} as P{LocalPlayerIndex + 1}");
                        OnRoomJoined?.Invoke(ActiveRoomCode);
                    }
                    else
                    {
                        string failReason = reader.ReadString();
                        OnClientError?.Invoke(failReason);
                    }
                    break;

                case NetworkOpCode.MatchFound:
                    ActiveRoomCode = reader.ReadString();
                    string hostP1 = reader.ReadString();
                    string guestP2 = reader.ReadString();
                    if (reader.Remaining >= 4)
                    {
                        LocalPlayerIndex = reader.ReadInt32();
                    }
                    OpponentName = LocalPlayerIndex == 0 ? guestP2 : hostP1;
                    Debug.Log($"[CLOUD CLIENT] Match found! Room: {ActiveRoomCode}, vs: {OpponentName}, LocalPlayerIndex: {LocalPlayerIndex}");
                    OnOpponentJoined?.Invoke(OpponentName);
                    break;

                case NetworkOpCode.PlayerJoined:
                    int joinedIdx = reader.ReadInt32();
                    OpponentName = reader.ReadString();
                    Debug.Log($"[CLOUD CLIENT] Opponent joined: {OpponentName}");
                    OnOpponentJoined?.Invoke(OpponentName);
                    break;

                case NetworkOpCode.LobbyCountdown:
                    int countdown = reader.ReadInt32();
                    OnLobbyCountdownTick?.Invoke(countdown);
                    break;

                case NetworkOpCode.MatchStarted:
                    string mCode = reader.ReadString();
                    string p1 = reader.ReadString();
                    string p2 = reader.ReadString();
                    if (reader.Remaining >= 4)
                    {
                        LocalPlayerIndex = reader.ReadInt32();
                    }
                    else if (LocalPlayerIndex < 0)
                    {
                        LocalPlayerIndex = (!string.IsNullOrEmpty(LocalPlayerName) && LocalPlayerName == p1) ? 0 : 1;
                    }
                    Debug.Log($"<color=#00FF88>[CLOUD CLIENT] Match started in room {mCode}! LocalPlayerIndex: {LocalPlayerIndex}</color>");
                    OnMatchStarted?.Invoke(mCode, p1, p2);
                    break;

                case NetworkOpCode.ShotBroadcast:
                    int playerIdx = reader.ReadInt32();
                    ShotIntentData shotIntent = reader.ReadShotIntent();
                    OnShotBroadcastReceived?.Invoke(playerIdx, shotIntent);
                    break;

                case NetworkOpCode.WorldSnapshot:
                    WorldSnapshotData snapshot = reader.ReadWorldSnapshot();
                    OnSnapshotReceived?.Invoke(snapshot);

                    if (snapshot.Phase == CloudMatchPhase.MatchCompleted && snapshot.WinnerPlayerIndex >= 0)
                    {
                        OnMatchCompleted?.Invoke(snapshot.WinnerPlayerIndex);
                    }
                    break;

                case NetworkOpCode.RematchConfirmed:
                    Debug.Log("<color=#00FF88>[CLOUD CLIENT] Rematch confirmed! Restarting...</color>");
                    OnRematchConfirmed?.Invoke();
                    break;

                case NetworkOpCode.OpponentDisconnected:
                    int discPlayer = reader.ReadInt32();
                    float graceSecs = reader.ReadSingle();
                    OnOpponentDisconnected?.Invoke(graceSecs);
                    break;

                case NetworkOpCode.OpponentReconnected:
                    int recPlayer = reader.ReadInt32();
                    OnOpponentReconnected?.Invoke();
                    break;

                case NetworkOpCode.MatchAbandoned:
                    int winIdx = reader.ReadInt32();
                    string abReason = reader.ReadString();
                    OnMatchAbandoned?.Invoke(winIdx, abReason);
                    break;

                case NetworkOpCode.ErrorMessage:
                    string serverErr = reader.ReadString();
                    OnClientError?.Invoke(serverErr);
                    break;
            }
        }
    }
}
