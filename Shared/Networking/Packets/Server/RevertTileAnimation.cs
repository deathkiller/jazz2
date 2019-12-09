using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct RevertTileAnimation : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 51;


        public int TileX;
        public int TileY;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            TileX = msg.ReadUInt16();
            TileY = msg.ReadUInt16();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((ushort)TileX);
            msg.Write((ushort)TileY);
        }
    }
}