using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct DestroyRemoteObject : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 16;

        public int Index;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadInt32();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((int)Index);
        }
    }
}