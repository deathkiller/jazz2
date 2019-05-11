using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct RemotePlayerHit : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 22;

        bool IClientPacket.SupportsUnconnected => false;


        public byte Index;
        public byte Damage;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();
            Damage = msg.ReadByte();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);
            msg.Write((byte)Damage);
        }
    }
}