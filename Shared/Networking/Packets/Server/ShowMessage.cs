using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct ShowMessage : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 16;


        public byte Flags;
        public string Text;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Flags = msg.ReadByte();
            Text = msg.ReadString();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Flags);
            msg.Write((string)Text);
        }
    }
}