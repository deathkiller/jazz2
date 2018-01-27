#if MULTIPLAYER

using Lidgren.Network;

namespace Jazz2.Server
{
    public class DiscoveryRequestEventArgs
    {
        public NetOutgoingMessage Message;
    }
}

#endif