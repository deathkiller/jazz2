using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct LoadLevel : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 11;


        public string LevelName;
        public byte AssignedPlayerIndex;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            LevelName = msg.ReadString();
            AssignedPlayerIndex = msg.ReadByte();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write(LevelName);
            msg.Write((byte)AssignedPlayerIndex);
        }
    }
}