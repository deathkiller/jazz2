using System.Collections.Generic;
using System.Threading;
using Duality;
using Duality.Async;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Game.UI;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Client;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Game.Multiplayer
{
    public class NetworkLevelHandler : LevelHandler
    {
        private NetworkHandler net;
        private byte localPlayerIndex;
        private float lastUpdate;
        private long lastServerUpdateTime;
        private int isStillLoading;

        private RemotePlayer[] remotePlayers = new RemotePlayer[256];

        private Dictionary<int, IRemotableActor> localRemotableActors = new Dictionary<int, IRemotableActor>();
        private Dictionary<int, RemoteActor> remoteActors = new Dictionary<int, RemoteActor>();
        //private int lastRemotableActorIndex;

        public byte PlayerIndex => localPlayerIndex;

        public NetworkLevelHandler(App root, NetworkHandler net, LevelInitialization data, byte playerIndex) : base(root, data)
        {
            this.net = net;
            this.localPlayerIndex = playerIndex;

            net.OnUpdateAllPlayers += OnUpdateAllPlayers;
            net.AddCallback<CreateControllablePlayer>(OnCreateControllablePlayer);
            net.AddCallback<CreateRemotePlayer>(OnCreateRemotePlayer);
            net.AddCallback<DestroyRemotePlayer>(OnDestroyRemotePlayer);
            net.AddCallback<CreateRemoteActor>(OnCreateRemoteObject);
            net.AddCallback<DestroyRemoteActor>(OnDestroyRemoteObject);
            net.AddCallback<DecreasePlayerHealth>(OnDecreasePlayerHealth);
            net.AddCallback<RemotePlayerDied>(OnRemotePlayerDied);

            net.AddCallback<RefreshActorAnimation>(OnRefreshActorAnimation);

            // Wait 3 frames and then inform server that loading is complete
            isStillLoading = 3;
        }

        protected override void OnDisposing(bool manually)
        {
            if (net != null) {
                net.OnUpdateAllPlayers -= OnUpdateAllPlayers;
                net.RemoveCallback<CreateControllablePlayer>();
                net.RemoveCallback<CreateRemotePlayer>();
                net.RemoveCallback<DestroyRemotePlayer>();
                net.RemoveCallback<CreateRemoteActor>();
                net.RemoveCallback<DestroyRemoteActor>();
                net.RemoveCallback<DecreasePlayerHealth>();
                net.RemoveCallback<RemotePlayerDied>();

                net.RemoveCallback<RefreshActorAnimation>();
            }

            base.OnDisposing(manually);
        }

        protected override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            // ToDo: This workaround should be removed
            if (isStillLoading > 0) {
                isStillLoading--;

                if (isStillLoading <= 0) {
                    net.Send(new LevelReady {
                        Index = localPlayerIndex
                    }, 2, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }
            }

#if DEBUG
            Hud.ShowDebugText("- Local Player Index: " + localPlayerIndex);
            Hud.ShowDebugText("- RTT: " + (int)(net.AverageRoundtripTime * 1000) + " ms / Up: " + net.Up + " / Down: " + net.Down);
            Hud.ShowDebugText("- Last Server Update: " + lastServerUpdateTime);
            Hud.ShowDebugText("- Remote Objects: " + localRemotableActors.Count + " / " + remoteActors.Count);
#endif

            if (players.Count > 0) {
                lastUpdate += timeMult;

                if (lastUpdate < 1.4f) {
                    return;
                }

                lastUpdate = 0f;

                long updateTime = (long)(NetTime.Now * 1000);

                // Send update to server
                UpdateSelf updateSelfPacket = players[0].CreateUpdatePacket();
                updateSelfPacket.Index = localPlayerIndex;
                updateSelfPacket.UpdateTime = updateTime;
                net.Send(updateSelfPacket, 29, NetDeliveryMethod.Unreliable, PacketChannels.UnreliableUpdates);

                // ToDo
                /*foreach (KeyValuePair<int, IRemotableActor> pair in localRemotableActors) {
                    UpdateRemotableActor p = new UpdateRemotableActor();
                    pair.Value.OnUpdateRemotableActor(ref p);
                    p.Index = pair.Key;
                    p.UpdateTime = updateTime;
                    net.Send(p, 32, NetDeliveryMethod.Unreliable, PacketChannels.Main);
                }*/
            }
        }

        public override bool HandlePlayerDied(Player player)
        {
            net.Send(new SelfDied {
                Index = localPlayerIndex
            }, 2, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);

            return false;
        }

        public override void BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams)
        {
            if (eventType == EventType.ModifierHurt) {
                net.Send(new RemotePlayerHit {
                    Index = (byte)eventParams[0],
                    Damage = (byte)eventParams[1]
                }, 3, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                return;
            }

            base.BroadcastTriggeredEvent(eventType, eventParams);
        }

        public override void AddActor(ActorBase actor)
        {
            base.AddActor(actor);

            // ToDo
            /*IRemotableActor remotableActor = actor as IRemotableActor;
            if (remotableActor == null || remotableActor.Index != 0) {
                return;
            }

            int actorIndex = localPlayerIndex | (lastRemotableActorIndex << 8);

            remotableActor.Index = actorIndex;
            localRemotableActors[actorIndex] = remotableActor;

            CreateRemotableActor p = new CreateRemotableActor();
            remotableActor.OnCreateRemotableActor(ref p);
            p.Index = actorIndex;
            net.Send(p, 13, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);

            lastRemotableActorIndex++;*/
        }

        public override void RemoveActor(ActorBase actor)
        {
            base.RemoveActor(actor);

            /*IRemotableActor remotableActor = actor as IRemotableActor;
            if (remotableActor == null || (remotableActor.Index & 0xff) != localPlayerIndex) {
                return;
            }

            int actorIndex = remotableActor.Index;
            if (!localRemotableActors.Remove(actorIndex)) {
                return;
            }

            DestroyRemotableActor p = new DestroyRemotableActor();
            p.Index = actorIndex;
            net.Send(p, 5, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);*/
        }

        /// <summary>
        /// Player should update state and position of all other players and objects
        /// This type of packet is sent very often, so it's handled differently
        /// </summary>
        private void OnUpdateAllPlayers(NetIncomingMessage msg)
        {
            msg.Position = 8; // Skip packet type

            long serverUpdateTime = msg.ReadInt64();
            if (lastServerUpdateTime > serverUpdateTime) {
                return;
            }

            lastServerUpdateTime = serverUpdateTime;

            float rtt = msg.SenderConnection.AverageRoundtripTime;

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

                AnimState animState = (AnimState)msg.ReadUInt32();
                float animTime = msg.ReadFloat();
                bool isFacingLeft = msg.ReadBoolean();

                if (playerIndex == localPlayerIndex || remotePlayers[playerIndex] == null) {
                    continue;
                }

                remotePlayers[playerIndex].UpdateFromServer(pos, animState, animTime, isFacingLeft);
            }

            int objectCount = msg.ReadInt32();
            for (int i = 0; i < objectCount; i++) {
                int objectIndex = msg.ReadInt32();
                byte flags = msg.ReadByte();

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

                bool visible = msg.ReadBoolean();
                bool isFacingLeft = msg.ReadBoolean();

                RemoteActor actor;
                if (remoteActors.TryGetValue(objectIndex, out actor)) {
                    pos.X += speed.X * rtt;
                    pos.Y += speed.Y * rtt;

                    actor.OnUpdateRemoteActor(pos, speed, visible, isFacingLeft);
                }
            }
        }

        /// <summary>
        /// Player is allowed to join game, controllable actor should be created
        /// </summary>
        private void OnCreateControllablePlayer(ref CreateControllablePlayer p)
        {
            // ToDo: throw on mismatch?
            localPlayerIndex = p.Index;

            PlayerType type = p.Type;
            Vector3 pos = p.Pos;
            byte health = p.Health;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    Player oldPlayer = players[0];
                    if (oldPlayer.PlayerType == type) {
                        oldPlayer.Respawn(pos.Xy);
                        oldPlayer.Health = health;
                        return;
                    }

                    RemoveActor(oldPlayer);
                    Players.Remove(oldPlayer);
                }

                Player player = new Player();
                player.OnActivated(new ActorActivationDetails {
                    LevelHandler = this,
                    Pos = pos,
                    Params = new[] { (ushort)type, (ushort)0 }
                });
                player.Health = health;
                AddPlayer(player);

                cameras[0].Transform.Pos = new Vector3(pos.Xy, 0);
                cameras[0].GetComponent<CameraController>().TargetObject = player;

                Hud hud = rootObject.AddComponent<Hud>();
                hud.LevelHandler = this;
                player.AttachToHud(hud);

                //player.ReceiveLevelCarryOver(data.ExitType, ref data.PlayerCarryOvers[i]);
            });
        }

        /// <summary>
        /// Another (remote) player was created, so count with him
        /// </summary>
        private void OnCreateRemotePlayer(ref CreateRemotePlayer p)
        {
            byte index = p.Index;

            if (remotePlayers[index] != null) {
                //throw new InvalidOperationException();
                return;
            }

            PlayerType type = p.Type;
            Vector3 pos = p.Pos;

            Await.NextAfterUpdate().OnCompleted(() => {
                RemotePlayer player = new RemotePlayer();

                if (Interlocked.CompareExchange(ref remotePlayers[index], player, null) != null) {
                    return;
                }

                player.OnActivated(new ActorActivationDetails {
                    LevelHandler = this,
                    Pos = pos,
                    Params = new ushort[] { (ushort)type, (ushort)index }
                });

                //AddObject(player);
                AddActor(player);
            });
        }

        /// <summary>
        /// Some remote player was destroyed (probably disconnect)
        /// </summary>
        private void OnDestroyRemotePlayer(ref DestroyRemotePlayer p)
        {
            byte index = p.Index;

            Await.NextAfterUpdate().OnCompleted(() => {
                RemotePlayer player = Interlocked.Exchange(ref remotePlayers[index], null);
                if (player != null) {
                    //RemoveObject(player);
                    RemoveActor(player);
                }
            });
        }

        /// <summary>
        /// Some remote object was created
        /// </summary>
        private void OnCreateRemoteObject(ref CreateRemoteActor p)
        {
            int index = p.Index;

            if ((index & 0xff) == localPlayerIndex) {
                return;
            }

            if (remoteActors.ContainsKey(index)) {
                return;
            }

            //EventType eventType = p.EventType;
            //ushort[] eventParams = p.EventParams;
            /*string metadataPath = p.MetadataPath;
            Vector3 pos = p.Pos;

            Await.NextAfterUpdate().OnCompleted(() => {
                ActorBase actor = EventSpawner.SpawnEvent(ActorInstantiationFlags.IsCreatedFromEventMap, eventType, pos, eventParams);
                IRemotableActor remotableActor = actor as IRemotableActor;
                if (remotableActor == null) {
                    return;
                }

                remotableActor.Index = index;
                remoteActors[index] = remotableActor;

                //AddObject(actor);
                AddActor(actor);
            });*/

            Vector3 pos = p.Pos;
            string metadataPath = p.MetadataPath;

            Await.NextAfterUpdate().OnCompleted(() => {
                RemoteActor actor = new RemoteActor();
                actor.OnActivated(this, pos, metadataPath);
                actor.Index = index;
                remoteActors[index] = actor;

                //AddObject(actor);
                AddActor(actor);
            });
        }

        /// <summary>
        /// Some remote object was destroyed
        /// </summary>
        private void OnDestroyRemoteObject(ref DestroyRemoteActor p)
        {
            int index = p.Index;

            if ((index & 0xff) == localPlayerIndex) {
                return;
            }

            Await.NextAfterUpdate().OnCompleted(() => {
                /*IRemotableActor actor;
                if (remoteActors.TryGetValue(index, out actor)) {
                    remoteActors.Remove(index);
                    //RemoveObject(actor as ActorBase);
                    RemoveActor(actor as ActorBase);
                }*/

                RemoteActor actor;
                if (remoteActors.TryGetValue(index, out actor)) {
                    remoteActors.Remove(index);
                    //RemoveObject(actor as ActorBase);
                    RemoveActor(actor as ActorBase);
                }
            });
        }

        /// <summary>
        /// Player is requested to decrease its health
        /// </summary>
        private void OnDecreasePlayerHealth(ref DecreasePlayerHealth p)
        {
            // ToDo: This should be probably replaced with PlayerHealthChanged event
            if (p.Index != localPlayerIndex) {
                return;
            }

            byte amount = p.Amount;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    players[0].TakeDamage(amount, 0f);
                }
            });
        }

        /// <summary>
        /// Some remote player died
        /// </summary>
        public void OnRemotePlayerDied(ref RemotePlayerDied p)
        {
            if (p.Index == localPlayerIndex) {
                return;
            }

            RemotePlayer player = remotePlayers[p.Index];
            if (player == null) {
                return;
            }

            PlayerCorpse corpse = new PlayerCorpse();
            corpse.OnActivated(new ActorActivationDetails {
                LevelHandler = this,
                Pos = player.Transform.Pos,
                Params = new[] { (ushort)player.PlayerType, (ushort)(player.IsFacingLeft ? 1 : 0) }
            });
            AddActor(corpse);

            player.UpdateFromServer(player.Transform.Pos, AnimState.Idle, -1, player.IsFacingLeft);
        }

        public void OnRefreshActorAnimation(ref RefreshActorAnimation p)
        {
            int index = p.Index;

            if ((index & 0xff) == localPlayerIndex) {
                return;
            }

            string identifier = p.Identifier;

            Await.NextAfterUpdate().OnCompleted(() => {
                RemoteActor actor;
                if (remoteActors.TryGetValue(index, out actor)) {
                    actor.OnRefreshActorAnimation(identifier);
                }
            });
        }
    }
}