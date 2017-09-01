using Lidgren.Network;

namespace Jazz2.NetworkPackets.Client
{
    public struct LevelReady : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 1;

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