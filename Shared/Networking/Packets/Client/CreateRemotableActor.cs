using Duality;
using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Client
{
    public struct CreateRemotableActor : IClientPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IClientPacket.Type => 30;

        bool IClientPacket.SupportsUnconnected => false;

        public int Index;

        public EventType EventType;
        public ushort[] EventParams;

        public Vector3 Pos;

        void IClientPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadInt32();

            EventType = (EventType)msg.ReadUInt16();

            EventParams = new ushort[8];
            for (int i = 0; i < 8; i++) {
                EventParams[i] = msg.ReadUInt16();
            }

            {
                ushort x = msg.ReadUInt16();
                ushort y = msg.ReadUInt16();
                ushort z = msg.ReadUInt16();
                Pos = new Vector3(x, y, z);
            }
        }

        void IClientPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((int)Index);

            msg.Write((ushort)EventType);

            int length = (EventParams == null ? 0 : MathF.Min(EventParams.Length, 8));

            int i;
            for (i = 0; i < length; i++) {
                msg.Write((ushort)EventParams[i]);
            }
            for (; i < 8; i++) {
                msg.Write((ushort)0);
            }

            msg.Write((ushort)Pos.X);
            msg.Write((ushort)Pos.Y);
            msg.Write((ushort)Pos.Z);
        }
    }
}