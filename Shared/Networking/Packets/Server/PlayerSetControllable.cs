using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerSetControllable : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 31;


        public byte Index;
        public bool IsControllable;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            IsControllable = msg.ReadBoolean();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((bool)IsControllable);
        }
    }
}