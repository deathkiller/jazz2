using Lidgren.Network;

namespace Jazz2.Server
{
    public class ClientConnectedEventArgs
    {
        public readonly NetIncomingMessage Message;
        public bool Allow;

        public ClientConnectedEventArgs(NetIncomingMessage message)
        {
            Message = message;
        }
    }
}