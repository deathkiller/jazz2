#if MULTIPLAYER && !SERVER

using System.Collections.Generic;
using Duality;
using Duality.Async;
using Jazz2.Actors;
using Jazz2.Game.Components;
using Jazz2.Game.Structs;
using Jazz2.Game.UI;
using Jazz2.Networking;
using Jazz2.Networking.Packets.Client;
using Jazz2.Networking.Packets.Server;
using Lidgren.Network;

namespace Jazz2.Game
{
    public class MultiplayerLevelHandler : LevelHandler
    {
        private GameClient client;
        private byte localPlayerIndex;
        private float lastUpdate;
        private long lastServerUpdateTime;
        private int isStillLoading;

        //private Dictionary<int, IRemotableActor> localRemotableActors = new Dictionary<int, IRemotableActor>();
        private Dictionary<int, RemoteActor> remoteActors = new Dictionary<int, RemoteActor>();

        public MultiplayerLevelHandler(App root, GameClient client, LevelInitialization data, byte playerIndex) : base(root, data)
        {
            this.client = client;
            this.localPlayerIndex = playerIndex;

            client.OnUpdateAllActors += OnUpdateAllActors;
            client.AddCallback<CreateControllablePlayer>(OnPacketCreateControllablePlayer);
            client.AddCallback<CreateRemoteActor>(OnPacketCreateRemoteActor);
            client.AddCallback<DestroyRemoteActor>(OnPacketDestroyRemoteActor);
            client.AddCallback<RefreshActorAnimation>(OnPacketRefreshActorAnimation);
            client.AddCallback<ShowMessage>(OnPacketShowMessage);

            client.AddCallback<PlayerTakeDamage>(OnPacketPlayerTakeDamage);
            client.AddCallback<PlayerAddHealth>(OnPacketPlayerAddHealth);
            client.AddCallback<PlayerActivateForce>(OnPacketPlayerActivateForce);
            client.AddCallback<PlayerRefreshAmmo>(OnPacketPlayerRefreshAmmo);
            client.AddCallback<PlayerRefreshWeaponUpgrades>(OnPacketPlayerRefreshWeaponUpgrades);
            client.AddCallback<PlayerWarpToPosition>(OnPacketPlayerWarpToPosition);
            client.AddCallback<PlayerSetDizzyTime>(OnPacketPlayerSetDizzyTime);
            client.AddCallback<PlayerSetModifier>(OnPacketPlayerSetModifier);
            client.AddCallback<PlayerSetLap>(OnPacketPlayerSetLap);
            client.AddCallback<PlayerSetInvulnerability>(OnPacketPlayerSetInvulnerability);

            client.AddCallback<AdvanceTileAnimation>(OnPacketAdvanceTileAnimation);
            client.AddCallback<SetTrigger>(OnPacketSetTrigger);

            // Wait 3 frames and then inform server that loading is complete
            isStillLoading = 3;
        }

        protected override void OnDisposing(bool manually)
        {
            if (client != null) {
                client.OnUpdateAllActors -= OnUpdateAllActors;
                client.RemoveCallback<CreateControllablePlayer>();
                client.RemoveCallback<CreateRemoteActor>();
                client.RemoveCallback<DestroyRemoteActor>();
                client.RemoveCallback<RefreshActorAnimation>();
                client.RemoveCallback<ShowMessage>();

                client.RemoveCallback<PlayerTakeDamage>();
                client.RemoveCallback<PlayerAddHealth>();
                client.RemoveCallback<PlayerActivateForce>();
                client.RemoveCallback<PlayerRefreshAmmo>();
                client.RemoveCallback<PlayerRefreshWeaponUpgrades>();
                client.RemoveCallback<PlayerWarpToPosition>();
                client.RemoveCallback<PlayerSetDizzyTime>();
                client.RemoveCallback<PlayerSetModifier>();
                client.RemoveCallback<PlayerSetLap>();
                client.RemoveCallback<PlayerSetInvulnerability>();

                client.RemoveCallback<AdvanceTileAnimation>();
                client.RemoveCallback<SetTrigger>();
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
                    client.SendToServer(new LevelReady {
                        Index = localPlayerIndex
                    }, 2, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);
                }
            }

#if DEBUG
            Hud.ShowDebugText("- Local Player Index: " + localPlayerIndex);
            Hud.ShowDebugText("- RTT: " + (int)(client.AverageRoundtripTime * 1000) + " ms / Up: " + client.UploadPacketBytes + " / Down: " + client.DownloadPacketBytes);
            Hud.ShowDebugText("- Last Server Update: " + lastServerUpdateTime);
            Hud.ShowDebugText("- Remote Objects: " + /*localRemotableActors.Count + " / " +*/ remoteActors.Count);
#endif

            if (players.Count > 0) {
                lastUpdate += timeMult;

                if (lastUpdate < 1.4f) {
                    return;
                }

                lastUpdate = 0f;

                long updateTime = (long)(NetTime.Now * 1000);

                // Send update to server
                Player player = players[0];

                client.SendToServer(new PlayerUpdate {
                    Index = localPlayerIndex,
                    UpdateTime = updateTime,
                    Pos = player.Transform.Pos,
                    CurrentSpecialMove = player.CurrentSpecialMove,
                    IsFacingLeft = player.IsFacingLeft,
                    IsActivelyPushing = player.IsActivelyPushing
                }, 18, NetDeliveryMethod.Unreliable, PacketChannels.UnorderedUpdates);
            }
        }

        public override bool HandlePlayerDied(Player player)
        {
            /*client.SendToServer(new PlayerDied {
                Index = localPlayerIndex
            }, 2, NetDeliveryMethod.ReliableOrdered, PacketChannels.Main);*/

            return false;
        }

        /*public override void BroadcastTriggeredEvent(EventType eventType, ushort[] eventParams)
        {
            base.BroadcastTriggeredEvent(eventType, eventParams);
        }*/

        public override void BroadcastAnimationChanged(ActorBase actor, string identifier)
        {
            base.BroadcastAnimationChanged(actor, identifier);

            switch (actor) {
                case Player player: {
                    client.SendToServer(new PlayerRefreshAnimation {
                        Index = localPlayerIndex,
                        Identifier = identifier
                    }, 48, NetDeliveryMethod.Unreliable, PacketChannels.UnorderedUpdates);
                    break;
                }
            }
        }

        /*public override void AddActor(ActorBase actor)
        {
            base.AddActor(actor);

            // ToDo
            IRemotableActor remotableActor = actor as IRemotableActor;
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

            lastRemotableActorIndex++;
        }

        public override void RemoveActor(ActorBase actor)
        {
            base.RemoveActor(actor);

            IRemotableActor remotableActor = actor as IRemotableActor;
            if (remotableActor == null || (remotableActor.Index & 0xff) != localPlayerIndex) {
                return;
            }

            int actorIndex = remotableActor.Index;
            if (!localRemotableActors.Remove(actorIndex)) {
                return;
            }

            DestroyRemotableActor p = new DestroyRemotableActor();
            p.Index = actorIndex;
            net.Send(p, 5, NetDeliveryMethod.ReliableUnordered, PacketChannels.Main);
        }*/

        public override bool OverridePlayerFireWeapon(Player player, WeaponType weaponType, out float weaponCooldown)
        {
            switch (weaponType) {
                case WeaponType.Blaster: weaponCooldown = 40f - (player.WeaponUpgrades[(int)WeaponType.Blaster] >> 1) * 2f; break;
                case WeaponType.Bouncer: weaponCooldown = 32f - (player.WeaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.7f; break;
                case WeaponType.Freezer: weaponCooldown = 46f - (player.WeaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.6f; break;
                case WeaponType.Seeker: weaponCooldown = 100f; break;
                case WeaponType.RF: weaponCooldown = 100f; break;
                case WeaponType.Toaster: weaponCooldown = 6f; break;
                case WeaponType.TNT: weaponCooldown = 30f; break;
                case WeaponType.Pepper: weaponCooldown = 36f - (player.WeaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.6f; break;
                case WeaponType.Electro: weaponCooldown = 32f - (player.WeaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.2f; break;
                // ToDo
                case WeaponType.Thunderbolt: weaponCooldown = 42f - (player.WeaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1f; break;

                default: {
                    weaponCooldown = 0;
                    return true;
                }
            }

            player.GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            client.SendToServer(new PlayerFireWeapon {
                Index = localPlayerIndex,
                WeaponType = weaponType,
                InitialPos = initialPos,
                GunspotPos = gunspotPos,
                Angle = angle
            }, 13, NetDeliveryMethod.ReliableUnordered, PacketChannels.UnorderedUpdates);

            return true;
        }

        /// <summary>
        /// Player should update state and position of all other players and objects
        /// This type of packet is sent very often, so it's handled differently
        /// </summary>
        private void OnUpdateAllActors(NetIncomingMessage msg)
        {
            msg.Position = 8; // Skip packet type

            long serverUpdateTime = msg.ReadInt64();
            if (lastServerUpdateTime > serverUpdateTime) {
                return;
            }

            lastServerUpdateTime = serverUpdateTime;

            while (true) {
                int objectIndex = msg.ReadInt32();
                if (objectIndex == -1) {
                    break;
                }

                RemoteActor actor;

                byte flags = msg.ReadByte();
                if ((flags & 0x01) != 0x01) {
                    // Not visible
                    if (remoteActors.TryGetValue(objectIndex, out actor)) {
                        actor.SyncWithServer(false);
                    }
                    continue;
                }

                Vector3 pos;
                {
                    ushort x = msg.ReadUInt16();
                    ushort y = msg.ReadUInt16();
                    ushort z = msg.ReadUInt16();
                    pos = new Vector3(x, y, z);
                }

                float scale = 0f;
                float angle = 0f;
                if ((flags & 0x02) == 0x02) {
                    scale = msg.ReadSingle();
                    angle = msg.ReadRangedSingle(0f, MathF.TwoPi, 8);
                }

                bool isFacingLeft = msg.ReadBoolean();

                if (remoteActors.TryGetValue(objectIndex, out actor)) {
                    actor.SyncWithServer(true, pos, isFacingLeft);

                    if ((flags & 0x02) == 0x02) {
                        actor.Transform.Scale = scale;
                        actor.Transform.Angle = angle;
                    }
                }
            }
        }

        /// <summary>
        /// Player is allowed to join game, controllable actor should be created
        /// </summary>
        private void OnPacketCreateControllablePlayer(ref CreateControllablePlayer p)
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
        /// Some remote object was created
        /// </summary>
        private void OnPacketCreateRemoteActor(ref CreateRemoteActor p)
        {
            int index = p.Index;

            if ((index & 0xff) == localPlayerIndex) {
                return;
            }

            if (remoteActors.ContainsKey(index)) {
                return;
            }

            Vector3 pos = p.Pos;
            string metadataPath = p.MetadataPath;
            CollisionFlags collisionFlags = p.CollisionFlags;

            Await.NextAfterUpdate().OnCompleted(() => {
                RemoteActor actor = new RemoteActor();
                actor.OnActivated(this, pos, metadataPath, collisionFlags);
                //actor.Index = index;
                remoteActors[index] = actor;
                AddActor(actor);
            });
        }

        /// <summary>
        /// Some remote object was destroyed
        /// </summary>
        private void OnPacketDestroyRemoteActor(ref DestroyRemoteActor p)
        {
            int index = p.Index;

            if ((index & 0xff) == localPlayerIndex) {
                return;
            }

            Await.NextAfterUpdate().OnCompleted(() => {
                if (remoteActors.TryGetValue(index, out RemoteActor actor)) {
                    remoteActors.Remove(index);
                    RemoveActor(actor);
                }
            });
        }

        /// <summary>
        /// Player is requested to decrease its health
        /// </summary>
        private void OnPacketPlayerTakeDamage(ref PlayerTakeDamage p)
        {
            // ToDo: This should be probably replaced with PlayerHealthChanged event
            if (p.Index != localPlayerIndex) {
                return;
            }

            byte healthBefore = p.HealthBefore;
            byte damageAmount = p.DamageAmount;
            float pushForce = p.PushForce;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    Player player = players[0];
                    player.Health = healthBefore;
                    if (damageAmount > 0) {
                        player.TakeDamage(damageAmount, pushForce);
                    }
                }
            });
        }

        private void OnPacketPlayerAddHealth(ref PlayerAddHealth p)
        {
            // ToDo: This should be probably replaced with PlayerHealthChanged event
            if (p.Index != localPlayerIndex) {
                return;
            }

            byte amount = p.Amount;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    players[0].AddHealth(amount == 0xff ? -1 : amount);
                }
            });
        }

        /*
        /// <summary>
        /// Some remote player died
        /// </summary>
        private void OnRemotePlayerDied(ref RemotePlayerDied p)
        {
            if (p.Index == localPlayerIndex) {
                return;
            }

            RemoteActor player = remotePlayers[p.Index];
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

            player.SyncWithServer(player.Transform.Pos, false, player.IsFacingLeft);
        }
        */

        private void OnPacketRefreshActorAnimation(ref RefreshActorAnimation p)
        {
            int index = p.Index;

            if ((index & 0xff) == localPlayerIndex) {
                return;
            }

            string identifier = p.Identifier;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (remoteActors.TryGetValue(index, out RemoteActor actor)) {
                    actor.OnRefreshActorAnimation(identifier);
                }
            });
        }

        private void OnPacketShowMessage(ref ShowMessage p)
        {
            string text = p.Text;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    Hud hud = players[0].AttachedHud;
                    if (hud != null) {
                        hud.ShowLevelText(text);
                    }
                }
            });
        }

        private void OnPacketPlayerActivateForce(ref PlayerActivateForce p)
        {
            byte index = p.Index;

            if (p.Index != localPlayerIndex) {
                return;
            }

            PlayerActivateForce.ForceType activatedBy = p.ActivatedBy;
            Vector2 force = p.Force;
            bool keepSpeedX = p.KeepSpeedX;
            bool keepSpeedY = p.KeepSpeedY;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    switch (activatedBy) {
                        case PlayerActivateForce.ForceType.Spring:
                            players[0].OnSpringActivated(force, keepSpeedX, keepSpeedY);
                            break;
                        case PlayerActivateForce.ForceType.PinballBumper:
                            players[0].OnPinballBumperActivated(force);
                            break;
                        case PlayerActivateForce.ForceType.PinballPaddle:
                            players[0].OnPinballPaddleActivated(force);
                            break;
                    }
                }
            });
        }

        private void OnPacketPlayerRefreshAmmo(ref PlayerRefreshAmmo p)
        {
            byte index = p.Index;

            if (p.Index != localPlayerIndex) {
                return;
            }

            WeaponType weaponType = p.WeaponType;
            short count = p.Count;
            bool switchTo = p.SwitchTo;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    players[0].OnRefreshAmmo(weaponType, count, switchTo);
                }
            });
        }

        private void OnPacketPlayerRefreshWeaponUpgrades(ref PlayerRefreshWeaponUpgrades p)
        {
            byte index = p.Index;

            if (p.Index != localPlayerIndex) {
                return;
            }

            WeaponType weaponType = p.WeaponType;
            byte upgrades = p.Upgrades;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    players[0].WeaponUpgrades[(int)weaponType] = upgrades;
                }
            });
        }

        private void OnPacketPlayerWarpToPosition(ref PlayerWarpToPosition p)
        {
            byte index = p.Index;

            if (p.Index != localPlayerIndex) {
                return;
            }

            Vector2 pos = p.Pos;
            bool fast = p.Fast;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    players[0].WarpToPosition(pos, fast);
                }
            });
        }

        private void OnPacketPlayerSetDizzyTime(ref PlayerSetDizzyTime p)
        {
            byte index = p.Index;

            if (p.Index != localPlayerIndex) {
                return;
            }

            float time = p.Time;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    players[0].SetDizzyTime(time);
                }
            });
        }

        private void OnPacketPlayerSetModifier(ref PlayerSetModifier p)
        {
            byte index = p.Index;

            if (p.Index != localPlayerIndex) {
                return;
            }

            Player.Modifier modifier = p.Modifier;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    players[0].SetModifier(modifier);
                }
            });
        }


        private void OnPacketPlayerSetLap(ref PlayerSetLap p)
        {
            // ToDo
            /*byte index = p.Index;

            if (p.Index != localPlayerIndex) {
                return;
            }*/
        }

        private void OnPacketPlayerSetInvulnerability(ref PlayerSetInvulnerability p)
        {
            byte index = p.Index;

            if (p.Index != localPlayerIndex) {
                return;
            }

            float time = p.Time;
            bool withCircleEffect = p.WithCircleEffect;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (players.Count > 0) {
                    players[0].SetInvulnerability(time, withCircleEffect);
                }
            });
        }

        private void OnPacketAdvanceTileAnimation(ref AdvanceTileAnimation p)
        {
            int tileX = p.TileX;
            int tileY = p.TileY;
            int amount = p.Amount;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (TileMap != null) {
                    TileMap.AdvanceTileAnimationExternally(tileX, tileY, amount);
                }
            });
        }

        private void OnPacketSetTrigger(ref SetTrigger p)
        {
            ushort triggerID = p.TriggerID;
            bool newState = p.NewState;

            Await.NextAfterUpdate().OnCompleted(() => {
                if (TileMap != null) {
                    TileMap.SetTrigger(triggerID, newState);
                }
            });
        }
    }
}

#endif