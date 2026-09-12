using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using PitStriker.Networking.Shared;

namespace PitStrikerServer
{
    public class WebSocketServer
    {
        private readonly ServerConfiguration _config;
        private readonly RoomManager _roomManager;
        private readonly ConcurrentDictionary<string, ClientSession> _sessions = new ConcurrentDictionary<string, ClientSession>();
        private readonly ConcurrentDictionary<string, ClientSession> _reconnectTokens = new ConcurrentDictionary<string, ClientSession>();

        private HttpListener? _httpListener;
        private CancellationTokenSource? _cts;
        private bool _isRunning = false;

        public WebSocketServer(ServerConfiguration config, RoomManager roomManager)
        {
            _config = config;
            _roomManager = roomManager;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _httpListener = new HttpListener();

            string prefixWildcard = $"http://+:{_config.Port}/";
            try
            {
                _httpListener.Prefixes.Add(prefixWildcard);
                _httpListener.Start();
                Console.WriteLine($"[SERVER] Bound to wildcard prefix: {prefixWildcard}");
            }
            catch
            {
                // Fallback to local endpoints if non-admin on Windows
                _httpListener.Close();
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{_config.Port}/");
                _httpListener.Prefixes.Add($"http://127.0.0.1:{_config.Port}/");
                _httpListener.Start();
                Console.WriteLine($"[SERVER] Bound to local endpoints (localhost, 127.0.0.1) on port {_config.Port}");
            }

            _isRunning = true;
            Console.WriteLine($"=================================================");
            Console.WriteLine($"★ PitStriker Cloud Server (.NET 10) Online ★");
            Console.WriteLine($"  Listening on: port {_config.Port}");
            Console.WriteLine($"  Environment:  {_config.Environment}");
            Console.WriteLine($"  Protocol Ver: {NetworkProtocol.Version}");
            Console.WriteLine($"=================================================");

            _ = AcceptConnectionsLoopAsync(_cts.Token);
            _ = ServerSimulationLoopAsync(_cts.Token);
        }

        public void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch { }
            Console.WriteLine("[SERVER] Server stopped.");
        }

        private async Task AcceptConnectionsLoopAsync(CancellationToken ct)
        {
            while (_isRunning && !ct.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = await _httpListener!.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        _ = ProcessWebSocketRequestAsync(context);
                    }
                    else
                    {
                        // Health check HTTP endpoint (for Google Cloud Load Balancer / Cloud Run)
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/json";
                        byte[] healthBytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"healthy\",\"game\":\"PitStriker\"}\n");
                        await context.Response.OutputStream.WriteAsync(healthBytes, 0, healthBytes.Length, ct);
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException) when (!_isRunning)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning) Console.WriteLine($"[SERVER] Accept error: {ex.Message}");
                }
            }
        }

        private async Task ProcessWebSocketRequestAsync(HttpListenerContext context)
        {
            WebSocketContext wsContext;
            try
            {
                wsContext = await context.AcceptWebSocketAsync(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] WebSocket handshake error: {ex.Message}");
                return;
            }

            WebSocket socket = wsContext.WebSocket;
            string sessionId = Guid.NewGuid().ToString("N");
            var session = new ClientSession(sessionId, socket, "Player");
            _sessions[sessionId] = session;
            _reconnectTokens[session.ReconnectToken] = session;

            Console.WriteLine($"[SESSION {sessionId}] Connected from {context.Request.RemoteEndPoint}");

            byte[] buffer = new byte[8192];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        try
                        {
                            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        }
                        catch { }
                        break;
                    }

                    if (result.Count > 0)
                    {
                        session.LastSeenUtc = DateTime.UtcNow;
                        await HandleMessageAsync(session, buffer, result.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SESSION {sessionId}] Disconnected with error: {ex.Message}");
            }
            finally
            {
                await HandleSessionDisconnectedAsync(session);
            }
        }

        private async Task HandleMessageAsync(ClientSession session, byte[] buffer, int length)
        {
            var reader = new NetworkByteReader(buffer, 0, length);
            NetworkOpCode opCode = (NetworkOpCode)reader.ReadByte();

            switch (opCode)
            {
                case NetworkOpCode.ConnectRequest:
                    int clientProtoVer = reader.ReadInt32();
                    string clientPlayerName = reader.ReadString();
                    session.PlayerName = !string.IsNullOrWhiteSpace(clientPlayerName) ? clientPlayerName : "Player";

                    var connectWriter = new NetworkByteWriter(128);
                    connectWriter.WriteByte((byte)NetworkOpCode.ConnectResponse);
                    connectWriter.WriteBool(clientProtoVer == NetworkProtocol.Version);
                    connectWriter.WriteString(session.SessionId);
                    connectWriter.WriteString(session.ReconnectToken);
                    await session.SendAsync(connectWriter.Buffer, connectWriter.Position);
                    break;

                case NetworkOpCode.Ping:
                    uint pingId = reader.ReadUInt32();
                    var pongWriter = new NetworkByteWriter(16);
                    pongWriter.WriteByte((byte)NetworkOpCode.Pong);
                    pongWriter.WriteUInt32(pingId);
                    await session.SendAsync(pongWriter.Buffer, pongWriter.Position);
                    break;

                case NetworkOpCode.CreateRoomRequest:
                    Room room = _roomManager.CreateRoom();
                    room.AddPlayer(session);

                    var createWriter = new NetworkByteWriter(128);
                    createWriter.WriteByte((byte)NetworkOpCode.CreateRoomResponse);
                    createWriter.WriteString(room.RoomCode);
                    createWriter.WriteInt32(0); // Host is Player Index 0
                    await session.SendAsync(createWriter.Buffer, createWriter.Position);
                    break;

                case NetworkOpCode.JoinRoomRequest:
                    string code = reader.ReadString();
                    Room? targetRoom = _roomManager.GetRoom(code);

                    var joinWriter = new NetworkByteWriter(128);
                    joinWriter.WriteByte((byte)NetworkOpCode.JoinRoomResponse);

                    if (targetRoom == null)
                    {
                        joinWriter.WriteBool(false);
                        joinWriter.WriteString("Room not found.");
                        await session.SendAsync(joinWriter.Buffer, joinWriter.Position);
                    }
                    else if (targetRoom.IsFull)
                    {
                        joinWriter.WriteBool(false);
                        joinWriter.WriteString("Room is already full.");
                        await session.SendAsync(joinWriter.Buffer, joinWriter.Position);
                    }
                    else
                    {
                        targetRoom.AddPlayer(session);
                        joinWriter.WriteBool(true);
                        joinWriter.WriteString(targetRoom.RoomCode);
                        joinWriter.WriteInt32(session.RoomPlayerIndex);
                        await session.SendAsync(joinWriter.Buffer, joinWriter.Position);

                        // Notify host that player 2 joined
                        await targetRoom.BroadcastOpCodeAsync(NetworkOpCode.PlayerJoined, w =>
                        {
                            w.WriteInt32(1);
                            w.WriteString(session.PlayerName);
                        });

                        // Both connected! Start lobby countdown
                        _ = _roomManager.RunCountdownAndStartMatchAsync(targetRoom);
                    }
                    break;

                case NetworkOpCode.QuickMatchRequest:
                    _roomManager.EnqueueQuickMatch(session);
                    break;

                case NetworkOpCode.SubmitShotIntent:
                    if (session.CurrentRoom != null)
                    {
                        ShotIntentData intent = reader.ReadShotIntent();
                        bool accepted = session.CurrentRoom.MatchEngine.SubmitShot(session.RoomPlayerIndex, intent);

                        if (accepted)
                        {
                            // Broadcast shot firing to both clients
                            await session.CurrentRoom.BroadcastOpCodeAsync(NetworkOpCode.ShotBroadcast, w =>
                            {
                                w.WriteInt32(session.RoomPlayerIndex);
                                w.WriteShotIntent(intent);
                            });
                        }
                    }
                    break;

                case NetworkOpCode.RematchRequest:
                    if (session.CurrentRoom != null)
                    {
                        var engine = session.CurrentRoom.MatchEngine;
                        if (session.RoomPlayerIndex == 0) engine.Player0WantsRematch = true;
                        if (session.RoomPlayerIndex == 1) engine.Player1WantsRematch = true;

                        Console.WriteLine($"[ROOM {session.CurrentRoom.RoomCode}] P{session.RoomPlayerIndex + 1} wants rematch (P0: {engine.Player0WantsRematch}, P1: {engine.Player1WantsRematch})");

                        if (engine.Player0WantsRematch && engine.Player1WantsRematch)
                        {
                            Console.WriteLine($"[ROOM {session.CurrentRoom.RoomCode}] Both players agreed to rematch! Starting fresh match...");
                            await session.CurrentRoom.BroadcastOpCodeAsync(NetworkOpCode.RematchConfirmed);
                            _ = _roomManager.RunCountdownAndStartMatchAsync(session.CurrentRoom);
                        }
                    }
                    break;

                case NetworkOpCode.ReconnectRequest:
                    string token = reader.ReadString();
                    var reconWriter = new NetworkByteWriter(128);
                    reconWriter.WriteByte((byte)NetworkOpCode.ReconnectResponse);

                    if (_reconnectTokens.TryGetValue(token, out ClientSession? priorSession) && priorSession.CurrentRoom != null)
                    {
                        priorSession.Socket = session.Socket;
                        session.CurrentRoom = priorSession.CurrentRoom;
                        session.RoomPlayerIndex = priorSession.RoomPlayerIndex;
                        session.PlayerName = priorSession.PlayerName;
                        if (priorSession.RoomPlayerIndex == 0)
                            priorSession.CurrentRoom.Player0 = session;
                        else if (priorSession.RoomPlayerIndex == 1)
                            priorSession.CurrentRoom.Player1 = session;

                        priorSession.CurrentRoom.DisconnectGraceStartUtc = null; // Cancel grace period forfeit
                        priorSession.CurrentRoom.DisconnectedPlayerIndex = -1;

                        reconWriter.WriteBool(true);
                        reconWriter.WriteString(priorSession.CurrentRoom.RoomCode);
                        reconWriter.WriteInt32(priorSession.RoomPlayerIndex);
                        await session.SendAsync(reconWriter.Buffer, reconWriter.Position);

                        // Broadcast opponent reconnected
                        await priorSession.CurrentRoom.BroadcastOpCodeAsync(NetworkOpCode.OpponentReconnected, w =>
                        {
                            w.WriteInt32(priorSession.RoomPlayerIndex);
                        });

                        // Rehydrate full snapshot
                        await priorSession.CurrentRoom.BroadcastSnapshotAsync();
                        Console.WriteLine($"[RECONNECT] Reconnected {priorSession.PlayerName} to room {priorSession.CurrentRoom.RoomCode}");
                    }
                    else
                    {
                        reconWriter.WriteBool(false);
                        reconWriter.WriteString("Session token not found or match expired.");
                        await session.SendAsync(reconWriter.Buffer, reconWriter.Position);
                    }
                    break;
            }
        }

        private async Task HandleSessionDisconnectedAsync(ClientSession session)
        {
            _sessions.TryRemove(session.SessionId, out _);

            if (session.CurrentRoom != null)
            {
                Room room = session.CurrentRoom;
                Console.WriteLine($"[ROOM {room.RoomCode}] Player {session.RoomPlayerIndex + 1} ({session.PlayerName}) disconnected.");

                // Start disconnect grace period
                room.DisconnectGraceStartUtc = DateTime.UtcNow;
                room.DisconnectedPlayerIndex = session.RoomPlayerIndex;

                // Notify opponent of grace period start
                await room.BroadcastOpCodeAsync(NetworkOpCode.OpponentDisconnected, w =>
                {
                    w.WriteInt32(session.RoomPlayerIndex);
                    w.WriteSingle(NetworkProtocol.DisconnectGracePeriod);
                });
            }
        }

        private async Task ServerSimulationLoopAsync(CancellationToken ct)
        {
            float dt = 1f / _config.TickRateHz;
            int tickDelayMs = (int)(dt * 1000f);

            int snapshotIntervalRolling = Math.Max(1, _config.TickRateHz / _config.RollingSnapshotRateHz);
            int snapshotIntervalIdle = Math.Max(1, _config.TickRateHz / _config.IdleSnapshotRateHz);
            int tickCount = 0;

            while (_isRunning && !ct.IsCancellationRequested)
            {
                try
                {
                    tickCount++;
                    _roomManager.Tick(dt);

                    // Broadcast snapshots
                    foreach (var room in _roomManager.ActiveRooms)
                    {
                        bool isRolling = room.MatchEngine.Phase == CloudMatchPhase.Rolling;
                        int interval = isRolling ? snapshotIntervalRolling : snapshotIntervalIdle;

                        if (tickCount % interval == 0 && room.MatchEngine.Phase != CloudMatchPhase.WaitingForPlayers)
                        {
                            _ = room.BroadcastSnapshotAsync();
                        }
                    }

                    if (tickCount % 200 == 0) // Every 10 seconds
                    {
                        _roomManager.RemoveEmptyRooms();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SIM LOOP ERROR] {ex.Message}");
                }

                await Task.Delay(tickDelayMs, ct);
            }
        }
    }
}
