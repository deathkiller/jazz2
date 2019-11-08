using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerSetDizzyTime : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 27;


        public byte Index;
        public float Time;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Time = msg.ReadSingle();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((float)Time);
        }
    }
}