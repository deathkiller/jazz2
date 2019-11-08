using Jazz2.Actors;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerSetModifier : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 28;


        public byte Index;
        public Player.Modifier Modifier;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Modifier = (Player.Modifier)msg.ReadByte();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)Modifier);
        }
    }
}