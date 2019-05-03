using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct LevelReady : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 11;

        bool IClientPacket.SupportsUnconnected => false;


        public int Index;

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