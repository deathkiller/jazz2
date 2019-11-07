using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerTakeDamage : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 20;


        public byte Index;
        public byte Amount;
        public float PushForce;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            Amount = msg.ReadByte();
            PushForce = msg.ReadSingle();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)Amount);
            msg.Write((float)PushForce);
        }
    }
}