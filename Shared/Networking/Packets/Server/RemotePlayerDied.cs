using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct RemotePlayerDied : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 21;


        public byte Index;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);
        }
    }
}