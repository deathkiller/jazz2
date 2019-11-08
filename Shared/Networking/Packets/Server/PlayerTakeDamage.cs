using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlayerTakeDamage : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 20;


        public byte Index;
        public byte HealthBefore;
        public byte DamageAmount;
        public float PushForce;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            HealthBefore = msg.ReadByte();
            DamageAmount = msg.ReadByte();
            PushForce = msg.ReadSingle();
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((byte)HealthBefore);
            msg.Write((byte)DamageAmount);
            msg.Write((float)PushForce);
        }
    }
}