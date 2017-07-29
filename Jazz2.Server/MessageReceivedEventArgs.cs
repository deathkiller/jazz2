using Lidgren.Network;

namespace Jazz2.Server
{
    public class MessageReceivedEventArgs
    {
        public readonly NetIncomingMessage Message;
        public readonly bool IsUnconnected;

        public MessageReceivedEventArgs(NetIncomingMessage message, bool isUnconnected)
        {
            Message = message;
            IsUnconnected = isUnconnected;
        }
    }
}