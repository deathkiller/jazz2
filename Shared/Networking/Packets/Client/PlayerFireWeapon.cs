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

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();
            WeaponType = (WeaponType)msg.ReadByte();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);
            msg.Write((byte)WeaponType);
        }
    }
}