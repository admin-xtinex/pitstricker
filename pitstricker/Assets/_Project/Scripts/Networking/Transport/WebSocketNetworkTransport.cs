using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PitStriker.Networking.Core;
using PitStriker.Networking.Shared;

namespace PitStriker.Networking.Transport
{
    /// <summary>
    /// Rock-solid WebSocket transport using System.Net.WebSockets.ClientWebSocket.
    /// Traverses cellular CGNAT, carrier firewalls, and enterprise Wi-Fi.
    /// </summary>
    public class WebSocketNetworkTransport : INetworkTransport
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private NetworkConnectionState _state = NetworkConnectionState.Disconnected;
        private readonly ConcurrentQueue<byte[]> _incomingQueue = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        private readonly byte[] _receiveBuffer = new byte[8192];
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private float _lastPingSentTime = 0f;
        private float _rttMs = 0f;

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<byte[], int> OnDataReceived;
        public event Action<string> OnError;

        public NetworkConnectionState State => _state;
        public float RttMilliseconds => _rttMs;

        public void SetRtt(float rtt)
        {
            _rttMs = rtt;
        }

        public async void Connect(string address)
        {
            if (_state == NetworkConnectionState.Connected || _state == NetworkConnectionState.Connecting)
            {
                Disconnect();
            }

            _state = NetworkConnectionState.Connecting;
            _cts = new CancellationTokenSource();

            try
            {
                // Auto-format address if missing scheme
                if (!address.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) &&
                    !address.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                {
                    address = "ws://" + address;
                }

                _webSocket = new ClientWebSocket();
                Uri serverUri = new Uri(address);
                var connectTask = _webSocket.ConnectAsync(serverUri, _cts.Token);
                var timeoutTask = Task.Delay(3000, _cts.Token);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask != connectTask)
                {
                    throw new TimeoutException($"Connection to {address} timed out after 3 seconds.");
                }
                await connectTask; // Propagate any exception

                if (_webSocket.State == WebSocketState.Open)
                {
                    _state = NetworkConnectionState.Connected;
                    _mainThreadActions.Enqueue(() => OnConnected?.Invoke());
                    _ = ReceiveLoopAsync(_cts.Token);
                }
                else
                {
                    _state = NetworkConnectionState.Failed;
                    _mainThreadActions.Enqueue(() => OnError?.Invoke("WebSocket failed to reach Open state."));
                }
            }
            catch (Exception ex)
            {
                _state = NetworkConnectionState.Failed;
                _mainThreadActions.Enqueue(() =>
                {
                    OnError?.Invoke($"Connection failed: {ex.Message}");
                    OnDisconnected?.Invoke(ex.Message);
                });
            }
        }

        public async void Disconnect()
        {
            if (_state == NetworkConnectionState.Disconnected) return;
            _state = NetworkConnectionState.Disconnected;

            try
            {
                _cts?.Cancel();
                if (_webSocket != null && (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived))
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                }
            }
            catch
            {
                // Ignored on teardown
            }
            finally
            {
                _webSocket?.Dispose();
                _webSocket = null;
                _cts?.Dispose();
                _cts = null;
                _mainThreadActions.Enqueue(() => OnDisconnected?.Invoke("Client disconnected."));
            }
        }

        public async void Send(byte[] buffer, int length, NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open) return;

            try
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(buffer, 0, length);
                await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WEBSOCKET] Send error: {ex.Message}");
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            byte[] tempBuffer = new byte[8192];
            try
            {
                while (_webSocket != null && _webSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(tempBuffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close", CancellationToken.None);
                        _state = NetworkConnectionState.Disconnected;
                        _mainThreadActions.Enqueue(() => OnDisconnected?.Invoke("Server closed connection."));
                        break;
                    }

                    if (result.Count > 0)
                    {
                        byte[] packet = new byte[result.Count];
                        Buffer.BlockCopy(tempBuffer, 0, packet, 0, result.Count);
                        _incomingQueue.Enqueue(packet);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Clean cancellation
            }
            catch (Exception ex)
            {
                if (_state != NetworkConnectionState.Disconnected)
                {
                    _state = NetworkConnectionState.Failed;
                    _mainThreadActions.Enqueue(() =>
                    {
                        OnError?.Invoke($"Receive loop error: {ex.Message}");
                        OnDisconnected?.Invoke(ex.Message);
                    });
                }
            }
        }

        public void Tick()
        {
            // Dispatch main-thread lifecycle events
            while (_mainThreadActions.TryDequeue(out Action action))
            {
                action?.Invoke();
            }

            // Dispatch incoming packets
            while (_incomingQueue.TryDequeue(out byte[] packet))
            {
                OnDataReceived?.Invoke(packet, packet.Length);
            }
        }
    }
}
