using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using PitStriker.Networking.Shared;

namespace PitStrikerServer
{
    public class ClientSession
    {
        public string SessionId { get; }
        public string ReconnectToken { get; }
        public WebSocket? Socket { get; set; }
        public string PlayerName { get; set; } = "Player";
        public int RoomPlayerIndex { get; set; } = -1; // 0 or 1
        public Room? CurrentRoom { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public bool IsConnected => Socket != null && Socket.State == WebSocketState.Open;

        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public ClientSession(string sessionId, WebSocket socket, string name)
        {
            SessionId = sessionId;
            ReconnectToken = Guid.NewGuid().ToString("N");
            Socket = socket;
            PlayerName = name;
            LastSeenUtc = DateTime.UtcNow;
        }

        public async Task SendAsync(byte[] data, int length)
        {
            if (Socket == null || Socket.State != WebSocketState.Open) return;

            await _sendLock.WaitAsync();
            try
            {
                if (Socket != null && Socket.State == WebSocketState.Open)
                {
                    await Socket.SendAsync(new ArraySegment<byte>(data, 0, length), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{SessionId}] Send error: {ex.Message}");
            }
            finally
            {
                _sendLock.Release();
            }
        }
    }
}
