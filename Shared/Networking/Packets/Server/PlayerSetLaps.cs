using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerSetLaps : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 25;


        public byte Index;
        public int Laps;
        public int LapsTotal;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Laps = msg.ReadByte();
            LapsTotal = msg.ReadByte();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)Laps);
            msg.Write((byte)LapsTotal);
        }
    }
}