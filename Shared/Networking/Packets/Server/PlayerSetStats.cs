using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerSetStats : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 30;


        public byte Index;
        public int Kills;
        public int Deaths;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Kills = msg.ReadUInt16();
            Deaths = msg.ReadUInt16();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((ushort)Kills);
            msg.Write((ushort)Deaths);
        }
    }
}