using Duality;
using Lidgren.Network;

namespace Jazz2.Networking.Packets.Server
{
    public struct CreateRemoteActor : IServerPacket
    {
        public NetConnection SenderConnection { get; set; }

        byte IServerPacket.Type => 13;

        public int Index;
        public string MetadataPath;
        public Actors.CollisionFlags CollisionFlags;
        public Vector3 Pos;

        void IServerPacket.Read(NetIncomingMessage msg)
        {
            Index = msg.ReadInt32();
            MetadataPath = msg.ReadString();
            CollisionFlags = (Actors.CollisionFlags)msg.ReadByte();

            float x = msg.ReadUInt16();
            float y = msg.ReadUInt16();
            float z = msg.ReadUInt16();
            Pos = new Vector3(x, y, z);
        }

        void IServerPacket.Write(NetOutgoingMessage msg)
        {
            msg.Write((int)Index);
            msg.Write((string)MetadataPath);
            msg.Write((byte)CollisionFlags);

            msg.Write((ushort)Pos.X);
            msg.Write((ushort)Pos.Y);
            msg.Write((ushort)Pos.Z);
        }
    }
}