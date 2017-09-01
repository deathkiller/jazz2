using Duality;
using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.NetworkPackets.Client
{
    public struct UpdateSelf : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 2;

        bool IClientPacket.SupportsUnconnected => false;


        public int Index;

        public Vector3 Pos;
        public Vector3 Speed;

        public AnimState AnimState;
        public float AnimTime;
        public bool IsFacingLeft;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            {
                float x = msg.ReadFloat();
                float y = msg.ReadFloat();
                float z = msg.ReadFloat();
                Pos = new Vector3(x, y, z);
            }

            {
                float x = msg.ReadFloat();
                float y = msg.ReadFloat();
                float z = msg.ReadFloat();
                Speed = new Vector3(x, y, z);
            }

            AnimState = (AnimState)msg.ReadUInt16();
            AnimTime = msg.ReadFloat();
            IsFacingLeft = msg.ReadBoolean();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write(Pos.X);
            msg.Write(Pos.Y);
            msg.Write(Pos.Z);

            msg.Write(Speed.X);
            msg.Write(Speed.Y);
            msg.Write(Speed.Z);

            msg.Write((ushort)AnimState);
            msg.Write((float)AnimTime);
            msg.Write((bool)IsFacingLeft);
        }
    }
}