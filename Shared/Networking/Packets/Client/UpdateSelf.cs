using Duality;
using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct UpdateSelf : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 2;

        bool IClientPacket.SupportsUnconnected => false;


        public byte Index;

        public Vector3 Pos;
        //public ushort X, Y, Z;
        public Vector3 Speed;

        public AnimState AnimState;
        public float AnimTime;
        public bool IsFacingLeft;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            {
                //float x = msg.ReadFloat();
                //float y = msg.ReadFloat();
                //float z = msg.ReadFloat();
                ushort x = msg.ReadUInt16();
                ushort y = msg.ReadUInt16();
                ushort z = msg.ReadUInt16();
                Pos = new Vector3(x, y, z);
            }

            {
                //float x = msg.ReadFloat();
                //float y = msg.ReadFloat();
                //float z = msg.ReadFloat();
                float x = msg.ReadInt16() * 0.002f;
                float y = msg.ReadInt16() * 0.002f;
                float z = msg.ReadInt16() * 0.002f;
                Speed = new Vector3(x, y, z);
            }

            AnimState = (AnimState)msg.ReadUInt16();
            AnimTime = msg.ReadFloat();
            IsFacingLeft = msg.ReadBoolean();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            //msg.Write(Pos.X);
            //msg.Write(Pos.Y);
            //msg.Write(Pos.Z);
            msg.Write((ushort)Pos.X);
            msg.Write((ushort)Pos.Y);
            msg.Write((ushort)Pos.Z);

            //msg.Write(Speed.X);
            //msg.Write(Speed.Y);
            //msg.Write(Speed.Z);
            msg.Write((short)(Speed.X * 500f));
            msg.Write((short)(Speed.Y * 500f));
            msg.Write((short)(Speed.Z * 500f));

            msg.Write((ushort)AnimState);
            msg.Write((float)AnimTime);
            msg.Write((bool)IsFacingLeft);
        }
    }
}