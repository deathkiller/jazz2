using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct DestroyRemotableActor : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 32;

        bool IClientPacket.SupportsUnconnected => false;

        public int Index;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadInt32();
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((int)Index);
        }
    }
}