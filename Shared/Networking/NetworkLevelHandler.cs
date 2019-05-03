using System;
using System.Threading;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Game.UI;
using Jazz2.Networking.Packets;
using Jazz2.Networking.Packets.Client;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class NetworkLevelHandler : LevelHandler
    {
        private NetworkHandler network;
        private byte localPlayerIndex;
        private float lastUpdate;
        private long lastServerUpdateTime;

        private RemotePlayer[] remotePlayers = new RemotePlayer[256];

        public byte PlayerIndex => localPlayerIndex;

        public NetworkLevelHandler(App root, NetworkHandler network, LevelInitialization data, byte playerIndex) : base(root, data)
        {
            this.network = network;
            this.localPlayerIndex = playerIndex;

            network.OnUpdateAllPlayers += OnUpdateAllPlayers;
            network.RegisterCallback<CreateRemotePlayer>(OnCreateRemotePlayer);
            network.RegisterCallback<DestroyRemotePlayer>(OnDestroyRemotePlayer);
        }

        protected override void OnDisposing(bool manually)
        {
            if (network != null) {
                network.OnUpdateAllPlayers -= OnUpdateAllPlayers;
                network.RemoveCallback<CreateRemotePlayer>();
                network.RemoveCallback<DestroyRemotePlayer>();
            }

            base.OnDisposing(manually);
        }

        protected override void OnUpdate()
        {
            Hud.ShowDebugText("- Local Player Index: " + localPlayerIndex);
            Hud.ShowDebugText("- RTT: " + (int)(network.AverageRoundtripTime * 1000) + " ms");
            Hud.ShowDebugText("- Last Server Update: " + lastServerUpdateTime);

            float timeMult = Time.TimeMult;
            lastUpdate += timeMult;

            if (lastUpdate < 1.6f) {
                return;
            }

            lastUpdate = 0f;

            UpdateSelf updateSelfPacket = Players[0].CreateUpdatePacket();
            updateSelfPacket.Index = localPlayerIndex;
            updateSelfPacket.UpdateTime = (long)(NetTime.Now * 1000);
            network.Send(updateSelfPacket, 29, NetDeliveryMethod.Unreliable, PacketChannels.Main);

            base.OnUpdate();
        }

        private void OnUpdateAllPlayers(NetIncomingMessage msg)
        {
            msg.Position = 8; // Skip packet type

            long serverUpdateTime = msg.ReadInt64();
            if (lastServerUpdateTime > serverUpdateTime) {
                return;
            }

            lastServerUpdateTime = serverUpdateTime;

            byte playerCount = msg.ReadByte();
            for (int i = 0; i < playerCount; i++) {
                byte playerIndex = msg.ReadByte();
                byte flags = msg.ReadByte();
                if (flags == 0) { // Not spawned
                    continue;
                }

                Vector3 pos;
                {
                    ushort x = msg.ReadUInt16();
                    ushort y = msg.ReadUInt16();
                    ushort z = msg.ReadUInt16();
                    pos = new Vector3(x, y, z);
                }
                Vector2 speed;
                {
                    float x = msg.ReadInt16() * 0.002f;
                    float y = msg.ReadInt16() * 0.002f;
                    speed = new Vector2(x, y);
                }

                AnimState animState = (AnimState)msg.ReadUInt32();
                float animTime = msg.ReadFloat();
                bool isFacingLeft = msg.ReadBoolean();

                if (playerIndex == localPlayerIndex || remotePlayers[playerIndex] == null) {
                    continue;
                }

                float rtt = msg.SenderConnection.AverageRoundtripTime;
                pos.X += speed.X * rtt * 0.5f;
                pos.Y += speed.Y * rtt * 0.5f;

                remotePlayers[playerIndex].UpdateFromServer(pos, speed, animState, animTime, isFacingLeft);
            }
        }

        private void OnCreateRemotePlayer(ref CreateRemotePlayer p)
        {
            int index = p.Index;

            if (remotePlayers[index] != null) {
                //throw new InvalidOperationException();
                return;
            }

            PlayerType type = p.Type;
            Vector3 pos = p.Pos;

            Root.DispatchToMainThread(delegate {
                RemotePlayer player = new RemotePlayer();

                if (Interlocked.CompareExchange(ref remotePlayers[index], player, null) != null) {
                    return;
                }

                player.OnAttach(new ActorInstantiationDetails {
                    Api = Api,
                    Pos = pos,
                    Params = new ushort[] { (ushort)type, (ushort)index }
                });

                AddObject(player);
            });
        }

        private void OnDestroyRemotePlayer(ref DestroyRemotePlayer p)
        {
            int index = p.Index;

            Root.DispatchToMainThread(delegate {
                RemotePlayer player = Interlocked.Exchange(ref remotePlayers[index], null);
                if (player != null) {
                    RemoveObject(player);
                }
            });
        }
    }
}