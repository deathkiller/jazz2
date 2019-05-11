using System.Collections.Generic;
using System.Threading;
using Duality;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Game.UI;
using Jazz2.Networking;
using Jazz2.Networking.Packets;
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

        private RemotePlayer[] remotePlayers = new RemotePlayer[256];

        private Dictionary<int, IRemotableActor> localRemotableActors = new Dictionary<int, IRemotableActor>();
        private Dictionary<int, IRemotableActor> remoteActors = new Dictionary<int, IRemotableActor>();
        private int lastRemotableActorIndex;

        public byte PlayerIndex => localPlayerIndex;

        public NetworkLevelHandler(App root, NetworkHandler net, LevelInitialization data, byte playerIndex) : base(root, data)
        {
            this.net = net;
            this.localPlayerIndex = playerIndex;

            net.OnUpdateAllPlayers += OnUpdateAllPlayers;
            net.RegisterCallback<CreateControllablePlayer>(OnCreateControllablePlayer);
            net.RegisterCallback<CreateRemotePlayer>(OnCreateRemotePlayer);
            net.RegisterCallback<DestroyRemotePlayer>(OnDestroyRemotePlayer);
            net.RegisterCallback<CreateRemoteObject>(OnCreateRemoteObject);
            net.RegisterCallback<DestroyRemoteObject>(OnDestroyRemoteObject);
            net.RegisterCallback<DecreasePlayerHealth>(OnDecreasePlayerHealth);
            net.RegisterCallback<RemotePlayerDied>(OnRemotePlayerDied);
        }

        protected override void OnDisposing(bool manually)
        {
            if (net != null) {
                net.OnUpdateAllPlayers -= OnUpdateAllPlayers;
                net.RemoveCallback<CreateControllablePlayer>();
                net.RemoveCallback<CreateRemotePlayer>();
                net.RemoveCallback<DestroyRemotePlayer>();
                net.RemoveCallback<CreateRemoteObject>();
                net.RemoveCallback<DestroyRemoteObject>();
                net.RemoveCallback<DecreasePlayerHealth>();
                net.RemoveCallback<RemotePlayerDied>();
            }

            base.OnDisposing(manually);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

#if DEBUG
            Hud.ShowDebugText("- Local Player Index: " + localPlayerIndex);
            Hud.ShowDebugText("- RTT: " + (int)(net.AverageRoundtripTime * 1000) + " ms / Up: " + net.Up + " / Down: " + net.Down);
            Hud.ShowDebugText("- Last Server Update: " + lastServerUpdateTime);
            Hud.ShowDebugText("- Remote Objects: " + localRemotableActors.Count + " / " + remoteActors.Count);
#endif

            if (players.Count > 0) {
                float timeMult = Time.TimeMult;
                lastUpdate += timeMult;

                if (lastUpdate < 1.4f) {
                    return;
                }

                lastUpdate = 0f;

                long updateTime = (long)(NetTime.Now * 1000);

                UpdateSelf updateSelfPacket = players[0].CreateUpdatePacket();
                updateSelfPacket.Index = localPlayerIndex;
                updateSelfPacket.UpdateTime = updateTime;
                net.Send(updateSelfPacket, 29, NetDeliveryMethod.Unreliable, PacketChannels.Main);

                foreach (KeyValuePair<int, IRemotableActor> pair in localRemotableActors) {
                    UpdateRemotableActor p = new UpdateRemotableActor();
                    pair.Value.OnUpdateRemotableActor(ref p);
                    p.Index = pair.Key;
                    p.UpdateTime = updateTime;
                    net.Send(p, 32, NetDeliveryMethod.Unreliable, PacketChannels.Main);
                }
            }
        }

        public override bool HandlePlayerDied(Player player)
        {
            net.Send(new SelfDied {
                Index = localPlayerIndex
            }, 2, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);

            return false;
        }

        public override void BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams)
        {
            if (eventType == EventType.ModifierHurt) {
                net.Send(new RemotePlayerHit {
                    Index = (byte)eventParams[0],
                    Damage = (byte)eventParams[1]
                }, 3, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
                return;
            }

            base.BroadcastTriggeredEvent(eventType, eventParams);
        }

        public override void AddActor(ActorBase actor)
        {
            base.AddActor(actor);

            IRemotableActor remotableActor = actor as IRemotableActor;
            if (remotableActor == null) {
                return;
            }

            int actorIndex = localPlayerIndex | (lastRemotableActorIndex << 8);

            remotableActor.Index = actorIndex;
            localRemotableActors[actorIndex] = remotableActor;

            CreateRemotableActor p = new CreateRemotableActor();
            remotableActor.OnCreateRemotableActor(ref p);
            p.Index = actorIndex;
            net.Send(p, 13, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);

            lastRemotableActorIndex++;
        }

        public override void RemoveActor(ActorBase actor)
        {
            base.RemoveActor(actor);

            IRemotableActor remotableActor = actor as IRemotableActor;
            if (remotableActor == null) {
                return;
            }

            int actorIndex = remotableActor.Index;
            if (!localRemotableActors.Remove(actorIndex)) {
                return;
            }

            DestroyRemotableActor p = new DestroyRemotableActor();
            p.Index = actorIndex;
            net.Send(p, 5, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
        }

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

                AnimState animState = (AnimState)msg.ReadUInt32();
                float animTime = msg.ReadFloat();
                bool isFacingLeft = msg.ReadBoolean();

                pos.X += speed.X * rtt;
                pos.Y += speed.Y * rtt;

                IRemotableActor actor;
                if (remoteActors.TryGetValue(objectIndex, out actor)) {
                    actor.OnUpdateRemoteActor(pos, speed, animState, animTime, isFacingLeft);
                }
            }
        }

        private void OnCreateControllablePlayer(ref CreateControllablePlayer p)
        {
            // ToDo: throw on mismatch?
            localPlayerIndex = p.Index;

            PlayerType type = p.Type;
            Vector3 pos = p.Pos;

            Root.DispatchToMainThread(delegate {
                if (players.Count > 0) {
                    Player oldPlayer = players[0];
                    if (oldPlayer.PlayerType == type) {
                        oldPlayer.Respawn(pos.Xy);
                        return;
                    }

                    RemoveActor(oldPlayer);
                    Players.Remove(oldPlayer);
                }

                Player player = new Player();
                player.OnAttach(new ActorInstantiationDetails {
                    Api = Api,
                    Pos = pos,
                    Params = new[] { (ushort)type, (ushort)0 }
                });
                AddPlayer(player);

                cameras[0].Transform.Pos = new Vector3(pos.Xy, 0);
                cameras[0].GetComponent<CameraController>().TargetObject = player;

                player.AttachToHud(rootObject.AddComponent<Hud>());

                //player.ReceiveLevelCarryOver(data.ExitType, ref data.PlayerCarryOvers[i]);
            });
        }

        private void OnCreateRemotePlayer(ref CreateRemotePlayer p)
        {
            byte index = p.Index;

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

                //AddObject(player);
                AddActor(player);
            });
        }

        private void OnDestroyRemotePlayer(ref DestroyRemotePlayer p)
        {
            byte index = p.Index;

            Root.DispatchToMainThread(delegate {
                RemotePlayer player = Interlocked.Exchange(ref remotePlayers[index], null);
                if (player != null) {
                    //RemoveObject(player);
                    RemoveActor(player);
                }
            });
        }

        private void OnCreateRemoteObject(ref CreateRemoteObject p)
        {
            int index = p.Index;

            if ((index & 0xff) == localPlayerIndex) {
                return;
            }

            if (remoteActors.ContainsKey(index)) {
                return;
            }

            EventType eventType = p.EventType;
            ushort[] eventParams = p.EventParams;
            Vector3 pos = p.Pos;

            Root.DispatchToMainThread(delegate {

                ActorBase actor = EventSpawner.SpawnEvent(ActorInstantiationFlags.None, eventType, pos, eventParams);
                IRemotableActor remotableActor = actor as IRemotableActor;
                if (remotableActor == null) {
                    return;
                }

                remoteActors[index] = remotableActor;

                AddObject(actor);
            });
        }

        private void OnDestroyRemoteObject(ref DestroyRemoteObject p)
        {
            int index = p.Index;

            if ((index & 0xff) == localPlayerIndex) {
                return;
            }

            Root.DispatchToMainThread(delegate {
                IRemotableActor actor;
                if (remoteActors.TryGetValue(index, out actor)) {
                    remoteActors.Remove(index);
                    RemoveObject(actor as ActorBase);
                }
            });
        }

        private void OnDecreasePlayerHealth(ref DecreasePlayerHealth p)
        {
            if (p.Index != localPlayerIndex) {
                return;
            }

            byte amount = p.Amount;

            Root.DispatchToMainThread(delegate {
                if (players.Count > 0) {
                    players[0].TakeDamage(amount, 0f);
                }
            });
        }

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
            corpse.OnAttach(new ActorInstantiationDetails {
                Api = Api,
                Pos = player.Transform.Pos,
                Params = new[] { (ushort)player.PlayerType, (ushort)(player.IsFacingLeft ? 1 : 0) }
            });
            AddActor(corpse);

            player.UpdateFromServer(player.Transform.Pos, AnimState.Idle, -1, player.IsFacingLeft);
        }
    }
}