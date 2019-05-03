using Duality;
using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct UpdateSelf : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 12;

        bool IClientPacket.SupportsUnconnected => false;

        public byte Index;

        public long UpdateTime;

        public Vector3 Pos;
        public Vector2 Speed;

        public AnimState AnimState;
        public float AnimTime;
        public bool IsFacingLeft;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            UpdateTime = msg.ReadInt64();

            {
                ushort x = msg.ReadUInt16();
                ushort y = msg.ReadUInt16();
                ushort z = msg.ReadUInt16();
                Pos = new Vector3(x, y, z);
            }

            {
                float x = msg.ReadInt16() * 0.002f;
                float y = msg.ReadInt16() * 0.002f;
                Speed = new Vector2(x, y);
            }

            AnimState = (AnimState)msg.ReadUInt32();
            AnimTime = msg.ReadFloat();
            IsFacingLeft = msg.ReadBoolean();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((long)UpdateTime);

            msg.Write((ushort)Pos.X);
            msg.Write((ushort)Pos.Y);
            msg.Write((ushort)Pos.Z);

            msg.Write((short)(Speed.X * 500f));
            msg.Write((short)(Speed.Y * 500f));

            msg.Write((uint)AnimState);
            msg.Write((float)AnimTime);
            msg.Write((bool)IsFacingLeft);
        }
    }
}