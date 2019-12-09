using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct SetTrigger : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 52;


        public ushort TriggerID;
        public bool NewState;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            TriggerID = msg.ReadUInt16();
            NewState = msg.ReadBoolean();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((ushort)TriggerID);
            msg.Write((bool)NewState);
        }
    }
}