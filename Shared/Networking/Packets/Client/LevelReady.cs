using Jazz2.Actors;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct LevelReady : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 11;

        bool IClientPacket.SupportsUnconnected => false;


        public byte Index;
        public PlayerType PlayerType;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            PlayerType = (PlayerType)msg.ReadByte();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)PlayerType);
        }
    }
}