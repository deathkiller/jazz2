#if MULTIPLAYER

using Lidgren.Network;

namespace Jazz2.Server.EventArgs
{
    public class DiscoveryRequestEventArgs
    {
        public NetOutgoingMessage Message;
    }
}

#endif