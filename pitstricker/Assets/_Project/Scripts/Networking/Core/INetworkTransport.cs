using System;
using PitStriker.Networking.Shared;

namespace PitStriker.Networking.Core
{
    /// <summary>
    /// Abstract transport interface decoupling game and session logic from the underlying network protocol.
    /// Supports WebSocket, UDP, KCP, etc.
    /// </summary>
    public interface INetworkTransport
    {
        event Action OnConnected;
        event Action<string> OnDisconnected;
        event Action<byte[], int> OnDataReceived;
        event Action<string> OnError;

        NetworkConnectionState State { get; }
        float RttMilliseconds { get; }

        void Connect(string address);
        void Disconnect();
        void Send(byte[] buffer, int length, NetworkDelivery delivery = NetworkDelivery.Reliable);
        void Tick();
    }
}
