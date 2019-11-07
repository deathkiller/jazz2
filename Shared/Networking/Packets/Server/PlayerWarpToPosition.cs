using Duality;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerWarpToPosition : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 24;


        public byte Index;
        public Vector2 Pos;
        public bool Fast;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            ushort x = msg.ReadUInt16();
            ushort y = msg.ReadUInt16();
            Pos = new Vector2(x, y);

            Fast = msg.ReadBoolean();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((ushort)Pos.X);
            msg.Write((ushort)Pos.Y);

            msg.Write((bool)Fast);
        }
    }
}