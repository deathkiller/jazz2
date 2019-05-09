using Duality;
using Jazz2.Game.Structs;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct CreateRemoteObject : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 15;

        public int Index;

        public EventType EventType;
        public ushort[] EventParams;

        public Vector3 Pos;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadInt32();

            EventType = (EventType)msg.ReadUInt16();

            EventParams = new ushort[8];
            for (int i = 0; i < 8; i++) {
                EventParams[i] = msg.ReadUInt16();
            }

            float x = msg.ReadUInt16();
            float y = msg.ReadUInt16();
            float z = msg.ReadUInt16();
            Pos = new Vector3(x, y, z);
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((int)Index);

            msg.Write((ushort)EventType);

            int length = MathF.Min(EventParams.Length, 8);

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