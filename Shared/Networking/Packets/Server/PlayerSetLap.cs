using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerSetLap : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 25;


        public byte Index;
        public int Lap;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Lap = msg.ReadUInt16();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((ushort)Lap);
        }
    }
}