using Duality;
using Jazz2.Actors;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct PlayerUpdate : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 12;

        bool IClientPacket.SupportsUnconnected => false;

        public byte Index;

        public long UpdateTime;

        public Vector3 Pos;
        public Vector3 Speed;

        //public AnimState AnimState;
        //public float AnimTime;
        public Player.SpecialMoveType CurrentSpecialMove;
        public bool IsVisible;
        public bool IsFacingLeft;
        public bool IsActivelyPushing;


        //public bool Controllable;
        //public bool IsFirePressed;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            UpdateTime = msg.ReadInt64();

            {
                float x = msg.ReadUInt16() * 0.4f;
                float y = msg.ReadUInt16() * 0.4f;
                float z = msg.ReadUInt16() * 0.4f;
                Pos = new Vector3(x, y, z);
            }

            {
                float x = msg.ReadSByte() * 0.5f;
                float y = msg.ReadSByte() * 0.5f;
                Speed = new Vector3(x, y, 0f);
            }

            CurrentSpecialMove = (Player.SpecialMoveType)msg.ReadByte();

            //AnimState = (AnimState)msg.ReadUInt32();
            //AnimTime = msg.ReadFloat();
            IsVisible = msg.ReadBoolean();
            IsFacingLeft = msg.ReadBoolean();
            IsActivelyPushing = msg.ReadBoolean();

            //Controllable = msg.ReadBoolean();
            //IsFirePressed = msg.ReadBoolean();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((long)UpdateTime);

            msg.Write((ushort)(Pos.X * 2.5f));
            msg.Write((ushort)(Pos.Y * 2.5f));
            msg.Write((ushort)(Pos.Z * 2.5f));

            msg.Write((sbyte)(Speed.X * 2f));
            msg.Write((sbyte)(Speed.Y * 2f));
            // Speed.Z is dropped

            msg.Write((byte)CurrentSpecialMove);

            //msg.Write((uint)AnimState);
            //msg.Write((float)AnimTime);
            msg.Write((bool)IsVisible);
            msg.Write((bool)IsFacingLeft);
            msg.Write((bool)IsActivelyPushing);

            //msg.Write((bool)Controllable);
            //msg.Write((bool)IsFirePressed);
        }
    }
}