using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using PitStriker.Networking.Shared;

namespace PitStrikerServer
{
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new ConcurrentDictionary<string, Room>();
        private readonly ConcurrentQueue<ClientSession> _quickMatchQueue = new ConcurrentQueue<ClientSession>();
        private readonly Random _random = new Random();

        public ICollection<Room> ActiveRooms => _rooms.Values;

        public Room CreateRoom()
        {
            string code;
            do
            {
                code = GenerateRoomCode();
            } while (_rooms.ContainsKey(code));

            var room = new Room(code);
            _rooms[code] = room;
            Console.WriteLine($"[ROOM MANAGER] Created private room: {code}");
            return room;
        }

        public Room? GetRoom(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            _rooms.TryGetValue(code.Trim().ToUpperInvariant(), out Room? room);
            return room;
        }

        public void EnqueueQuickMatch(ClientSession session)
        {
            // Clean up any stale disconnected sessions in queue
            while (_quickMatchQueue.TryDequeue(out ClientSession? opponent))
            {
                if (opponent.IsConnected && opponent != session)
                {
                    // Found opponent! Create a match room for them
                    Room room = CreateRoom();
                    room.AddPlayer(opponent);
                    room.AddPlayer(session);

                    Console.WriteLine($"[MATCHMAKING] Matched '{opponent.PlayerName}' with '{session.PlayerName}' in room {room.RoomCode}!");

                    // Notify both
                    _ = NotifyMatchFoundAsync(room);
                    return;
                }
            }

            // No waiting opponent found, add to queue
            _quickMatchQueue.Enqueue(session);
            Console.WriteLine($"[MATCHMAKING] '{session.PlayerName}' added to Quick Match queue.");
        }

        private async Task NotifyMatchFoundAsync(Room room)
        {
            var writer = new NetworkByteWriter(256);
            writer.WriteByte((byte)NetworkOpCode.MatchFound);
            writer.WriteString(room.RoomCode);
            writer.WriteString(room.Player0?.PlayerName ?? "Player 1");
            writer.WriteString(room.Player1?.PlayerName ?? "Player 2");

            await room.BroadcastAsync(writer.Buffer, writer.Position);

            // Start countdown
            _ = RunCountdownAndStartMatchAsync(room);
        }

        public async Task RunCountdownAndStartMatchAsync(Room room)
        {
            var writer = new NetworkByteWriter(64);

            for (int countdown = 3; countdown >= 1; countdown--)
            {
                writer.Reset();
                writer.WriteByte((byte)NetworkOpCode.LobbyCountdown);
                writer.WriteInt32(countdown);
                await room.BroadcastAsync(writer.Buffer, writer.Position);
                await Task.Delay(1000);
            }

            // Start match!
            room.MatchEngine.StartMatch();

            writer.Reset();
            writer.WriteByte((byte)NetworkOpCode.MatchStarted);
            writer.WriteString(room.RoomCode);
            writer.WriteString(room.Player0?.PlayerName ?? "Player 1");
            writer.WriteString(room.Player1?.PlayerName ?? "Player 2");
            await room.BroadcastAsync(writer.Buffer, writer.Position);

            // Broadcast initial world snapshot
            await room.BroadcastSnapshotAsync();
        }

        public void Tick(float dt)
        {
            foreach (var room in _rooms.Values)
            {
                // Tick the authoritative match simulation
                room.MatchEngine.Tick(dt);

                // Check disconnect grace period
                if (room.DisconnectGraceStartUtc.HasValue)
                {
                    double elapsed = (DateTime.UtcNow - room.DisconnectGraceStartUtc.Value).TotalSeconds;
                    if (elapsed >= NetworkProtocol.DisconnectGracePeriod)
                    {
                        Console.WriteLine($"[ROOM {room.RoomCode}] Disconnect grace period expired. Declaring forfeit.");
                        room.DisconnectGraceStartUtc = null;

                        int remainingPlayer = 1 - room.DisconnectedPlayerIndex;
                        _ = room.BroadcastOpCodeAsync(NetworkOpCode.MatchAbandoned, w =>
                        {
                            w.WriteInt32(remainingPlayer); // Remaining player is declared winner
                            w.WriteString("Opponent failed to reconnect within grace window.");
                        });
                    }
                }
            }
        }

        public void RemoveEmptyRooms()
        {
            foreach (var kvp in _rooms)
            {
                if (kvp.Value.IsEmpty)
                {
                    _rooms.TryRemove(kvp.Key, out _);
                    Console.WriteLine($"[ROOM MANAGER] Pruned empty room: {kvp.Key}");
                }
            }
        }

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Omit ambiguous 0/O, 1/I
            char[] code = new char[6];
            for (int i = 0; i < code.Length; i++)
            {
                code[i] = chars[_random.Next(chars.Length)];
            }
            return new string(code);
        }
    }
}
