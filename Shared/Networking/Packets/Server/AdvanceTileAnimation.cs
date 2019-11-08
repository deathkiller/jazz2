using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct AdvanceTileAnimation : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 50;


        public int TileX;
        public int TileY;
        public int Amount;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            TileX = msg.ReadUInt16();
            TileY = msg.ReadUInt16();
            Amount = msg.ReadByte();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((ushort)TileX);
            msg.Write((ushort)TileY);
            msg.Write((byte)Amount);
        }
    }
}