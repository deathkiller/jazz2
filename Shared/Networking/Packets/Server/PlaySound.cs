using Duality;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct PlaySound : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 17;


        public int Index;
        public string SoundName;
        public Vector3 Pos;
        public float Gain;
        public float Pitch;
        public float Lowpass;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadByte();

            SoundName = msg.ReadString();

            {
                float x = msg.ReadUInt16() * 0.4f;
                float y = msg.ReadUInt16() * 0.4f;
                float z = msg.ReadUInt16() * 0.4f;
                Pos = new Vector3(x, y, z);
            }

            Gain = msg.ReadRangedSingle(0f, 20f, 8);
            Pitch = msg.ReadRangedSingle(0f, 20f, 8);
            Lowpass = msg.ReadRangedSingle(0f, 1f, 8);
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((byte)Index);

            msg.Write((string)SoundName);

            msg.Write((ushort)(Pos.X * 2.5f));
            msg.Write((ushort)(Pos.Y * 2.5f));
            msg.Write((ushort)(Pos.Z * 2.5f));

            msg.WriteRangedSingle(Gain, 0f, 20f, 8);
            msg.WriteRangedSingle(Pitch, 0f, 20f, 8);
            msg.WriteRangedSingle(Lowpass, 0f, 1f, 8);
        }
    }
}