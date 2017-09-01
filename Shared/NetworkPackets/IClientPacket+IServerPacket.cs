using Lidgren.Network;

namespace Jazz2.NetworkPackets
{
    public delegate void PacketCallback<T>(ref T p);

    public interface IClientPacket
    {
        void Read(NetIncomingMessage msg);
        void Write(NetOutgoingMessage msg);

        NetConnection SenderConnection { get; set; }
        byte Type { get; }
        bool SupportsUnconnected { get; }
    }

    public interface IServerPacket
    {
        void Read(NetIncomingMessage msg);
        void Write(NetOutgoingMessage msg);

        NetConnection SenderConnection { get; set; }
        byte Type { get; }
    }
}