using Duality;
using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct PlayerFireWeapon : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 36;

        bool IClientPacket.SupportsUnconnected => false;

        public byte Index;
        public WeaponType WeaponType;
        public Vector3 InitialPos;
        public Vector3 GunspotPos;
        public float Angle;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();
            WeaponType = (WeaponType)msg.ReadByte();

            InitialPos.X = msg.ReadUInt16();
            InitialPos.Y = msg.ReadUInt16();
            InitialPos.Z = msg.ReadUInt16();

            sbyte diffX = msg.ReadSByte();
            sbyte diffY = msg.ReadSByte();
            GunspotPos.X = InitialPos.X + diffX;
            GunspotPos.Y = InitialPos.Y + diffY;
            GunspotPos.Z = InitialPos.Z;

            Angle = msg.ReadRangedSingle(0f, MathF.TwoPi, 16);
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);
            msg.Write((byte)WeaponType);

            msg.Write((ushort)InitialPos.X);
            msg.Write((ushort)InitialPos.Y);
            msg.Write((ushort)InitialPos.Z);

            float diffX = (GunspotPos.X - InitialPos.X);
            float diffY = (GunspotPos.Y - InitialPos.Y);
            msg.Write((sbyte)diffX);
            msg.Write((sbyte)diffY);

            msg.WriteRangedSingle(Angle, 0f, MathF.TwoPi, 16);
        }
    }
}