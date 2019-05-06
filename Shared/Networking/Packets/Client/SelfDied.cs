using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct SelfDied : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 21;

        bool IClientPacket.SupportsUnconnected => false;


        public byte Index;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);
        }
    }
}