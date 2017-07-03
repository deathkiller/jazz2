using System;
using System.Collections.Generic;
using Duality;
using Duality.Audio;
using Jazz2.Actors.Collectibles;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Environment;
using Jazz2.Actors.Solid;
using Jazz2.Game;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors
{
    public enum PlayerType
    {
        Jazz,
        Spaz,
        Lori
    }

    public partial class Player : ActorBase
    {
        private enum SpecialMoveType
        {
            None,
            Buttstomp,
            Uppercut,
            Sidekick
        }

        private const float MAX_DASHING_SPEED = 9.0f;
        private const float MAX_RUNNING_SPEED = 4.0f;
        private const float ACCELERATION = 0.2f;
        private const float DECELERATION = 0.22f; // 0.25f

        private bool isActivelyPushing;
        private int pushFrames;
        private bool controllable = true;

        private bool wasUpPressed;
        private bool wasDownPressed;
        private bool wasJumpPressed;
        private bool wasFirePressed;

        private float controllableTimeout;

        private PlayerType playerType;
        private SpecialMoveType currentSpecialMove;
        private bool isAttachedToPole;
        private float copterFramesLeft;
        private bool levelExiting;
        private bool isFreefall, inWater, isAirboard, isLifting, isSpring;

        private bool inIdleTransition;
        private MovingPlatform carryingObject;
        private bool canDoubleJump = true;

        private bool isSugarRush;
        private int lives, score, coins;
        private Vector2 savePointPos;
        private float savePointLight;

        private int gems, gemsPitch;
        private float gemsTimer;
        private float bonusWarpTimer;

        private float invulnerableTime;
        private float invulnerableBlinkTime;

        private float lastPoleTime;
        private Point2 lastPolePos;

        private Hud attachedHud;

        public int Lives => lives;
        public PlayerType PlayerType => playerType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            playerType = (PlayerType)details.Params[0];

            switch (playerType) {
                case PlayerType.Jazz:
                    RequestMetadata("Interactive/PlayerJazz");
                    break;
                case PlayerType.Spaz:
                    RequestMetadata("Interactive/PlayerSpaz");
                    break;
                case PlayerType.Lori:
                    RequestMetadata("Interactive/PlayerLori");
                    break;
            }

            SetAnimation(AnimState.Fall);

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 1.0f;
            light.RadiusNear = 40;
            light.RadiusFar = 110;

            weaponAmmo = new int[(int)WeaponType.Count];
            weaponUpgrades = new byte[(int)WeaponType.Count];

            weaponAmmo[(int)WeaponType.Blaster] = -1;

            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithSolidObjects | CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation | CollisionFlags.IsSolidObject;

            maxHealth = health = 5;
            currentWeapon = WeaponType.Toaster;

            savePointPos = details.Pos.Xy;
            savePointLight = api.AmbientLight;
        }

        public void ReceiveLevelCarryOver(ExitType exitType, ref PlayerCarryOver carryOver)
        {
            lives = carryOver.Lives;
            score = carryOver.Score;
            currentWeapon = carryOver.CurrentWeapon;

            if (carryOver.Ammo != null) {
                int n = Math.Min(weaponAmmo.Length, carryOver.Ammo.Length);
                for (int i = 0; i < n; i++) {
                    weaponAmmo[i] = carryOver.Ammo[i];
                }
            }

            if (carryOver.WeaponUpgrades != null) {
                int n = Math.Min(weaponUpgrades.Length, carryOver.WeaponUpgrades.Length);
                for (int i = 0; i < n; i++) {
                    weaponUpgrades[i] = carryOver.WeaponUpgrades[i];
                }
            }

            if (exitType == ExitType.Warp || exitType == ExitType.Bonus) {
                PlaySound("COMMON_WARP_OUT");

                collisionFlags &= ~CollisionFlags.ApplyGravitation;

                isFreefall = CanFreefall();
                SetPlayerTransition(isFreefall ? AnimState.TransitionWarpOutFreefall : AnimState.TransitionWarpOut, false, true, SpecialMoveType.None, delegate {
                    isInvulnerable = false;
                    collisionFlags |= CollisionFlags.ApplyGravitation;
                    controllable = true;
                });
            }
        }

        public PlayerCarryOver PrepareLevelCarryOver()
        {
            return new PlayerCarryOver {
                Type = playerType,

                Lives = lives,
                Score = score,
                CurrentWeapon = currentWeapon,
                Ammo = weaponAmmo,
                WeaponUpgrades = weaponUpgrades
            };
        }

        public void OnLevelChanging(ExitType exitType)
        {
            levelExiting = true;

            // ToDo: Implement better level transitions
            if (exitType == ExitType.Warp || exitType == ExitType.Bonus) {
                //addTimer(285u, false, [this]() {
                //inFreefall = inFreefall || (!canJump && speedY > 14f);
                SetPlayerTransition(isFreefall ? AnimState.TransitionWarpInFreefall : AnimState.TransitionWarpIn, false, true, SpecialMoveType.None, delegate {
                    renderer.Active = false;
                });
                PlaySound("COMMON_WARP_IN");
                //});
            } else {
                // ToDo: Sound with timer
                //addTimer(255u, false, [this]() {
                SetPlayerTransition(AnimState.TransitionEndOfLevel, false, true, SpecialMoveType.None, delegate {
                    renderer.Active = false;
                });
                PlaySound("PLAYER_EOL_1");
                //});
                //addTimer(335u, false, [this]() {
                //PlayNonPositionalSound("PLAYER_EOL_2");
                //});
            }

            isFacingLeft = false;
            isInvulnerable = true;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;
            speedX = 0;
            speedY = 0;
            externalForceX = 0;
            externalForceY = 0;
            internalForceY = 0;
        }

        protected override void OnUpdate()
        {
            float timeMult = Time.TimeMult;
            float lastX = Transform.Pos.X;
            float lastForceX = externalForceX;

            base.OnUpdate();

            FollowCarryingPlatform();
            UpdateSpeedBasedAnimation(timeMult, lastX, lastForceX);

            PushSolidObjects();
            CheckSuspendedStatus();
            CheckDestructibleTiles();
            CheckEndOfSpecialMoves(timeMult);

            HandleWater();
            HandleAreaEvents();
            HandleActorCollisions();

            // Weapons
            if (weaponCooldown > 0f) {
                weaponCooldown -= timeMult;
            }

            if (controllableTimeout > 0f) {
                controllableTimeout -= timeMult;

                if (controllableTimeout <= 0f) {
                    controllable = true;
                }
            }

            if (gemsTimer > 0f) {
                gemsTimer -= timeMult;

                if (gemsTimer <= 0f) {
                    gemsPitch = 0;
                }
            }

            if (invulnerableTime > 0f) {
                invulnerableTime -= timeMult;

                if (invulnerableTime <= 0f) {
                    isInvulnerable = false;

                    renderer.Active = true;
                } else if (currentTransitionState != AnimState.Hurt) {
                    if (invulnerableBlinkTime > 0f) {
                        invulnerableBlinkTime -= timeMult;
                    } else {
                        renderer.Active ^= true;

                        invulnerableBlinkTime = 3f;
                    }
                } else {
                    renderer.Active = true;
                }
            }

            if (bonusWarpTimer > 0f) {
                bonusWarpTimer -= timeMult;
            }

            if (lastPoleTime > 0f) {
                lastPoleTime -= timeMult;
            }

            Hud.ShowDebugText("- Pos.: {" + (int)Transform.Pos.X + "; " + (int)Transform.Pos.Y + "}");
            Hud.ShowDebugText("  Speed: {" + speedX.ToString("F1") + "; " + speedY.ToString("F1") + "}");
            Hud.ShowDebugText("  Force: {" + externalForceX.ToString("F1") + "; " + externalForceY.ToString("F1") + "} " + internalForceY + " | " + ((collisionFlags & CollisionFlags.ApplyGravitation) != 0 ? " G" : "") + (controllable ? " C" : "") + (inWater ? " W" : "") + (canJump ? " J" : ""));
            Hud.ShowDebugText("  A.: " + currentAnimationState + " | T.: " + currentTransitionState);
            //Hud.ShowDebug("  Sus.: " + suspendType + " | Health: " + health + "/" + maxHealth);
            //Hud.ShowDebug("  Score: " + score.ToString("##00000000") + " | Coins: " + coins);

            // Controls
            // Move
            bool isRightPressed;
            if (!isLifting && controllable && ((isRightPressed = DualityApp.Keyboard.KeyPressed(Duality.Input.Key.Right)) ^ DualityApp.Keyboard.KeyPressed(Duality.Input.Key.Left))) {
                SetAnimation(currentAnimationState & ~(AnimState.Lookup | AnimState.Crouch));

                isFacingLeft = !isRightPressed;
                isActivelyPushing = true;

                bool isDashPressed = DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C);
                if (suspendType == SuspendType.None && isDashPressed) {
                    speedX = MathF.Clamp(speedX + ACCELERATION * timeMult * (isFacingLeft ? -1 : 1), -MAX_DASHING_SPEED, MAX_DASHING_SPEED);
                } else if (suspendType != SuspendType.Hook && !(wasFirePressed && suspendType == SuspendType.Vine)) {
                    speedX = MathF.Clamp(speedX + ACCELERATION * timeMult * (isFacingLeft ? -1 : 1), -MAX_RUNNING_SPEED, MAX_RUNNING_SPEED);
                }

                if (canJump) {
                    wasUpPressed = wasDownPressed = false;
                }
            } else {
                speedX = MathF.Max((MathF.Abs(speedX) - DECELERATION * timeMult), 0) * (speedX < 0 ? -1 : 1);
                isActivelyPushing = false;
            }

            if (!controllable) {
                return;
            }

            if (inWater || isAirboard) {
                bool isDownPressed;
                if (((isDownPressed = DualityApp.Keyboard.KeyPressed(Duality.Input.Key.Down)) ^ DualityApp.Keyboard.KeyPressed(Duality.Input.Key.Up))) {
                    float mult;
                    if (isAirboard) {
                        mult = (isDownPressed ? -1f : 0.2f);
                    } else {
                        mult = (isDownPressed ? -1f : 1f);
                    }

                    speedY = MathF.Clamp(speedY - ACCELERATION * timeMult * mult, -MAX_RUNNING_SPEED, MAX_RUNNING_SPEED);
                } else {
                    speedY = MathF.Max((MathF.Abs(speedY) - DECELERATION * timeMult), 0) * (speedY < 0 ? -1 : 1);
                }
            } else {
                // Look-up
                if (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.Up)) {
                    if (!wasUpPressed) {
                        if (canJump && !isLifting && Math.Abs(speedX) < float.Epsilon) {
                            wasUpPressed = true;

                            SetAnimation(AnimState.Lookup);
                        }
                    }
                } else if (wasUpPressed) {
                    wasUpPressed = false;

                    SetAnimation(currentAnimationState & ~AnimState.Lookup);
                }

                // Crouch
                if (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.Down)) {
                    if (suspendType != SuspendType.None) {
                        wasDownPressed = true;

                        MoveInstantly(new Vector2(0f, 10f), MoveType.RelativeTime, true);
                        suspendType = SuspendType.None;

                        // ToDo: Workaround
                        collisionFlags |= CollisionFlags.ApplyGravitation;
                    } else if (!wasDownPressed) {
                        if (canJump) {
                            if (!isLifting && Math.Abs(speedX) < float.Epsilon) {
                                wasDownPressed = true;

                                SetAnimation(AnimState.Crouch);
                            }
                        } else {
                            wasDownPressed = true;

                            controllable = false;
                            speedX = 0;
                            speedY = 0;
                            internalForceY = 0;
                            externalForceY = 0;
                            collisionFlags &= ~CollisionFlags.ApplyGravitation;
                            currentSpecialMove = SpecialMoveType.Buttstomp;
                            SetAnimation(AnimState.Buttstomp);
                            SetPlayerTransition(AnimState.TransitionButtstompStart, true, false, SpecialMoveType.Buttstomp, delegate {
                                speedY = 9;
                                SetAnimation(AnimState.Buttstomp);
                                PlaySound("PLAYER_BUTTSTOMP"); // ToDo: Sound freq. 0.8f here ???
                                PlaySound("PLAYER_BUTTSTOMP_2");
                            });
                        }
                    }
                } else if (wasDownPressed) {
                    wasDownPressed = false;

                    SetAnimation(currentAnimationState & ~AnimState.Crouch);
                }

                // Jump
                if (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.V)) {
                    if (!wasJumpPressed) {
                        wasJumpPressed = true;

                        if (isLifting && canJump && currentSpecialMove == SpecialMoveType.None) {
                            canJump = false;
                            SetAnimation(currentAnimationState & (~AnimState.Lookup & ~AnimState.Crouch));
                            PlaySound("COMMON_JUMP");
                            carryingObject = null;

                            collisionFlags &= ~CollisionFlags.IsSolidObject;

                            isLifting = false;
                            controllable = false;

                            speedY = -3f;
                            internalForceY = 0.86f;

                            collisionFlags &= ~CollisionFlags.CollideWithSolidObjects;

                            SetTransition(AnimState.TransitionLiftEnd, false, delegate {
                                controllable = true;
                                collisionFlags |= CollisionFlags.CollideWithSolidObjects;
                            });
                        } else {

                            switch (playerType) {
                                case PlayerType.Jazz:
                                    if ((currentAnimationState & AnimState.Crouch) != 0) {
                                        controllable = false;
                                        SetAnimation(AnimState.Uppercut);
                                        SetPlayerTransition(AnimState.TransitionUppercutA, true, true, SpecialMoveType.Uppercut, delegate {
                                            externalForceY = 1.5f;
                                            speedY = -2f;
                                            canJump = false;
                                            SetPlayerTransition(AnimState.TransitionUppercutB, true, true, SpecialMoveType.Uppercut);
                                        });
                                    } else {
                                        if (speedY > 0.01f && !canJump && (currentAnimationState & (AnimState.Fall | AnimState.Copter)) != 0) {
                                            collisionFlags &= ~CollisionFlags.ApplyGravitation;
                                            speedY = 1.5f;
                                            if ((currentAnimationState & AnimState.Copter) == 0) {
                                                SetAnimation(AnimState.Copter);
                                            }
                                            copterFramesLeft = 70;
                                        }
                                    }
                                    break;

                                case PlayerType.Spaz:
                                    if ((currentAnimationState & AnimState.Crouch) != 0) {
                                        controllable = false;
                                        SetAnimation(AnimState.Uppercut);
                                        SetPlayerTransition(AnimState.TransitionUppercutA, true, true, SpecialMoveType.Sidekick, delegate {
                                            externalForceX = 8f * (isFacingLeft ? -1f : 1f);
                                            speedX = 14.4f * (isFacingLeft ? -1f : 1f);
                                            collisionFlags &= ~CollisionFlags.ApplyGravitation;
                                            SetPlayerTransition(AnimState.TransitionUppercutB, true, true, SpecialMoveType.Sidekick);
                                        });

                                        PlaySound("PLAYER_SIDEKICK");
                                    } else {
                                        if (!canJump && canDoubleJump) {
                                            canDoubleJump = false;
                                            isFreefall = false;

                                            internalForceY = 1.15f;
                                            speedY = -2f - MathF.Max(0f, (MathF.Abs(speedX) - 4f) * 0.3f);

                                            PlaySound("PLAYER_DOUBLE_JUMP");

                                            SetTransition(AnimState.Spring, false);
                                        }
                                    }
                                    break;

                                case PlayerType.Lori:
                                    if ((currentAnimationState & AnimState.Crouch) != 0) {
                                        controllable = false;
                                        SetAnimation(AnimState.Uppercut);
                                        SetPlayerTransition(AnimState.TransitionUppercutA, true, true, SpecialMoveType.Sidekick, delegate {
                                            externalForceX = 15f * (isFacingLeft ? -1f : 1f);
                                            speedX = 6f * (isFacingLeft ? -1f : 1f);
                                            collisionFlags &= ~CollisionFlags.ApplyGravitation;
                                        });
                                    } else {
                                        if (speedY > 0.01f && !canJump && (currentAnimationState & (AnimState.Fall | AnimState.Copter)) != 0) {
                                            collisionFlags &= ~CollisionFlags.ApplyGravitation;
                                            speedY = 1.5f;
                                            if ((currentAnimationState & AnimState.Copter) == 0) {
                                                SetAnimation(AnimState.Copter);
                                            }
                                            copterFramesLeft = 70;
                                        }
                                    }
                                    break;
                            }

                        }
                    } else {
                        if (suspendType != SuspendType.None) {
                            MoveInstantly(new Vector2(0f, -8f), MoveType.RelativeTime, true);
                            canJump = true;
                        }
                        if (canJump && currentSpecialMove == SpecialMoveType.None && !DualityApp.Keyboard.KeyPressed(Duality.Input.Key.Down)) {
                            canJump = false;
                            isFreefall = false;
                            SetAnimation(currentAnimationState & (~AnimState.Lookup & ~AnimState.Crouch));
                            PlaySound("PLAYER_JUMP");
                            carryingObject = null;

                            collisionFlags &= ~CollisionFlags.IsSolidObject;

                            internalForceY = 1.15f;
                            speedY = -3f - MathF.Max(0f, (MathF.Abs(speedX) - 4f) * 0.3f);
                        }
                    }
                } else {
                    if (!wasJumpPressed) {
                        if (internalForceY > 0) {
                            internalForceY = 0;
                        }
                    } else {
                        wasJumpPressed = false;
                    }
                }
            }

            // Fire
            if (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.Space)) {
                if (!isLifting && (currentAnimationState & AnimState.Push) == 0 && pushFrames <= 0f) {
                    if (weaponAmmo[(int)currentWeapon] != 0) {
                        SetAnimation(currentAnimationState | AnimState.Shoot);

                        if (!wasFirePressed) {
                            wasFirePressed = true;
                            //SetTransition(currentAnimationState | AnimState.TRANSITION_IDLE_TO_SHOOT, false);
                        }

                        FireWeapon();
                    }
                }
            } else if (wasFirePressed) {
                wasFirePressed = false;
                // ToDo: Handle crouch, vine
                // ToDo: (... & 0x7) is only quickfix
                if (!inWater && !isAirboard && !isLifting) {
                    SetTransition((currentAnimationState & (AnimState)0x00000007) | AnimState.TransitionShootToIdle, true);
                }
            }

            // ToDo: Debug keys only
            if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.X)) {
                do {
                    currentWeapon = (WeaponType)((int)(currentWeapon + 1) % (int)WeaponType.Count);
                } while (weaponAmmo[(int)currentWeapon] == 0);

                // Only played on 1-9 numkey press
                //PlaySound("PLAYER_SWITCH_AMMO");

                attachedHud?.ChangeCurrentWeapon(currentWeapon, weaponUpgrades[(int)currentWeapon]);
            }

#if DEBUG
            if (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.T)) {
                WarpToPosition(new Vector2(Transform.Pos.X, Transform.Pos.Y - (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C) ? 500f : 150f)));
            } else if (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.G)) {
                WarpToPosition(new Vector2(Transform.Pos.X, Transform.Pos.Y + (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C) ? 500f : 150f)));
            } else if (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.F)) {
                WarpToPosition(new Vector2(Transform.Pos.X - (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C) ? 500f : 150f), Transform.Pos.Y));
            } else if (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.H)) {
                WarpToPosition(new Vector2(Transform.Pos.X + (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C) ? 500f : 150f), Transform.Pos.Y));
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.N)) {
                api.InitLevelChange(ExitType.Warp, null);
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.J)) {
                coins += 5;
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.U)) {
                for (int i = 0; i < weaponAmmo.Length; i++) {
                    weaponAmmo[i] = 100;
                }
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.I)) {
                AddFastFire(1);
                //api.AddActor(api.EventSpawner.SpawnEvent(ActorInstantiationFlags.None, EventType.TriggerCrate, Transform.Pos + new Vector3(0f, -100f, 20f), new ushort[8]));
            }
#else
            // ToDo: Remove this in release
            if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.N)) {
                api.InitLevelChange(ExitType.Warp, null);
            }
#endif
        }

        protected override void OnHitFloorHook()
        {
            Vector3 pos = Transform.Pos;
            if (api.EventMap.IsHurting(pos.X, pos.Y + 24)) {
                TakeDamage(speedX * 0.25f);
            } else if (!canJump && !inWater && !isAirboard) {
                PlaySound("COMMON_LAND", 0.8f);

                if (MathF.Rnd.NextFloat() < 0.6f) {
                    Explosion.Create(api, pos + new Vector3(0f, 20f, 0f), Explosion.TinyDark);
                }
            }

            canDoubleJump = true;
            isFreefall = false;
            //isSpring = false;

            collisionFlags |= CollisionFlags.IsSolidObject;
        }

        protected override void OnHitCeilingHook()
        {
            Vector3 pos = Transform.Pos;
            if (api.EventMap.IsHurting(pos.X, pos.Y - 4f)) {
                TakeDamage(speedX * 0.25f);
            }
        }

        protected override void OnHitWallHook()
        {
            Vector3 pos = Transform.Pos;
            if (api.EventMap.IsHurting(pos.X + (speedX > 0f ? 1f : -1f) * 16f, pos.Y)) {
                TakeDamage(speedX * 0.25f);
            }
        }

        protected override void OnUpdateHitbox()
        {
            // ToDo: Figure out how to use hot/coldspots properly.
            // The sprite is always located relative to the hotspot.
            // The coldspot is usually located at the ground level of the sprite,
            // but for falling sprites for some reason somewhere above the hotspot instead.
            // It is absolutely important that the position of the hitbox stays constant
            // to the hotspot, though; otherwise getting stuck at walls happens all the time.
            Vector3 pos = Transform.Pos;

            currentHitbox = new Hitbox(pos.X - 14f, pos.Y + 8f - 12f, pos.X + 14f, pos.Y + 8f + 12f);
        }

        public override bool Deactivate(int x, int y, int tileDistance)
        {
            // Player can never be deactivated
            return false;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            if (currentTransitionState != AnimState.TransitionDeath) {
                isInvulnerable = true;
                currentTransitionCancellable = true;
                SetPlayerTransition(AnimState.TransitionDeath, false, true, SpecialMoveType.None, delegate {
                    // ToDo: Animation is not played, instant switch to main menu ???
                    if (lives > 1) {
                        lives--;

                        // Reset health and remove one life
                        health = maxHealth;

                        // Negate all possible movement effects etc.
                        currentTransitionState = AnimState.Idle;

                        canJump = false;
                        externalForceX = 0f;
                        externalForceY = 0f;
                        internalForceY = 0f;
                        speedX = 0f;
                        speedY = 0f;
                        controllable = true;
                        isAirboard = false;

                        PlayerCorpse corpse = new PlayerCorpse();
                        corpse.OnAttach(new ActorInstantiationDetails {
                            Api = api,
                            Pos = Transform.Pos,
                            Params = new[] { (ushort)(playerType), (ushort)(isFacingLeft ? 1 : 0) }
                        });
                        api.AddActor(corpse);

                        isInvulnerable = false;

                        SetAnimation(AnimState.Idle);
                        collisionFlags |= CollisionFlags.ApplyGravitation;

                        // Remove fast fires
                        weaponUpgrades[(int)WeaponType.Blaster] = (byte)(weaponUpgrades[(int)WeaponType.Blaster] & 0x1);

                        // Return to the last save point
                        MoveInstantly(savePointPos, MoveType.Absolute, true);
                        api.AmbientLight = savePointLight;
                        api.WarpCameraToTarget(this);
                    } else {
                        api.HandleGameOver();
                    }
                });
            }
            return false;
        }

        private void UpdateSpeedBasedAnimation(float timeMult, float lastX, float lastForceX)
        {
            if (controllable) {
                float posX = Transform.Pos.X;

                AnimState oldState = currentAnimationState;
                AnimState newState;
                if (inWater) {
                    newState = AnimState.Swim;
                } else if (isAirboard) {
                    newState = AnimState.Airboard;
                } else if (isLifting) {
                    newState = AnimState.Lift;
                } else if (canJump && isActivelyPushing && (pushFrames > 0 || (carryingObject == null && Math.Abs((posX - lastX) - (speedX * timeMult)) > 0.1f && Math.Abs(/*externalForceX*/lastForceX) < float.Epsilon && (isFacingLeft ^ (speedX > 0))))) {
                    newState = AnimState.Push;
                } else {

                    // determine current animation last bits from speeds
                    // it's okay to call setAnimation on every tick because it doesn't do
                    // anything if the animation is the same as it was earlier

                    // Only certain ones don't need to be preserved from earlier state, others should be set as expected
                    AnimState composite = unchecked(currentAnimationState & (AnimState)0xFFF8BFE0);
                    if (isActivelyPushing) {
                        if (Math.Abs(speedX) > MAX_RUNNING_SPEED + float.Epsilon) {
                            // Shift-running, speed is more than 3px/frame
                            composite |= AnimState.Dash;
                        } else if (Math.Abs(speedX) > 2) {
                            // Running, speed is between 2px and 3px/frame
                            composite |= AnimState.Run;
                        } else if (Math.Abs(speedX) > float.Epsilon) {
                            // Walking, speed is less than 2px/frame (mostly a transition zone)
                            composite |= AnimState.Walk;
                        }
                    } else {
                        if (inIdleTransition && Math.Abs(speedX) <= float.Epsilon) {
                            CancelTransition();
                        } else if (wasFirePressed) {
                            composite |= AnimState.Shoot;
                        }
                    }

                    if (suspendType != SuspendType.None) {
                        composite |= AnimState.Hook;
                    } else {
                        if (canJump) {
                            // Grounded, no vertical speed
                        } else if (speedY < -float.Epsilon) {
                            // Jumping, ver. speed is negative
                            if (isSpring) {
                                composite |= AnimState.Spring;
                            } else {
                                composite |= AnimState.Jump;
                            }
                            
                        } else if (isFreefall) {
                            // Free falling, ver. speed is positive
                            composite |= AnimState.Freefall;
                            isSpring = false;
                        } else {
                            // Falling, ver. speed is positive
                            composite |= AnimState.Fall;
                            isSpring = false;
                        }
                    }

                    newState = composite;
                }
                SetAnimation(newState);

                switch (oldState) {
                    case AnimState.Run:
                        if (newState == AnimState.Idle || newState == AnimState.Walk) {
                            inIdleTransition = true;
                            SetTransition(AnimState.TransitionRunToIdle, true, delegate {
                                inIdleTransition = false;
                            });
                        } else if (newState == AnimState.Dash) {
                            SetTransition(AnimState.TransitionRunToDash, true);
                        }
                        break;
                    case AnimState.Dash:
                        if (newState == AnimState.Idle) {
                            inIdleTransition = true;
                            SetTransition(AnimState.TransitionDashToIdle, true, delegate {
                                inIdleTransition = false;
                                SetTransition(AnimState.TransitionRunToIdle, true);
                            });
                        }
                        break;
                    case AnimState.Fall:
                    case AnimState.Freefall:
                        if (newState == AnimState.Idle) {
                            SetTransition(AnimState.TransitionFallToIdle, true);
                        }
                        break;
                    case AnimState.Idle:
                        if (newState == AnimState.Jump) {
                            SetTransition(AnimState.TransitionIdleToJump, true);
                        }
                        break;
                    //case AnimState.SHOOT:
                    //    if (newState == AnimState.IDLE) {
                    //        SetTransition(AnimState.TRANSITION_IDLE_SHOOT_TO_IDLE, true);
                    //    }
                    //    break;
                }
            }
        }

        private void PushSolidObjects()
        {
            if (pushFrames > 0) {
                pushFrames--;
            }

            if (canJump && controllable && isActivelyPushing && MathF.Abs(speedX) > float.Epsilon) {
                ActorBase collider;
                Hitbox hitbox = currentHitbox + new Vector2(speedX < 0 ? -2f : 2f, 0f);
                if (!api.IsPositionEmpty(ref hitbox, false, this, out collider)) {
                    SolidObjectBase solidObject = collider as SolidObjectBase;
                    if (solidObject != null && solidObject.Push(speedX < 0)) {
                        pushFrames = 3;

                        //if (currentHitbox.Top >= solidObject.Hitbox.Top) {
                        //    lift = true;
                        //}
                    }
                }
            } else if ((collisionFlags & CollisionFlags.IsSolidObject) != 0) {
                ActorBase collider;
                Hitbox hitbox = currentHitbox + new Vector2(0f, -2f);
                if (!api.IsPositionEmpty(ref hitbox, false, this, out collider)) {
                    SolidObjectBase solidObject = collider as SolidObjectBase;
                    if (solidObject != null) {

                        if (currentHitbox.Top >= solidObject.Hitbox.Top && !isLifting) {
                            isLifting = true;

                            SetTransition(AnimState.TransitionLiftStart, true);
                        }
                    } else {
                        isLifting = false;
                    }
                } else {
                    isLifting = false;
                }
            } else {
                isLifting = false;
            }
        }

        private void CheckEndOfSpecialMoves(float timeMult)
        {
            // Buttstomp
            if (currentSpecialMove == SpecialMoveType.Buttstomp && (canJump || suspendType != SuspendType.None)) {
                EndDamagingMove();
                if (suspendType == SuspendType.None) {
                    SetTransition(AnimState.TransitionButtstompEnd, false, delegate {
                        controllable = true;
                    });
                } else {
                    controllable = true;
                }
            }

            // Uppercut
            if (currentSpecialMove == SpecialMoveType.Uppercut && currentTransitionState == AnimState.Idle && ((currentAnimationState & AnimState.Uppercut) != 0) && speedY > -2 && !canJump) {
                EndDamagingMove();
                if (suspendType == SuspendType.None) {
                    SetTransition(AnimState.TransitionUppercutEnd, false, delegate {
                        controllable = true;
                    });
                } else {
                    controllable = true;
                }
            }

            // Sidekick
            if (currentSpecialMove == SpecialMoveType.Sidekick && currentTransitionState == AnimState.Idle /*&& ((currentAnimationState & AnimState.UPPERCUT) != 0)*/ && Math.Abs(speedX) < 0.01f) {
                EndDamagingMove();
                controllable = true;
                if (suspendType == SuspendType.None) {
                    SetTransition(AnimState.TransitionUppercutEnd, false);
                }
            }

            // Copter Ears
            bool cancelCopter;
            if ((currentAnimationState & AnimState.Copter) != 0) {
                cancelCopter = (canJump || suspendType != SuspendType.None || copterFramesLeft <= 0f);

                copterFramesLeft -= timeMult;
            } else {
                cancelCopter = ((currentAnimationState & AnimState.Fall) != 0 && copterFramesLeft > 0f);
            }

            if (cancelCopter) {
                copterFramesLeft = 0f;
                SetAnimation(currentAnimationState & ~AnimState.Copter);
                if (!isAttachedToPole) {
                    collisionFlags |= CollisionFlags.ApplyGravitation;
                }
            }
        }

        private void CheckDestructibleTiles()
        {
            TileMap tiles = api.TileMap;
            if (tiles == null) {
                return;
            }

            //Hitbox tileCollisionHitbox = currentHitbox.Extend(1f).Extend(-speedX, -speedY, speedX, speedY);
            Hitbox tileCollisionHitbox = currentHitbox + new Vector2(speedX + externalForceX, speedY - externalForceY);

            // Buttstomp/etc. tiles checking
            if (currentSpecialMove != SpecialMoveType.None || isSugarRush) {
                int destroyedCount = tiles.CheckSpecialDestructible(ref tileCollisionHitbox);
                AddScore(destroyedCount * 50);

                ActorBase solidObject;
                if (!(api.IsPositionEmpty(ref tileCollisionHitbox, false, this, out solidObject))) {
                    {
                        TriggerCrate collider = solidObject as TriggerCrate;
                        if (collider != null) {
                            collider.DecreaseHealth(1, this);
                        }
                    }
                    {
                        GenericContainer collider = solidObject as GenericContainer;
                        if (collider != null) {
                            collider.DecreaseHealth(1, this);
                        }
                    }
                    {
                        PowerUpWeaponMonitor collider = solidObject as PowerUpWeaponMonitor;
                        if (collider != null) {
                            collider.DestroyAndApplyToPlayer(this);
                        }
                    }
                    {
                        PowerUpSwapMonitor collider = solidObject as PowerUpSwapMonitor;
                        if (collider != null) {
                            collider.DestroyAndApplyToPlayer(this);
                        }
                    }
                }
            }

            tileCollisionHitbox = currentHitbox.Extend(MathF.Abs(speedX), MathF.Abs(speedY)).Extend(3);

            // Speed tiles checking
            if (MathF.Abs(speedX) > float.Epsilon || MathF.Abs(speedY) > float.Epsilon || isSugarRush) {
                int destroyedCount = tiles.CheckSpecialSpeedDestructible(ref tileCollisionHitbox,
                    isSugarRush ? 64.0 : MathF.Max(MathF.Abs(speedX), MathF.Abs(speedY)));

                AddScore(destroyedCount * 50);
            }

            tiles.CheckCollapseDestructible(ref tileCollisionHitbox);
        }

        private void CheckSuspendedStatus()
        {
            TileMap tiles = api.TileMap;
            if (tiles == null) {
                return;
            }

            Vector3 pos = Transform.Pos;

            AnimState currentState = currentAnimationState;
            SuspendType newSuspendState = tiles.GetTileSuspendState(pos.X, pos.Y - 1f);

            if (newSuspendState == suspendType) {
                return;
            }

            if (newSuspendState != SuspendType.None) {
                if (currentSpecialMove != SpecialMoveType.Uppercut) {

                    suspendType = newSuspendState;
                    collisionFlags &= ~CollisionFlags.ApplyGravitation;

                    if (speedY > 0 && newSuspendState == SuspendType.Vine) {
                        PlaySound("PLAYER_VINE_ATTACH");
                    }

                    speedY = 0;
                    externalForceY = 0;
                    isFreefall = false;
                    isSpring = false;

                    if (newSuspendState == SuspendType.Hook) {
                        speedX = 0;
                    }

                    // Move downwards until we're on the standard height
                    while (tiles.GetTileSuspendState(pos.X, pos.Y /*- 5*/- 1) != SuspendType.None) {
                        MoveInstantly(new Vector2(0f, 1f), MoveType.Relative, true);
                        pos = Transform.Pos;
                    }
                    MoveInstantly(new Vector2(0f, -1f), MoveType.Relative, true);
                }
            } else {
                suspendType = SuspendType.None;
                if ((currentState & (AnimState.Buttstomp | AnimState.Copter)) == 0 && !isAttachedToPole) {
                    collisionFlags |= CollisionFlags.ApplyGravitation;
                }
            }
        }

        private void HandleWater()
        {
            if (inWater) {
                if (Transform.Pos.Y >= api.WaterLevel) {
                    collisionFlags &= ~CollisionFlags.ApplyGravitation;

                    if (Math.Abs(speedX) > 1f || Math.Abs(speedY) > 1f) {

                        float angle;
                        if (speedX == 0f) {
                            if (isFacingLeft) {
                                angle = MathF.Atan2(-speedY, -float.Epsilon);
                            } else {
                                angle = MathF.Atan2(speedY, float.Epsilon);
                            }
                        } else if (speedX < 0f) {
                            angle = MathF.Atan2(-speedY, -speedX);
                        } else {
                            angle = MathF.Atan2(speedY, speedX);
                        }

                        if (angle > MathF.Pi) {
                            angle = angle - MathF.TwoPi;
                        }

                        Transform.Angle = MathF.Clamp(angle, -MathF.PiOver3, MathF.PiOver3);
                    }

                    if (currentTransitionState == AnimState.Idle) {
                        renderer.AnimDuration = MathF.Max(currentAnimation.FrameDuration + 1f - new Vector2(speedX, speedY).Length * 0.26f, 0.4f);
                    }

                } else {
                    inWater = false;

                    collisionFlags |= CollisionFlags.ApplyGravitation;
                    canJump = true;

                    Transform.Angle = 0;

                    SetAnimation(AnimState.Jump);
                }
            } else {
                if (Transform.Pos.Y >= api.WaterLevel) {
                    inWater = true;

                    Explosion.Create(api, Transform.Pos - new Vector3(0f, 4f, 0f), Explosion.WaterSplash);

                    controllable = true;
                    EndDamagingMove();
                }
            }
        }

        private void HandleAreaEvents()
        {
            EventMap events = api.EventMap;
            if (events == null) {
                return;
            }

            Vector3 pos = Transform.Pos;

            ushort[] p = null;
            EventType tileEvent = events.GetEventByPosition(pos.X, pos.Y, ref p);
            switch (tileEvent) {
                case EventType.LightSet: { // Intensity, Red, Green, Blue, Flicker
                    // ToDo: Change only player view, handle multiplayer
                    api.AmbientLight = p[0] * 0.01f;
                    break;
                }
                case EventType.WarpOrigin: { // Warp ID, Set Lap, Show Anim, Fast
                    if (currentTransitionState == AnimState.Idle || currentTransitionCancellable) {
                        Vector2 c = events.GetWarpTarget(p[0]);
                        if (c.X != -1f && c.Y != -1f) {
                            WarpToPosition(c);
                        }
                    }
                    break;
                }
                case EventType.ModifierHPole: {
                    InitialPoleStage(true);
                    break;
                }
                case EventType.ModifierVPole: {
                    InitialPoleStage(false);
                    break;
                }
                case EventType.ModifierTube: { // XSpeed, YSpeed, Trig Sample, Become Noclip, Noclip Only, Wait Time
                    // ToDo: Implement other parameters
                    // ToDo: Doesn't work well sometimes
                    EndDamagingMove();
                    // ToDo: Check this...
                    controllable = true;
                    SetPlayerTransition(AnimState.Dash | AnimState.Jump, false, false, SpecialMoveType.None, delegate {
                        collisionFlags |= CollisionFlags.ApplyGravitation;
                    });

                    collisionFlags &= ~CollisionFlags.ApplyGravitation;
                    speedX = speedY = 0;

                    if (p[0] != 0) {
                        float moveX = unchecked((short)p[0]) * 1.4f;

                        //pos.X += moveX;
                        pos.Y = (float)(Math.Floor(pos.Y / 32) * 32) + 8;
                        Transform.Pos = pos;

                        speedX = moveX;
                        //externalForceX = moveX;
                        MoveInstantly(new Vector2(speedX, 0f), MoveType.RelativeTime, true);
                    } else {
                        float moveY = unchecked((short)p[1]) * 1.4f;

                        pos.X = (float)(Math.Floor(pos.X / 32) * 32) + 16;
                        //pos.Y += moveY;
                        Transform.Pos = pos;

                        speedY = moveY;
                        //externalForceY = -moveY;
                        MoveInstantly(new Vector2(0f, speedY), MoveType.RelativeTime, true);
                    }
                    break;
                }
                case EventType.AreaEOL: { // ExitType, Fast (No score count, only black screen), TextID, TextOffset, Coins
                    if (!levelExiting) {
                        // ToDo: Implement Fast parameter
                        if (p[4] <= coins) {
                            coins -= p[4];

                            string nextLevel;
                            if (p[2] == 0) {
                                nextLevel = null;
                            } else {
                                nextLevel = api.GetLevelText(p[2]).SubstringByOffset('|', p[3]);
                            }
                            api.InitLevelChange((ExitType)p[0], nextLevel);
                            PlaySound("PLAYER_EOL");
                        } else if (bonusWarpTimer <= 0f) {
                            attachedHud?.ShowCoins(coins);
                            PlaySound("PLAYER_BONUS_WARP_NOT_ENOUGH_COINS");

                            bonusWarpTimer = 400f;
                        }
                    }
                    break;
                }
                case EventType.AreaText: { // Text, TextOffset, Vanish
                    string text = api.GetLevelText(p[0]);
                    if (p[1] != 0) {
                        text = text.SubstringByOffset('|', p[1]);
                    }
                    if (!string.IsNullOrEmpty(text)) {
                        attachedHud?.ShowLevelText(text);
                    }
                    if (p[2] != 0) {
                        api.EventMap.StoreTileEvent((int)(pos.X / 32), (int)(pos.Y / 32), EventType.Empty);
                    }
                    break;
                }
                case EventType.AreaCallback: { // Function, Param, Vanish
                    // ToDo: Call function #{p[0]}(sender, p[1]); implement level extensions
                    attachedHud?.ShowLevelText("\f[s:75]\f[w:95]\f[c:6]\n\n\n\nWARNING: Callbacks aren't implemented yet. (" + p[0] + ", " + p[1] + ")");
                    if (p[2] != 0) {
                        api.EventMap.StoreTileEvent((int)(pos.X / 32), (int)(pos.Y / 32), EventType.Empty);
                    }
                    break;
                }
                case EventType.AreaActivateBoss: { // Music
                    // ToDo: Implement bosses somehow + music + camera lock
                    api.ActivateBoss(p[0]);

                    // ToDo: Fix deactivating of boss on death, and remove this SavePoint
                    savePointPos = Transform.Pos.Xy;
                    savePointLight = api.AmbientLight;
                    break;
                }
                case EventType.AreaFlyOff: {
                    if (isAirboard) {
                        isAirboard = false;

                        collisionFlags |= CollisionFlags.ApplyGravitation;
                        canJump = true;

                        SetAnimation(AnimState.Fall);
                    }
                    break;
                }
                case EventType.TriggerZone: { // Trigger ID, Turn On, Switch
                    TileMap tiles = api.TileMap;
                    if (tiles != null) {
                        // ToDo: Implement Switch parameter
                        tiles.SetTrigger(p[0], p[1] != 0);
                    }
                    break;
                }
                case EventType.ModifierDeath: {
                    DecreaseHealth(int.MaxValue);
                    break;
                }
                case EventType.ModifierSetWater: { // Height, Instant, Lighting
                    // ToDo: Implement Instant (non-instant transition), Lighting
                    api.WaterLevel = p[0];
                    break;
                }
            }

            // ToDo: Implement Slide modifier with JJ2+ parameter

            // Check floating from each corner of an extended hitbox
            // Player should not pass from a single tile wide gap if the columns left or right have
            // float events, so checking for a wider box is necessary.
            const float extendedHitbox = 2f; // 5f

            if ((events.GetEventByPosition(pos.X, pos.Y, ref p) == EventType.AreaFloatUp) ||
                (events.GetEventByPosition(currentHitbox.Left - extendedHitbox, currentHitbox.Top - extendedHitbox, ref p) == EventType.AreaFloatUp) ||
                (events.GetEventByPosition(currentHitbox.Right + extendedHitbox, currentHitbox.Top - extendedHitbox, ref p) == EventType.AreaFloatUp) ||
                (events.GetEventByPosition(currentHitbox.Right + extendedHitbox, currentHitbox.Bottom + extendedHitbox, ref p) == EventType.AreaFloatUp) ||
                (events.GetEventByPosition(currentHitbox.Left - extendedHitbox, currentHitbox.Bottom + extendedHitbox, ref p) == EventType.AreaFloatUp)
               ) {
                if ((collisionFlags & CollisionFlags.ApplyGravitation) != 0) {
                    float gravity = api.Gravity;

                    externalForceY = gravity * 2;
                    speedY = MathF.Min(gravity, speedY);
                } else {
                    //speedY = Math.Min(api.Gravity * 10, speedY);
                    speedY -= api.Gravity * 1.2f;
                }
            }

            if ((events.GetEventByPosition(pos.X, pos.Y, ref p) == EventType.AreaHForce) ||
                (events.GetEventByPosition(currentHitbox.Left - extendedHitbox, currentHitbox.Top - extendedHitbox, ref p) == EventType.AreaHForce) ||
                (events.GetEventByPosition(currentHitbox.Right + extendedHitbox, currentHitbox.Top - extendedHitbox, ref p) == EventType.AreaHForce) ||
                (events.GetEventByPosition(currentHitbox.Right + extendedHitbox, currentHitbox.Bottom + extendedHitbox, ref p) == EventType.AreaHForce) ||
                (events.GetEventByPosition(currentHitbox.Left - extendedHitbox, currentHitbox.Bottom + extendedHitbox, ref p) == EventType.AreaHForce)
               ) {
                //speedX += (p[3] - p[2]) * 0.1f;//0.4f
                if (/*!canJump &&*/ (p[5] != 0 || p[4] != 0)) {
                    MoveInstantly(new Vector2((p[5] - p[4]) * /*0.6f*/0.4f, 0), MoveType.RelativeTime);
                }
            }

            //
            if (canJump) {
                tileEvent = events.GetEventByPosition(pos.X, pos.Y + 32, ref p);
                switch (tileEvent) {
                    case EventType.AreaHForce: {
                        if (p[1] != 0 || p[0] != 0) {
                            MoveInstantly(new Vector2((p[1] - p[0]) * 0.4f, 0), MoveType.RelativeTime);
                        }
                        if (p[3] != 0 || p[2] != 0) {
                            speedX += (p[3] - p[2]) * 0.1f;
                        }
                        break;
                    }
                }
            }
        }

        private void HandleActorCollisions()
        {
            List<ActorBase> collisions = api.FindCollisionActors(this);
            bool removeSpecialMove = false;

            for (int i = 0; i < collisions.Count; i++) {
                // Different things happen with different actor types

                if (currentSpecialMove != SpecialMoveType.None || isSugarRush) {
                    TurtleShell collider = collisions[i] as TurtleShell;
                    if (collider != null) {
                        collider.DecreaseHealth(int.MaxValue, this);

                        if ((currentAnimationState & AnimState.Buttstomp) != 0) {
                            removeSpecialMove = true;
                            speedY *= -0.6f;
                        }

                        continue;
                    }
                }

                {
                    EnemyBase collider = collisions[i] as EnemyBase;
                    if (collider != null) {
                        if (currentSpecialMove != SpecialMoveType.None || isSugarRush) {
                            collider.DecreaseHealth(4, this);

                            Explosion.Create(api, collider.Transform.Pos, Explosion.Small);

                            if (isSugarRush) {
                                if (canJump) {
                                    speedY = 3;
                                    canJump = false;
                                    externalForceY = 0.6f;
                                }
                                speedY *= -0.5f;
                            }
                            if ((currentAnimationState & AnimState.Buttstomp) != 0) {
                                removeSpecialMove = true;
                                speedY *= -0.6f;
                                canJump = true;
                            }
                        } else {
                            if (collider.CanHurtPlayer) {
                                TakeDamage(4 * (Transform.Pos.X > collider.Transform.Pos.X ? 1 : -1));
                            }
                        }
                        continue;
                    }
                }
                {
                    SavePoint collider = collisions[i] as SavePoint;
                    if (collider != null) {
                        if (collider.ActivateSavePoint()) {
                            savePointPos = collider.Transform.Pos.Xy + new Vector2(0f, -20f);
                            savePointLight = api.AmbientLight;
                        }
                        continue;
                    }
                }
                {
                    //SignEol collider = collisions[i] as SignEol;
                    //if (collider != null) {
                    //    if (!levelExiting) {
                    //        api.InitLevelChange(collider.ExitType, null);
                    //        PlaySound("PLAYER_EOL");
                    //    }
                    //    continue;
                    //}
                }
                {
                    Spring spring = collisions[i] as Spring;
                    if (spring != null) {
                        // Collide only with hitbox
                        if (spring.Hitbox.Overlaps(ref currentHitbox)) {
                            removeSpecialMove = true;

                            Vector2 force = spring.Activate();
                            int sign = ((force.X + force.Y) > float.Epsilon ? 1 : -1);
                            if (MathF.Abs(force.X) > float.Epsilon) {
                                speedX = (4 + MathF.Abs(force.X)) * sign;
                                externalForceX = force.X;
                                SetPlayerTransition(AnimState.Dash | AnimState.Jump, true, true, SpecialMoveType.None);
                                // ToDo: ...
                                controllableTimeout = 20f;
                            } else if (MathF.Abs(force.Y) > float.Epsilon) {
                                speedY = (4 + MathF.Abs(force.Y)) * sign;
                                externalForceY = -force.Y;

                                if (sign > 0) {
                                    //controllable = false;
                                    removeSpecialMove = false;
                                    currentSpecialMove = SpecialMoveType.Buttstomp;
                                    SetAnimation(AnimState.Buttstomp);
                                } else {
                                    removeSpecialMove = true;
                                    isSpring = true;
                                }
                            } else {
                                continue;
                            }
                            canJump = false;

                            PlaySound("PLAYER_SPRING");
                        }
                        continue;
                    }
                }
                {
                    PinballBumper bumper = collisions[i] as PinballBumper;
                    if (bumper != null) {
                        removeSpecialMove = true;
                        canJump = false;

                        Vector2 force = bumper.Activate(this);
                        if (force != Vector2.Zero) {
                            speedX += force.X * 0.4f;
                            speedY += force.Y * 0.4f;
                            externalForceX += force.X * 0.04f;
                            externalForceY -= force.Y * 0.04f;

                            // ToDo: Check this...
                            AddScore(500);
                        }

                        continue;
                    }
                }
                {
                    PinballPaddle paddle = collisions[i] as PinballPaddle;
                    if (paddle != null) {
                        Vector2 force = paddle.Activate(this);
                        if (force != Vector2.Zero) {
                            speedX = force.X;
                            speedY = force.Y;
                        }
                        continue;
                    }
                }
                {
                    Collectible collectible = collisions[i] as Collectible;
                    if (collectible != null) {
                        collectible.Collect(this);
                    }
                }

                if (!isAirboard) {
                    AirboardGenerator airboard = collisions[i] as AirboardGenerator;
                    if (airboard != null) {
                        if (airboard.Activate()) {
                            isAirboard = true;

                            controllable = true;
                            EndDamagingMove();
                            collisionFlags &= ~CollisionFlags.ApplyGravitation;

                            speedY = 0f;
                            externalForceY = 0f;

                            MoveInstantly(new Vector2(0f, -16f), MoveType.Relative);
                        }
                    }
                }

                if (currentTransitionState == AnimState.Idle || currentTransitionCancellable) {
                    BonusWarp collider = collisions[i] as BonusWarp;
                    if (collider != null) {
                        if (collider.Cost <= coins) {
                            coins -= collider.Cost;
                            WarpToPosition(collider.WarpTarget);

                            // Convert remaing coins to gems
                            gems += coins;
                            coins = 0;
                        } else if (bonusWarpTimer <= 0f) {
                            attachedHud?.ShowCoins(coins);
                            PlaySound("PLAYER_BONUS_WARP_NOT_ENOUGH_COINS");

                            bonusWarpTimer = 400f;
                        }
                    }
                }
            }

            if (removeSpecialMove) {
                controllable = true;
                EndDamagingMove();
            }
        }

        private void EndDamagingMove()
        {
            currentSpecialMove = SpecialMoveType.None;
            collisionFlags |= CollisionFlags.ApplyGravitation;
            SetAnimation(currentAnimationState & ~(AnimState.Uppercut /*| AnimState.Sidekick*/ | AnimState.Buttstomp));
        }

        private bool SetPlayerTransition(AnimState state, bool cancellable, bool removeControl, SpecialMoveType specialMove, Action callback = null)
        {
            if (removeControl) {
                controllable = false;
            }

            currentSpecialMove = specialMove;
            return SetTransition(state, cancellable, callback);
        }

        public void TakeDamage(float pushForce)
        {
            if (!isInvulnerable && !levelExiting) {
                DecreaseHealth(1, null);

                internalForceY = 0f;
                speedX = 0f;
                canJump = false;
                if (health > 0) {
                    externalForceX = pushForce;

                    if (!inWater && !isAirboard) {
                        speedY = -6.5f;

                        collisionFlags |= CollisionFlags.ApplyGravitation;
                        SetAnimation(AnimState.Idle);
                    } else {
                        speedY = -1f;
                    }

                    SetPlayerTransition(AnimState.Hurt, false, true, SpecialMoveType.None, delegate {
                        controllable = true;
                    });
                    SetInvulnerability(180f, true);

                    PlaySound("PLAYER_HURT");
                } else {
                    externalForceX = 0f;
                    speedY = 0f;

                    PlaySound("PLAYER_DIE");
                }
            }
        }

        public void WarpToPosition(Vector2 pos)
        {
            //inFreefall = inFreefall || (!canJump && speedY > 14f);
            SetPlayerTransition(isFreefall ? AnimState.TransitionWarpInFreefall : AnimState.TransitionWarpIn, false, true, SpecialMoveType.None, delegate {
                Vector3 posOld = Transform.Pos;
                bool isFar = (new Vector2(posOld.X - pos.X, posOld.Y - pos.Y).Length > 250);

                MoveInstantly(pos, MoveType.Absolute, true);
                PlaySound("COMMON_WARP_OUT");

                if (isFar) {
                    api.WarpCameraToTarget(this);
                }

                isFreefall = isFreefall || CanFreefall();
                SetPlayerTransition(isFreefall ? AnimState.TransitionWarpOutFreefall : AnimState.TransitionWarpOut, false, true, SpecialMoveType.None, delegate {
                    isInvulnerable = false;
                    collisionFlags |= CollisionFlags.ApplyGravitation;
                    controllable = true;
                });
            });

            EndDamagingMove();
            isInvulnerable = true;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;
            speedX = 0;
            speedY = 0;
            externalForceX = 0;
            externalForceY = 0;
            internalForceY = 0;
            PlaySound("COMMON_WARP_IN");
        }

        private void InitialPoleStage(bool horizontal)
        {
            if (isAttachedToPole) {
                return;
            }

            Vector3 pos = Transform.Pos;
            int x = (int)pos.X / 32;
            int y = (int)pos.Y / 32;

            if (lastPoleTime > 0f && lastPolePos.X == x && lastPolePos.Y == y) {
                return;
            }

            lastPoleTime = 80f;
            lastPolePos = new Point2(x, y);

            float activeForce;
            if (horizontal) {
                //activeForce = speedX;
                activeForce = (Math.Abs(externalForceX) > 1f ? externalForceX : speedX);
            } else {
                activeForce = speedY;
            }
            bool positive = (activeForce > 0);

            pos.X = x * 32 + 16;
            pos.Y = y * 32 + 16;
            Transform.Pos = pos;

            speedX = 0;
            speedY = 0;
            externalForceX = 0;
            externalForceY = 0;
            internalForceY = 0;
            canJump = false;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;
            controllable = false;
            isAttachedToPole = true;

            AnimState poleAnim = (horizontal ? AnimState.TransitionPoleHSlow : AnimState.TransitionPoleVSlow);
            SetPlayerTransition(poleAnim, false, true, SpecialMoveType.None, delegate {
                NextPoleStage(horizontal, positive, 2);
            });

            PlaySound("PLAYER_POLE");
        }

        private void NextPoleStage(bool horizontal, bool positive, int stagesLeft)
        {
            if (stagesLeft > 0) {
                AnimState poleAnim = (horizontal ? AnimState.TransitionPoleH : AnimState.TransitionPoleV);
                SetPlayerTransition(poleAnim, false, true, SpecialMoveType.None, delegate {
                    NextPoleStage(horizontal, positive, stagesLeft - 1);
                });

                PlaySound("PLAYER_POLE");
            } else {
                int mp = (positive ? 1 : -1);
                if (horizontal) {
                    speedX = 10 * mp;
                    MoveInstantly(new Vector2(speedX, -1), MoveType.Relative, true);
                    externalForceX = (10 * mp);
                    isFacingLeft = !positive;
                } else {
                    MoveInstantly(new Vector2(0, mp * 16), MoveType.Relative, true);
                    speedY = (5 * mp);
                    externalForceY = (-2 * mp);
                }
                //controllable = true;
                collisionFlags |= CollisionFlags.ApplyGravitation;
                isAttachedToPole = false;

                controllableTimeout = 4f;

                lastPoleTime = 40f;
            }
        }

        private bool CanFreefall()
        {
            Vector3 pos = Transform.Pos;
            Hitbox hitbox = new Hitbox(pos.X - 14, pos.Y + 8 - 12, pos.X + 14, pos.Y + 8 + 12 + 100);
            return api.IsPositionEmpty(ref hitbox, true, this);
        }

        public void SetCarryingPlatform(MovingPlatform platform)
        {
            if (speedY < -float.Epsilon || inWater || isAirboard) {
                return;
            }

            carryingObject = platform;
            canJump = true;
            internalForceY = 0;
            speedY = 0;
        }

        private void FollowCarryingPlatform()
        {
            if (carryingObject == null) {
                return;
            }

            if (Math.Abs(speedY) > float.Epsilon || !controllable || (collisionFlags & CollisionFlags.ApplyGravitation) == 0) {
                carryingObject = null;
            } else {
                Vector2 delta = carryingObject.GetLocationDelta();
                delta.Y -= 1f;

                // ToDo: disregard the carrying object itself in this collision check to
                // eliminate the need of the correction pixel removed from the delta
                // and to make the ride even smoother (right now the pixel gap is clearly
                // visible when platforms go down vertically)
                // ToDo: Sometimes collides with Push animation
                // ToDo: [It seems it's fixed now] Player fall off at ~10 o'clock sometimes
                if (
                    !MoveInstantly(delta, MoveType.Relative) &&
                    !MoveInstantly(new Vector2(0f, delta.Y), MoveType.Relative)
                ) {
                    carryingObject = null;
                }
            }
        }

        public void SetInvulnerability(float time, bool blink)
        {
            if (time <= 0) {
                isInvulnerable = false;
                invulnerableTime = 0;
                return;
            }

            isInvulnerable = true;
            invulnerableTime = time;
        }

        public void AttachToHud(Hud hud)
        {
            if (attachedHud != null) {
                attachedHud.Owner = null;
            }

            attachedHud = hud;

            if (attachedHud != null) {
                attachedHud.Owner = this;
                attachedHud.ChangeCurrentWeapon(currentWeapon, weaponUpgrades[(int)currentWeapon]);
            }
        }

        public void AddScore(int plus)
        {
            score = Math.Min(score + plus, 999999999);
        }

        public bool AddHealth(int count)
        {
            const int healthLimit = 5;

            if (health >= healthLimit) {
                return false;
            }

            if (count < 0) {
                //if (health < maxHealth) {
                    health = Math.Max(maxHealth, healthLimit);
                //}
                PlaySound("PLAYER_CONSUME_MAX_CARROT");
            } else {
                health = Math.Min(health + count, healthLimit);
                if (maxHealth < health) {
                    maxHealth = health;
                }
                PlaySound("PLAYER_CONSUME_FOOD");
            }

            return true;
        }

        public void AddLives(int count)
        {
            lives += count;

            PlaySound("PLAYER_PICKUP_ONEUP");
        }

        public void AddCoins(int count)
        {
            coins += count;

            attachedHud?.ShowCoins(coins);
            PlaySound("PLAYER_PICKUP_COIN");
        }

        public void AddGems(int count)
        {
            gems += count;

            attachedHud?.ShowGems(gems);

            SoundResource resource;
            if (availableSounds.TryGetValue("PLAYER_PICKUP_GEM", out resource)) {
                SoundInstance instance = DualityApp.Sound.PlaySound3D(resource.Sound, this);
                // ToDo: Hardcoded volume
                instance.Volume = Settings.SfxVolume;
                instance.Pitch = MathF.Min(0.7f + gemsPitch * 0.05f, 1.3f);
            }

            gemsTimer = 120f;
            gemsPitch++;
        }

        public void ConsumeFood(bool isDrinkable) {
            // ToDo: Implement Sugar Rush (+ HUD)
            /*foodCounter += 1;
            if (foodCounter >= SUGAR_RUSH_THRESHOLD) {
                foodCounter = foodCounter % SUGAR_RUSH_THRESHOLD;
                if (!isSugarRush) {
                    api->pauseMusic();
                    playNonPositionalSound("PLAYER_SUGAR_RUSH");

                    isSugarRush = true;
                    osd->setSugarRushActive();
                    addTimer(21.548 * 70.0, false, [this]() {
                        isSugarRush = false;
                        api->resumeMusic();
                    });
                }
            }*/

            if (isDrinkable) {
                PlaySound("PLAYER_CONSUME_DRINK");
            } else {
                PlaySound("PLAYER_CONSUME_FOOD");
            }
        }

        public void ShowLevelText(string text)
        {
            attachedHud?.ShowLevelText(text);
        }

        public void TransformTo(PlayerType type)
        {
            playerType = type;

            Explosion.Create(api, Transform.Pos + new Vector3(-12f, -6f, -4f), Explosion.SmokeBrown);
            Explosion.Create(api, Transform.Pos + new Vector3(-8f, 28f, -4f), Explosion.SmokeBrown);
            Explosion.Create(api, Transform.Pos + new Vector3(12f, 10f, -4f), Explosion.SmokeBrown);

            Explosion.Create(api, Transform.Pos + new Vector3(0f, 12f, -6f), Explosion.SmokePoof);

            // Load new metadata
            switch (playerType) {
                case PlayerType.Jazz:
                    RequestMetadata("Interactive/PlayerJazz");
                    break;
                case PlayerType.Spaz:
                    RequestMetadata("Interactive/PlayerSpaz");
                    break;
                case PlayerType.Lori:
                    RequestMetadata("Interactive/PlayerLori");
                    break;
            }

            // Refresh animation state
            currentAnimation = null;
            SetAnimation(currentAnimationState);
        }
    }
}