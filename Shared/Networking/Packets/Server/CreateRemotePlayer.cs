using Duality;
using Jazz2.Actors;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct CreateRemotePlayer : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 2;


        public int Index;
        public PlayerType Type;
        public Vector3 Pos;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Type = (PlayerType)msg.ReadByte();

            {
                float x = msg.ReadFloat();
                float y = msg.ReadFloat();
                float z = msg.ReadFloat();
                Pos = new Vector3(x, y, z);
            }
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)Type);

            msg.Write(Pos.X);
            msg.Write(Pos.Y);
            msg.Write(Pos.Z);
        }
    }
}