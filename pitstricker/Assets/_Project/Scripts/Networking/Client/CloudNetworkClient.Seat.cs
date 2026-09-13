using UnityEngine;

namespace PitStriker.Networking.Client
{
    public static class CloudNetworkClientSeat
    {
        public static int ResolvedLocalPlayerIndex(this CloudNetworkClient client)
        {
            if (client == null) return -1;
            if (client.LocalPlayerIndex == 0 || client.LocalPlayerIndex == 1)
                return client.LocalPlayerIndex;
            return -1;
        }
    }
}
