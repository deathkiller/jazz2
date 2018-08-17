#if MULTIPLAYER

using Lidgren.Network;

namespace Jazz2.Server.EventArgs
{
    public class ClientStatusChangedEventArgs
    {
        public readonly NetConnection SenderConnection;
        public readonly NetConnectionStatus Status;

        public ClientStatusChangedEventArgs(NetConnection connection, NetConnectionStatus status)
        {
            SenderConnection = connection;
            Status = status;
        }
    }
}

#endif