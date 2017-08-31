#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.NetworkPackets;
using Jazz2.NetworkPackets.Client;
using Jazz2.NetworkPackets.Server;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class NetworkLevelHandler : LevelHandler
    {
        private NetworkHandler network;
        private int playerIndex;
        private int lastUpdate;

        private Dictionary<int, RemotePlayer> remotePlayers = new Dictionary<int, RemotePlayer>();

        public int PlayerIndex => playerIndex;

        public NetworkLevelHandler(Controller root, NetworkHandler network, LevelInitialization data, int playerIndex) : base(root, data)
        {
            this.network = network;
            this.playerIndex = playerIndex;

            rootObject.AddComponent(new LocalController(this));

            network.RegisterCallback<CreateRemotePlayer>(OnCreateRemotePlayer);
            network.RegisterCallback<UpdateRemotePlayer>(OnUpdateRemotePlayer);
            network.RegisterCallback<DestroyRemotePlayer>(OnDestroyRemotePlayer);
        }

        private void OnUpdate()
        {
            if (Time.FrameCount - lastUpdate < 2) {
                return;
            }

            lastUpdate = Time.FrameCount;

            Player player = Players[0];

            network.Send(new UpdateSelf {
                Index = playerIndex,

                Pos = player.Transform.Pos,
                Speed = player.Speed,

                AnimState = player.ActiveAnimState,
                AnimTime = player.ActimeAnimTime,
                IsFacingLeft = player.IsFacingLeft
            }, 2 + 6 * 4, NetDeliveryMethod.ReliableSequenced, NetworkChannels.PlayerUpdate);

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

#endif