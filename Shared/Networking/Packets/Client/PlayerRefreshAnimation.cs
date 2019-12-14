using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct PlayerRefreshAnimation : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 35;

        bool IClientPacket.SupportsUnconnected => false;

        public byte Index;
        public string Identifier;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();
            Identifier = msg.ReadString();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);
            msg.Write((string)Identifier);
        }
    }
}