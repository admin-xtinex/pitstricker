using System;
using System.Threading.Tasks;
using PitStriker.Networking.Shared;

namespace PitStrikerServer
{
    public class Room
    {
        public string RoomCode { get; }
        public ClientSession? Player0 { get; set; }
        public ClientSession? Player1 { get; set; }
        public AuthoritativeMatchEngine MatchEngine { get; }

        public bool IsFull => Player0 != null && Player1 != null;
        public bool IsEmpty => Player0 == null && Player1 == null;

        public DateTime? DisconnectGraceStartUtc { get; set; }
        public int DisconnectedPlayerIndex { get; set; } = -1;

        private readonly NetworkByteWriter _writer = new NetworkByteWriter(2048);

        public Room(string code)
        {
            RoomCode = code;
            MatchEngine = new AuthoritativeMatchEngine(this);
        }

        public bool AddPlayer(ClientSession session)
        {
            if (Player0 == null)
            {
                Player0 = session;
                session.RoomPlayerIndex = 0;
                session.CurrentRoom = this;
                return true;
            }
            else if (Player1 == null)
            {
                Player1 = session;
                session.RoomPlayerIndex = 1;
                session.CurrentRoom = this;
                return true;
            }
            return false;
        }

        public void RemovePlayer(ClientSession session)
        {
            if (Player0 == session)
            {
                Player0 = null;
            }
            else if (Player1 == session)
            {
                Player1 = null;
            }
            session.RoomPlayerIndex = -1;
            session.CurrentRoom = null;
        }

        public async Task BroadcastAsync(byte[] buffer, int length)
        {
            if (Player0?.IsConnected == true)
            {
                await Player0.SendAsync(buffer, length);
            }
            if (Player1?.IsConnected == true)
            {
                await Player1.SendAsync(buffer, length);
            }
        }

        public async Task BroadcastOpCodeAsync(NetworkOpCode opCode, Action<NetworkByteWriter>? payloadWriter = null)
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)opCode);
                payloadWriter?.Invoke(_writer);
                byte[] data = _writer.Buffer;
                int len = _writer.Position;
                _ = BroadcastAsync(data, len);
            }
        }

        public async Task BroadcastSnapshotAsync()
        {
            WorldSnapshotData snapshot = MatchEngine.CreateSnapshot();
            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteByte((byte)NetworkOpCode.WorldSnapshot);
                _writer.WriteWorldSnapshot(snapshot);
                byte[] data = _writer.Buffer;
                int len = _writer.Position;
                _ = BroadcastAsync(data, len);
            }
        }
    }
}
