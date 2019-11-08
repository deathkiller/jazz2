using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerRefreshWeaponUpgrades : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 26;


        public byte Index;
        public WeaponType WeaponType;
        public byte Upgrades;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            WeaponType = (WeaponType)msg.ReadByte();
            Upgrades = msg.ReadByte();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)WeaponType);
            msg.Write((byte)Upgrades);
        }
    }
}