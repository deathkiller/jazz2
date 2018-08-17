using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Client;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class NetworkLevelHandler : LevelHandler
    {
        private NetworkHandler network;
        private byte playerIndex;
        private float lastUpdate;

        private Dictionary<int, RemotePlayer> remotePlayers = new Dictionary<int, RemotePlayer>();

        public byte PlayerIndex => playerIndex;

        public NetworkLevelHandler(App root, NetworkHandler network, LevelInitialization data, byte playerIndex) : base(root, data)
        {
            this.network = network;
            this.playerIndex = playerIndex;

            rootObject.AddComponent(new LocalController(this));

            network.OnDisconnected += OnDisconnected;
            network.RegisterCallback<CreateRemotePlayer>(OnCreateRemotePlayer);
            network.RegisterCallback<UpdateRemotePlayer>(OnUpdateRemotePlayer);
            network.RegisterCallback<DestroyRemotePlayer>(OnDestroyRemotePlayer);

            if (!network.IsConnected) {
                OnDisconnected();
            }
        }

        protected override void OnDisposing(bool manually)
        {
            if (network != null) {
                network.OnDisconnected -= OnDisconnected;
                network.RemoveCallback<CreateRemotePlayer>();
                network.RemoveCallback<UpdateRemotePlayer>();
                network.RemoveCallback<DestroyRemotePlayer>();
            }

            base.OnDisposing(manually);
        }

        private void OnUpdate()
        {
            float timeMult = Time.TimeMult;
            lastUpdate += timeMult;

            if (lastUpdate < 1.2f) {
                return;
            }

            lastUpdate = 0f;

            Player player = Players[0];

            UpdateSelf updateSelfPacket = player.CreateUpdatePacket();
            updateSelfPacket.Index = playerIndex;
            network.Send(updateSelfPacket, 2 + 6 * 4, NetDeliveryMethod.UnreliableSequenced, NetworkChannels.PlayerUpdate);
        }

        private void OnDisconnected()
        {
            Root.DispatchToMainThread(delegate {
                Root.ShowMainMenu();
            });
        }

        private void OnCreateRemotePlayer(ref CreateRemotePlayer p)
        {
            int index = p.Index;

            if (remotePlayers.ContainsKey(index)) {
                throw new InvalidOperationException();
            }

            PlayerType type = p.Type;
            Vector3 pos = p.Pos;

            Root.DispatchToMainThread(delegate {
                RemotePlayer player = new RemotePlayer();
                player.OnAttach(new ActorInstantiationDetails {
                    Api = Api,
                    Pos = pos,
                    Params = new ushort[] { (ushort)type, (ushort)index }
                });
                remotePlayers[index] = player;

                AddObject(player);
            });

            Console.WriteLine(" | RemotePlayer spawned: " + index);
        }

        private void OnUpdateRemotePlayer(ref UpdateRemotePlayer p)
        {
            RemotePlayer player;
            if (remotePlayers.TryGetValue(p.Index, out player)) {
                player.UpdateFromServer(ref p);
            }
        }

        private void OnDestroyRemotePlayer(ref DestroyRemotePlayer p)
        {
            int index = p.Index;

            RemotePlayer player;
            if (remotePlayers.TryGetValue(index, out player)) {
                Root.DispatchToMainThread(delegate {
                    remotePlayers.Remove(index);

                    RemoveObject(player);
                });

                Console.WriteLine(" | RemotePlayer destroyed: " + index);
            }
        }

        private class LocalController : Component, ICmpUpdatable
        {
            private readonly NetworkLevelHandler levelHandler;

            public LocalController(NetworkLevelHandler levelHandler)
            {
                this.levelHandler = levelHandler;
            }

            void ICmpUpdatable.OnUpdate()
            {
                levelHandler.OnUpdate();
            }
        }
    }
}