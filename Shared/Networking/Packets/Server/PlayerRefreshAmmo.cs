using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerRefreshAmmo : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 23;


        public byte Index;
        public WeaponType WeaponType;
        public short Count;
        public bool SwitchTo;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            WeaponType = (WeaponType)msg.ReadByte();
            Count = msg.ReadInt16();
            SwitchTo = msg.ReadBoolean();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)WeaponType);
            msg.Write((short)Count);
            msg.Write((bool)SwitchTo);
        }
    }
}