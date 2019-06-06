#if MULTIPLAYER

using Lidgren.Network;

namespace Jazz2.Server.EventArgs
{
    public class ClientConnectedEventArgs
    {
        public readonly NetIncomingMessage Message;
        public string DenyReason;

        public ClientConnectedEventArgs(NetIncomingMessage message)
        {
            Message = message;
        }
    }
}

#endif