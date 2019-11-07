using Duality;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerActivateSpring : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 22;


        public byte Index;
        public Vector2 Force;
        public bool KeepSpeedX;
        public bool KeepSpeedY;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Force.X = msg.ReadFloat();
            Force.Y = msg.ReadFloat();

            KeepSpeedX = msg.ReadBoolean();
            KeepSpeedY = msg.ReadBoolean();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((float)Force.X);
            msg.Write((float)Force.Y);

            msg.Write((bool)KeepSpeedX);
            msg.Write((bool)KeepSpeedY);
        }
    }
}