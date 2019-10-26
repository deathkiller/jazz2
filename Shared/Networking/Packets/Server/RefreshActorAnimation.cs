using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct RefreshActorAnimation : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 50;

        public int Index;
        public string Identifier;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadInt32();
            Identifier = msg.ReadString();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((int)Index);
            msg.Write(Identifier);
        }
    }
}