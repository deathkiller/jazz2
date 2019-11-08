using Duality;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerActivateForce : IServerPacket
    {
        public enum ForceType
        {
            Unspecified,
            Spring,
            PinballBumper,
            PinballPaddle
        }

        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 22;


        public byte Index;
        public ForceType ActivatedBy;
        public Vector2 Force;
        public bool KeepSpeedX;
        public bool KeepSpeedY;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            ActivatedBy = (ForceType)msg.ReadByte();
            Force.X = msg.ReadFloat();
            Force.Y = msg.ReadFloat();

            KeepSpeedX = msg.ReadBoolean();
            KeepSpeedY = msg.ReadBoolean();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)ActivatedBy);
            msg.Write((float)Force.X);
            msg.Write((float)Force.Y);

            msg.Write((bool)KeepSpeedX);
            msg.Write((bool)KeepSpeedY);
        }
    }
}