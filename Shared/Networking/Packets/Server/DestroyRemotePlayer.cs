using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct DestroyRemotePlayer : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 14;


        public int Index;
        public byte Reason;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Reason = msg.ReadByte();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)Reason);
        }
    }
}