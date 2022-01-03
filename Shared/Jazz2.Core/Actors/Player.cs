using System;
using System.Threading.Tasks;
using Duality;
using Duality.Audio;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Environment;
using Jazz2.Actors.Solid;
using Jazz2.Game;
using Jazz2.Game.Collisions;
using Jazz2.Game.Events;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;
using MathF = Duality.MathF;
using Jazz2.Game.Components;
#if !SERVER
using Jazz2.Game.UI;
#endif

namespace Jazz2.Actors
{
    public enum PlayerType
    {
        Jazz,
        Spaz,
        Lori,
        Frog
    }

    public partial class Player : ActorBase
    {
        public enum SpecialMoveType
        {
            None,
            Buttstomp,
            Uppercut,
            Sidekick
        }

        public enum Modifier
        {
            None,
            Airboard,
            Copter,
            LizardCopter
        }

        public enum LevelExitingState
        {
            None,
            Waiting,
            Transition,
            Ready
        }

        private const float MaxDashingSpeed = 9f;
        private const float MaxRunningSpeed = 4f;
        private const float MaxVineSpeed = 2f;
        private const float MaxDizzySpeed = 2.4f;
        private const float MaxShallowWaterSpeed = 3.6f;
        private const float Acceleration = 0.2f;
        private const float Deceleration = 0.22f;

        private int playerIndex;
        private bool isActivelyPushing, wasActivelyPushing;
        private bool controllable = true;
        private bool controllableExternal = true;
        private float controllableTimeout;

        private bool wasUpPressed, wasDownPressed, wasJumpPressed, wasFirePressed;

        private PlayerType playerType, playerTypeOriginal;
        private SpecialMoveType currentSpecialMove;
        private bool isAttachedToPole;
        private float copterFramesLeft, fireFramesLeft, pushFramesLeft, waterCooldownLeft;
        private LevelExitingState levelExiting;
        private bool isFreefall, inWater, isLifting, isSpring;
        private int inShallowWater = -1;
        private Modifier activeModifier;

        private bool inIdleTransition, inLedgeTransition;
        private MovingPlatform carryingObject;
        private SwingingVine currentVine;
        private bool canDoubleJump = true;
        private SoundInstance copterSound;

        private int lives, coins, foodEaten;
        private uint score;
        private Vector2 checkpointPos;
        private float checkpointLight;

        private float sugarRushLeft, sugarRushStarsTime;

        private int gems, gemsPitch;
        private float gemsTimer;
        private float bonusWarpTimer;

        private float invulnerableTime;
        private float invulnerableBlinkTime;

        private float idleTime;
        private float keepRunningTime;
        private float lastPoleTime;
        private Point2 lastPolePos;
        private float inTubeTime;
        private float dizzyTime;

#if !SERVER
        private Hud attachedHud;

        public Hud AttachedHud => attachedHud;
#endif

        public int Lives => lives;
        public uint Score => score;
        public PlayerType PlayerType => playerType;

        public bool CanBreakSolidObjects => (currentSpecialMove != SpecialMoveType.None || sugarRushLeft > 0f);

        public bool CanMoveVertically => (inWater || activeModifier != Modifier.None);

        public bool InWater => inWater;

        public bool IsControllableExternal
        {
            get => controllableExternal;
            set => controllableExternal = value;
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            playerTypeOriginal = (PlayerType)details.Params[0];
            playerType = playerTypeOriginal;
            playerIndex = details.Params[1];

            switch (playerType) {
                case PlayerType.Jazz:
                    await RequestMetadataAsync("Interactive/PlayerJazz");
                    break;
                case PlayerType.Spaz:
                    await RequestMetadataAsync("Interactive/PlayerSpaz");
                    break;
                case PlayerType.Lori:
                    await RequestMetadataAsync("Interactive/PlayerLori");
                    break;
                case PlayerType.Frog:
                    await RequestMetadataAsync("Interactive/PlayerFrog");
                    break;
            }

            SetAnimation(AnimState.Fall);

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 1.0f;
            light.RadiusNear = 40;
            light.RadiusFar = 110;

            weaponAmmo = new short[(int)WeaponType.Count];
            weaponUpgrades = new byte[(int)WeaponType.Count];

            weaponAmmo[(int)WeaponType.Blaster] = -1;

            CollisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithSolidObjects | CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation | CollisionFlags.IsSolidObject;

            health = 5;
            maxHealth = health;
            currentWeapon = WeaponType.Blaster;

            checkpointPos = details.Pos.Xy;
            checkpointLight = levelHandler.AmbientLightCurrent;
        }

        public void ReceiveLevelCarryOver(ExitType exitType, ref PlayerCarryOver carryOver)
        {
            lives = carryOver.Lives;
            score = carryOver.Score;
            foodEaten = carryOver.FoodEaten;
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
                PlaySound("WarpOut");

                CollisionFlags &= ~CollisionFlags.ApplyGravitation;

                isFreefall = CanFreefall();
                SetPlayerTransition(isFreefall ? AnimState.TransitionWarpOutFreefall : AnimState.TransitionWarpOut, false, true, SpecialMoveType.None, delegate {
                    isInvulnerable = false;
                    CollisionFlags |= CollisionFlags.ApplyGravitation;
                    controllable = true;
                });

#if !SERVER
                attachedHud?.BeginFadeIn(false);
#endif
            } else {
#if !SERVER
                attachedHud?.BeginFadeIn(true);
#endif
            }

            // Preload all weapons
            for (int i = 0; i < (int)WeaponType.Count; i++) {
                if (weaponAmmo[(int)currentWeapon] != 0) {
                    PreloadMetadata("Weapon/" + (WeaponType)i);
                }
            }
        }

        public PlayerCarryOver PrepareLevelCarryOver()
        {
            return new PlayerCarryOver {
                Type = playerType,

                Lives = lives,
                Score = score,
                FoodEaten = foodEaten,
                CurrentWeapon = currentWeapon,
                Ammo = weaponAmmo,
                WeaponUpgrades = weaponUpgrades
            };
        }

        public bool OnLevelChanging(ExitType exitType)
        {
            if (activeBird != null) {
                activeBird.FlyAway();
                activeBird = null;
            }

            if (levelExiting != LevelExitingState.None) {
                if (levelExiting == LevelExitingState.Waiting) {
                    if (canJump && speedX < 1f && speedY < 1f) {
                        levelExiting = LevelExitingState.Transition;

                        SetPlayerTransition(AnimState.TransitionEndOfLevel, false, true, SpecialMoveType.None, delegate {
                            renderer.Active = false;
#if !SERVER
                            attachedHud?.BeginFadeOut(true);
#endif
                            levelExiting = LevelExitingState.Ready;
                        });
                        PlaySound("EndOfLevel1");

                        CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                        speedX = 0;
                        speedY = 0;
                        externalForceX = 0f;
                        externalForceY = 0f;
                        internalForceY = 0f;
                    } else if (lastPoleTime <= 0f) {
                        // Waiting timeout - use warp transition instead
                        levelExiting = LevelExitingState.Transition;

                        SetPlayerTransition(isFreefall ? AnimState.TransitionWarpInFreefall : AnimState.TransitionWarpIn, false, true, SpecialMoveType.None, delegate {
                            renderer.Active = false;
#if !SERVER
                            attachedHud?.BeginFadeOut(false);
#endif
                            levelExiting = LevelExitingState.Ready;
                        });
                        PlaySound("WarpIn");

                        CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                        speedX = 0;
                        speedY = 0;
                        externalForceX = 0f;
                        externalForceY = 0f;
                        internalForceY = 0f;
                    }

                    return false;
                }

                return (levelExiting == LevelExitingState.Ready);
            }

            if (exitType == ExitType.Warp || exitType == ExitType.Bonus || inWater) {
                levelExiting = LevelExitingState.Transition;

                SetPlayerTransition(isFreefall ? AnimState.TransitionWarpInFreefall : AnimState.TransitionWarpIn, false, true, SpecialMoveType.None, delegate {
                    renderer.Active = false;
#if !SERVER
                    attachedHud?.BeginFadeOut(false);
#endif
                    levelExiting = LevelExitingState.Ready;
                });
                PlaySound("WarpIn");

                CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                speedX = 0;
                speedY = 0;
                externalForceX = 0f;
                externalForceY = 0f;
                internalForceY = 0f;
            } else {
                levelExiting = LevelExitingState.Waiting;

                if (suspendType != SuspendType.None) {
                    MoveInstantly(new Vector2(0f, 10f), MoveType.Relative, true);
                    suspendType = SuspendType.None;
                }

                CollisionFlags |= CollisionFlags.ApplyGravitation;
            }

            controllable = false;
            IsFacingLeft = false;
            isInvulnerable = true;
            copterFramesLeft = 0f;
            pushFramesLeft = 0f;

            // Used for waiting timeout
            lastPoleTime = 300f;

            return false;
        }

        public override void OnUpdate()
        {
            //base.OnUpdate();

#if !SERVER
            // Process KeyHit events in OnUpdate() instead of OnFixedUpdate()
#if DEBUG
            // ToDo: Debug keys only
            if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.T)) {
                WarpToPosition(new Vector2(Transform.Pos.X, Transform.Pos.Y - (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C) ? 500f : 150f)), false);
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.G)) {
                WarpToPosition(new Vector2(Transform.Pos.X, Transform.Pos.Y + (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C) ? 500f : 150f)), false);
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.F)) {
                WarpToPosition(new Vector2(Transform.Pos.X - (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C) ? 500f : 150f), Transform.Pos.Y), false);
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.H)) {
                WarpToPosition(new Vector2(Transform.Pos.X + (DualityApp.Keyboard.KeyPressed(Duality.Input.Key.C) ? 500f : 150f), Transform.Pos.Y), false);
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.N)) {
                levelHandler.InitLevelChange(ExitType.Normal, null);
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.J)) {
                //coins += 5;
                controllable = true;
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.U)) {
                attachedHud?.ShowLevelText("\f[s:75]\f[w:95]\f[c:1]\n\n\nCheat activated: \f[c:6]Add Ammo", false);

                for (int i = 0; i < weaponAmmo.Length; i++) {
                    AddAmmo((WeaponType)i, short.MaxValue);
                }
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.I)) {
                attachedHud?.ShowLevelText("\f[s:75]\f[w:95]\f[c:1]\n\n\nCheat activated: \f[c:6]Add Sugar Rush", false);

                BeginSugarRush();
            } else if (DualityApp.Keyboard.KeyHit(Duality.Input.Key.O)) {
                attachedHud?.ShowLevelText("\f[s:75]\f[w:95]\f[c:1]\n\n\nCheat activated: \f[c:6]Add all Power-ups", false);

                for (int i = 0; i < weaponAmmo.Length; i++) {
                    AddWeaponUpgrade((WeaponType)i, 0x1);
                }
            }
#endif
            if (!controllable || !controllableExternal) {
                return;
            }

            if (playerType != PlayerType.Frog) {
                if (ControlScheme.PlayerActionHit(playerIndex, PlayerActions.SwitchWeapon)) {
                    SwitchToNextWeapon();
                } else if (playerIndex == 0) {
                    // Use numeric key to switch weapons for the first player
                    int maxWeaponCount = Math.Min(weaponAmmo.Length, 9);
                    for (int i = 0; i < maxWeaponCount; i++) {
                        if (weaponAmmo[i] != 0 && DualityApp.Keyboard.KeyHit(Duality.Input.Key.Number1 + i)) {
                            SwitchToWeaponByIndex(i);
                        }
                    }
                }
            }
#endif
        }

        public override void OnFixedUpdate(float timeMult)
        {
#if !SERVER
            Hud.ShowDebugText("- Pos.: {" + (int)Transform.Pos.X + "; " + (int)Transform.Pos.Y + "}");
            Hud.ShowDebugText("  Speed: {" + speedX.ToString("F1") + "; " + speedY.ToString("F1") + "}");
            Hud.ShowDebugText("  Force: {" + externalForceX.ToString("F1") + "; " + externalForceY.ToString("F1") + "} " + internalForceY + " | " + ((CollisionFlags & CollisionFlags.ApplyGravitation) != 0 ? " G" : "") + (controllable ? " C" : "") + (inWater ? " W" : "") + (canJump ? " J" : ""));
            Hud.ShowDebugText("  A.: " + currentAnimationState + " | T.: " + currentTransitionState + " | S.: " + shieldTime);
#endif

            // Process level bounds
            Vector3 lastPos = Transform.Pos;
            Rect levelBounds = levelHandler.LevelBounds;
            if (lastPos.X < levelBounds.X) {
                lastPos.X = levelBounds.X;
                Transform.Pos = lastPos;
            } else if (lastPos.X > levelBounds.X + levelBounds.W) {
                lastPos.X = levelBounds.X + levelBounds.W;
                Transform.Pos = lastPos;
            }

            PushSolidObjects(timeMult);

            //base.OnFixedUpdate(timeMult);
            {
                if (timeMult > 1.25f) {
                    TryStandardMovement(1f);
                    TryStandardMovement(timeMult - 1f);
                } else {
                    TryStandardMovement(timeMult);
                }

                OnUpdateHitbox();

                if (renderer != null && renderer.AnimPaused) {
                    if (frozenTimeLeft <= 0f) {
                        renderer.AnimPaused = false;
                    } else {
                        frozenTimeLeft -= timeMult;
                    }
                }
            }

            FollowCarryingPlatform();
            UpdateAnimation(timeMult);

            CheckSuspendedStatus();
            CheckDestructibleTiles(timeMult);
            CheckEndOfSpecialMoves(timeMult);

            OnHandleWater();

            OnHandleAreaEvents(timeMult, out bool areaWeaponAllowed, out int areaWaterBlock);

            // Invulnerability
            if (invulnerableTime > 0f) {
                invulnerableTime -= timeMult;

                if (invulnerableTime <= 0f) {
                    isInvulnerable = false;

                    renderer.AnimHidden = false;

#if !SERVER
                    if (currentCircleEffectRenderer != null) {
                        SetCircleEffect(false);
                    }
#endif

#if MULTIPLAYER && SERVER
                    ((LevelHandler)levelHandler).OnPlayerSetInvulnerability(this, 0f, false);
#endif
                }
#if !SERVER
                else if (currentTransitionState != AnimState.Hurt && currentCircleEffectRenderer == null) {
                    if (invulnerableBlinkTime > 0f) {
                        invulnerableBlinkTime -= timeMult;
                    } else {
                        renderer.AnimHidden ^= true;

                        invulnerableBlinkTime = 3f;
                    }
                }
#endif
                else {
                    renderer.AnimHidden = false;
                }
            }

            // Timers
            if (controllableTimeout > 0f) {
                controllableTimeout -= timeMult;

                if (controllableTimeout <= 0f) {
                    controllable = true;

                    if (isAttachedToPole) {
                        // Something went wrong, detach and try to continue
                        // To prevent stucking
                        for (int i = -1; i > -6; i--) {
                            if (MoveInstantly(new Vector2(speedX, i), MoveType.Relative)) {
                                break;
                            }
                        }

                        CollisionFlags |= CollisionFlags.ApplyGravitation;
                        isAttachedToPole = false;
                        wasActivelyPushing = false;

                        controllableTimeout = 4f;
                        lastPoleTime = 10f;
                    }
                } else {
                    controllable = false;
                }
            }

            if (weaponCooldown > 0f) {
                weaponCooldown -= timeMult;
            }

            if (bonusWarpTimer > 0f) {
                bonusWarpTimer -= timeMult;
            }

            if (lastPoleTime > 0f) {
                lastPoleTime -= timeMult;
            }

            if (gemsTimer > 0f) {
                gemsTimer -= timeMult;

                if (gemsTimer <= 0f) {
                    gemsPitch = 0;
                }
            }

            if (waterCooldownLeft > 0f) {
                waterCooldownLeft -= timeMult;
            }

            // Weapons
            if (fireFramesLeft > 0f) {
                fireFramesLeft -= timeMult;

                if (fireFramesLeft <= 0f) {
                    // Play post-fire animation
                    if ((currentAnimationState & (AnimState.Walk | AnimState.Run | AnimState.Dash | AnimState.Crouch | AnimState.Buttstomp | AnimState.Swim | AnimState.Airboard | AnimState.Lift | AnimState.Spring)) == 0 &&
                        currentTransitionState != AnimState.TransitionRunToIdle &&
                        currentTransitionState != AnimState.TransitionDashToIdle &&
                        !isAttachedToPole) {

                        if ((currentAnimationState & AnimState.Hook) == AnimState.Hook) {
                            SetTransition(AnimState.TransitionHookShootToHook, false);
                        } else if ((currentAnimationState & AnimState.Copter) != 0) {
                            SetAnimation(AnimState.Copter);
                            SetTransition(AnimState.TransitionCopterShootToCopter, false);
                        } else if ((currentAnimationState & AnimState.Fall) != 0) {
                            SetTransition(AnimState.TransitionFallShootToFall, false);
                        } else {
                            SetTransition(AnimState.TransitionShootToIdle, false);
                        }
                    }
                }
            }

            // Shield
            if (shieldTime > 0f) {
                shieldTime -= timeMult;

                if (shieldTime <= 0f) {
                    SetShield(ShieldType.None, 0f);
                }
            }

            // Dizziness
            if (dizzyTime > 0f) {
                dizzyTime -= timeMult;
            }

            // Sugar Rush
            if (sugarRushLeft > 0f) {
                sugarRushLeft -= timeMult;

                if (sugarRushLeft > 0f) {
                    if (sugarRushStarsTime > 0f) {
                        sugarRushStarsTime -= timeMult;
                    } else {
                        sugarRushStarsTime = MathF.Rnd.NextFloat(2f, 8f);

                        TileMap tilemap = levelHandler.TileMap;
                        if (tilemap != null) {
                            if (availableAnimations.TryGetValue("SugarRush", out GraphicResource res)) {
                                Vector3 pos = Transform.Pos;
                                pos.Z -= 30f;

                                Material material = res.Material.Res;
                                Texture texture = material.MainTexture.Res;

                                float speedX = MathF.Rnd.NextFloat(-1f, 1f) * MathF.Rnd.NextFloat(0.4f, 4f);
                                tilemap.CreateDebris(new TileMap.DestructibleDebris {
                                    Pos = pos,
                                    Size = res.Base.FrameDimensions,
                                    Speed = new Vector2(speedX, -1f * MathF.Rnd.NextFloat(2.2f, 4f)),
                                    Acceleration = new Vector2(0f, 0.2f),

                                    Scale = MathF.Rnd.NextFloat(0.1f, 0.5f),
                                    ScaleSpeed = -0.002f,
                                    Angle = MathF.Rnd.NextFloat() * MathF.TwoPi,
                                    AngleSpeed = speedX * 0.04f,
                                    Alpha = 1f,
                                    AlphaSpeed = -0.018f,

                                    Time = 160f,

                                    Material = material,
                                    MaterialOffset = texture.LookupAtlas(res.FrameOffset + MathF.Rnd.Next(res.FrameCount))
                                });
                            }
                        }
                    }
                } else {
                    OnAnimationStarted();
                }
            }

            // Copter
            if (activeModifier != Modifier.None) {
                if (activeModifier == Modifier.Copter || activeModifier == Modifier.LizardCopter) {
                    copterFramesLeft -= timeMult;
                    if (copterFramesLeft <= 0) {
                        SetModifier(Modifier.None);
                    }
                }
            }

            if (copterSound != null && (currentAnimationState & AnimState.Copter) == 0) {
                copterSound.Stop();
                copterSound = null;
            }

            // Shallow Water
            if (areaWaterBlock != -1) {
                if (inShallowWater == -1) {
                    Vector3 pos = Transform.Pos;
                    pos.Y = areaWaterBlock;
                    pos.Z -= 2f;

                    Explosion.Create(levelHandler, pos, Explosion.WaterSplash);
                    levelHandler.PlayCommonSound("WaterSplash", this, 0.7f, 0.5f);
                }

                inShallowWater = areaWaterBlock;
            } else if (inShallowWater != -1) {
                Vector3 pos = Transform.Pos;
                pos.Y = inShallowWater;
                pos.Z -= 2f;

                Explosion.Create(levelHandler, pos, Explosion.WaterSplash);
                levelHandler.PlayCommonSound("WaterSplash", this, 1f, 0.5f);

                inShallowWater = -1;
            }

            // Tube
            if (inTubeTime > 0f) {
                inTubeTime -= timeMult;

                if (inTubeTime <= 0f) {
                    controllable = true;
                    CollisionFlags |= (CollisionFlags.ApplyGravitation | CollisionFlags.CollideWithTileset);
                } else {
                    // Skip controls, player is not controllable in tube
                    // Weapons are automatically disabled if player is not controllable
                    if (weaponToasterSound != null) {
                        weaponToasterSound.Stop();
                        weaponToasterSound = null;
                    }

                    return;
                }
            }

            // Controls
            // Move
            if (keepRunningTime <= 0f) {
#if !SERVER
                bool canWalk = (controllable && controllableExternal && !isLifting && suspendType != SuspendType.SwingingVine &&
                    (playerType != PlayerType.Frog || !ControlScheme.PlayerActionPressed(playerIndex, PlayerActions.Fire)));

                float playerMovement = ControlScheme.PlayerHorizontalMovement(playerIndex);
                float playerMovementVelocity = MathF.Abs(playerMovement);
                if (canWalk && playerMovementVelocity > 0.5f) {
                    SetAnimation(currentAnimationState & ~(AnimState.Lookup | AnimState.Crouch));

                    if (dizzyTime > 0f) {
                        IsFacingLeft = (playerMovement > 0f);
                    } else {
                        IsFacingLeft = (playerMovement < 0f);
                    }

                    isActivelyPushing = wasActivelyPushing = true;

                    if (dizzyTime > 0f || playerType == PlayerType.Frog) {
                        speedX = MathF.Clamp(speedX + Acceleration * timeMult * (IsFacingLeft ? -1 : 1), -MaxDizzySpeed * playerMovementVelocity, MaxDizzySpeed * playerMovementVelocity);
                    } else if (inShallowWater != -1 && levelHandler.ReduxMode && playerType != PlayerType.Lori) {
                        // Use lower speed in shallow water if Redux Mode is enabled
                        // Also, exclude Lori, because she can't ledge climb or double jump (rescue/01_colon1)
                        speedX = MathF.Clamp(speedX + Acceleration * timeMult * (IsFacingLeft ? -1 : 1), -MaxShallowWaterSpeed * playerMovementVelocity, MaxShallowWaterSpeed * playerMovementVelocity);
                    } else {
                        if (suspendType == SuspendType.None && !inWater && ControlScheme.PlayerActionPressed(playerIndex, PlayerActions.Run)) {
                            speedX = MathF.Clamp(speedX + Acceleration * timeMult * (IsFacingLeft ? -1 : 1), -MaxDashingSpeed * playerMovementVelocity, MaxDashingSpeed * playerMovementVelocity);
                        } else if (suspendType == SuspendType.Vine) {
                            if (wasFirePressed) {
                                speedX = 0f;
                            } else {
                                speedX = MathF.Clamp(speedX + Acceleration * timeMult * (IsFacingLeft ? -1 : 1), -MaxVineSpeed * playerMovementVelocity, MaxVineSpeed * playerMovementVelocity);
                            }
                        } else if (suspendType != SuspendType.Hook) {
                            speedX = MathF.Clamp(speedX + Acceleration * timeMult * (IsFacingLeft ? -1 : 1), -MaxRunningSpeed * playerMovementVelocity, MaxRunningSpeed * playerMovementVelocity);
                        }
                    }

                    if (canJump) {
                        wasUpPressed = wasDownPressed = false;
                    }
                }
                else
#endif
                {
                    speedX = MathF.Max((MathF.Abs(speedX) - Deceleration * timeMult), 0) * (speedX < 0 ? -1 : 1);
                    isActivelyPushing = false;

                    float absSpeedX = MathF.Abs(speedX);
                    if (absSpeedX > 4f) {
                        IsFacingLeft = (speedX < 0f);
                    } else if (absSpeedX < 0.001f) {
                        wasActivelyPushing = false;
                    }
                }
            } else {
                keepRunningTime -= timeMult;

                isActivelyPushing = wasActivelyPushing = true;

                float absSpeedX = MathF.Abs(speedX);
                if (absSpeedX > 1f) {
                    IsFacingLeft = (speedX < 0f);
                } else if (absSpeedX < 1f) {
                    keepRunningTime = 0f;
                }
            }

#if !SERVER
            if (!controllable || !controllableExternal) {
                // Weapons are automatically disabled if player is not controllable
                if (weaponToasterSound != null) {
                    weaponToasterSound.Stop();
                    weaponToasterSound = null;
                }

                return;
            }

            if (inWater || activeModifier != Modifier.None) {
                float playerMovement = ControlScheme.PlayerVerticalMovement(playerIndex);
                float playerMovementVelocity = MathF.Abs(playerMovement);
                if (playerMovementVelocity > 0.5f) {
                    float mult;
                    switch (activeModifier) {
                        case Modifier.Airboard: mult = (playerMovement > 0 ? -1f : 0.2f); break;
                        case Modifier.LizardCopter: mult = (playerMovement > 0 ? -2f : 2f); break;
                        default: mult = (playerMovement > 0 ? -1f : 1f); break;
                    }

                    speedY = MathF.Clamp(speedY - Acceleration * timeMult * mult, -MaxRunningSpeed * playerMovementVelocity, MaxRunningSpeed * playerMovementVelocity);
                } else {
                    speedY = MathF.Max((MathF.Abs(speedY) - Deceleration * timeMult), 0) * (speedY < 0 ? -1 : 1);
                }
            } else {
                // Look-up
                if (ControlScheme.PlayerActionPressed(playerIndex, PlayerActions.Up)) {
                    if (!wasUpPressed && dizzyTime <= 0f) {
                        if ((canJump || (suspendType != SuspendType.None && suspendType != SuspendType.SwingingVine)) && !isLifting && Math.Abs(speedX) < float.Epsilon) {
                            wasUpPressed = true;

                            SetAnimation(AnimState.Lookup | (currentAnimationState & AnimState.Hook));
                        }
                    }
                } else if (wasUpPressed) {
                    wasUpPressed = false;

                    SetAnimation(currentAnimationState & ~AnimState.Lookup);
                }

                // Crouch
                if (ControlScheme.PlayerActionPressed(playerIndex, PlayerActions.Down)) {
                    if (suspendType == SuspendType.SwingingVine) {
                        // ToDo
                    } else if (suspendType != SuspendType.None) {
                        wasDownPressed = true;

                        MoveInstantly(new Vector2(0f, 10f), MoveType.Relative, true);
                        suspendType = SuspendType.None;

                        CollisionFlags |= CollisionFlags.ApplyGravitation;
                    } else if (!wasDownPressed && dizzyTime <= 0f) {
                        if (canJump) {
                            if (!isLifting && Math.Abs(speedX) < float.Epsilon) {
                                wasDownPressed = true;

                                SetAnimation(AnimState.Crouch);
                            }
                        } else if (playerType != PlayerType.Frog) {
                            wasDownPressed = true;

                            controllable = false;
                            speedX = 0f;
                            speedY = 0f;
                            internalForceY = 0f;
                            externalForceY = 0f;
                            CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                            currentSpecialMove = SpecialMoveType.Buttstomp;
                            SetAnimation(AnimState.Buttstomp);
                            SetPlayerTransition(AnimState.TransitionButtstompStart, true, false, SpecialMoveType.Buttstomp, delegate {
                                speedY = 9f;
                                CollisionFlags |= CollisionFlags.ApplyGravitation;
                                SetAnimation(AnimState.Buttstomp);
                                PlaySound("Buttstomp", 1f, 0.8f);
                                PlaySound("Buttstomp2");
                            });
                        }
                    }
                } else if (wasDownPressed) {
                    wasDownPressed = false;

                    SetAnimation(currentAnimationState & ~AnimState.Crouch);
                }

                // Jump
                if (ControlScheme.PlayerActionPressed(playerIndex, PlayerActions.Jump)) {
                    if (!wasJumpPressed) {
                        wasJumpPressed = true;

                        if (isLifting && canJump && currentSpecialMove == SpecialMoveType.None) {
                            canJump = false;
                            SetAnimation(currentAnimationState & (~AnimState.Lookup & ~AnimState.Crouch));
                            PlaySound("Jump");
                            carryingObject = null;

                            CollisionFlags &= ~CollisionFlags.IsSolidObject;

                            isLifting = false;
                            controllable = false;

                            speedY = -3f;
                            internalForceY = 0.86f;

                            CollisionFlags &= ~CollisionFlags.CollideWithSolidObjects;

                            SetTransition(AnimState.TransitionLiftEnd, false, delegate {
                                controllable = true;
                                CollisionFlags |= CollisionFlags.CollideWithSolidObjects;
                            });
                        } else {
                            switch (playerType) {
                                case PlayerType.Jazz: {
                                    if ((currentAnimationState & AnimState.Crouch) != 0) {
                                        controllable = false;
                                        SetAnimation(AnimState.Uppercut);
                                        SetPlayerTransition(AnimState.TransitionUppercutA, true, true, SpecialMoveType.Uppercut, delegate {
                                            externalForceY = 1.4f;
                                            speedY = -2f;
                                            canJump = false;
                                            SetPlayerTransition(AnimState.TransitionUppercutB, true, true, SpecialMoveType.Uppercut);
                                        });
                                    } else {
                                        if (speedY > 0.01f && !canJump && (currentAnimationState & (AnimState.Fall | AnimState.Copter)) != 0) {
                                            CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                                            speedY = 1.5f;
                                            if ((currentAnimationState & AnimState.Copter) == 0) {
                                                SetAnimation(AnimState.Copter);
                                            }
                                            copterFramesLeft = 70;

                                            if (copterSound == null) {
                                                copterSound = PlaySound("Copter", 0.6f, 1.5f);
                                                if (copterSound != null) {
                                                    copterSound.Flags |= SoundInstanceFlags.Looped;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                                case PlayerType.Spaz: {
                                    if ((currentAnimationState & AnimState.Crouch) != 0) {
                                        controllable = false;
                                        SetAnimation(AnimState.Uppercut);
                                        SetPlayerTransition(AnimState.TransitionUppercutA, true, true, SpecialMoveType.Sidekick, delegate {
                                            externalForceX = 8f * (IsFacingLeft ? -1f : 1f);
                                            speedX = 14.4f * (IsFacingLeft ? -1f : 1f);
                                            CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                                            SetPlayerTransition(AnimState.TransitionUppercutB, true, true, SpecialMoveType.Sidekick);
                                        });

                                        PlaySound("Sidekick");
                                    } else {
                                        if (!canJump && canDoubleJump) {
                                            canDoubleJump = false;
                                            isFreefall = false;

                                            internalForceY = 1.15f;
                                            speedY = -0.6f - MathF.Max(0f, (MathF.Abs(speedX) - 4f) * 0.3f);
                                            speedX *= 0.4f;

                                            PlaySound("DoubleJump");

                                            SetTransition(AnimState.Spring, false);
                                        }
                                    }
                                    break;
                                }
                                case PlayerType.Lori: {
                                    if ((currentAnimationState & AnimState.Crouch) != 0) {
                                        controllable = false;
                                        SetAnimation(AnimState.Uppercut);
                                        SetPlayerTransition(AnimState.TransitionUppercutA, true, true, SpecialMoveType.Sidekick, delegate {
                                            externalForceX = 15f * (IsFacingLeft ? -1f : 1f);
                                            speedX = 6f * (IsFacingLeft ? -1f : 1f);
                                            CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                                        });
                                    } else {
                                        if (speedY > 0.01f && !canJump && (currentAnimationState & (AnimState.Fall | AnimState.Copter)) != 0) {
                                            CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                                            speedY = 1.5f;
                                            if ((currentAnimationState & AnimState.Copter) == 0) {
                                                SetAnimation(AnimState.Copter);
                                            }
                                            copterFramesLeft = 70;

                                            if (copterSound == null) {
                                                copterSound = PlaySound("Copter", 0.6f, 1.5f);
                                                if (copterSound != null) {
                                                    copterSound.Flags |= SoundInstanceFlags.Looped;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            }

                        }
                    } else {
                        if (suspendType != SuspendType.None) {
                            if (suspendType == SuspendType.SwingingVine) {
                                suspendType = SuspendType.None;
                                currentVine = null;
                                CollisionFlags |= CollisionFlags.ApplyGravitation;
                            } else {
                                MoveInstantly(new Vector2(0f, -8f), MoveType.Relative, true);
                            }
                            canJump = true;
                        }
                        if (!canJump) {
                            if (copterFramesLeft > 0f) {
                                copterFramesLeft = 70f;
                            }
                        } else if (currentSpecialMove == SpecialMoveType.None && !ControlScheme.PlayerActionPressed(playerIndex, PlayerActions.Down)) {
                            canJump = false;
                            isFreefall = false;
                            SetAnimation(currentAnimationState & (~AnimState.Lookup & ~AnimState.Crouch));
                            PlaySound("Jump");
                            carryingObject = null;

                            // Gravitation is sometimes off because of active copter, turn it on again
                            CollisionFlags |= CollisionFlags.ApplyGravitation;

                            CollisionFlags &= ~CollisionFlags.IsSolidObject;

                            internalForceY = 1.02f;
                            speedY = -3.55f - MathF.Max(0f, (MathF.Abs(speedX) - 4f) * 0.3f);
                        }
                    }
                } else {
                    if (!wasJumpPressed) {
                        if (internalForceY > 0f) {
                            internalForceY = 0f;
                        }
                    } else {
                        wasJumpPressed = false;
                    }
                }
            }

            // Fire
            bool weaponInUse = false;
            if (weaponAllowed && areaWeaponAllowed && ControlScheme.PlayerActionPressed(playerIndex, PlayerActions.Fire)) {
                if (!isLifting && suspendType != SuspendType.SwingingVine && (currentAnimationState & AnimState.Push) == 0 && pushFramesLeft <= 0f) {
                    if (playerType == PlayerType.Frog) {
                        if (currentTransitionState == AnimState.Idle && MathF.Abs(speedX) < 0.1f && MathF.Abs(speedY) < 0.1f && MathF.Abs(externalForceX) < 0.1f && MathF.Abs(externalForceY) < 0.1f) {
                            PlaySound("Tongue", 0.8f);

                            controllable = false;
                            controllableTimeout = 120f;

                            SetTransition(currentAnimationState | AnimState.Shoot, false, delegate {
                                controllable = true;
                                controllableTimeout = 0f;
                            });
                        }
                    } else if (weaponAmmo[(int)currentWeapon] != 0) {
                        if (currentTransitionState == AnimState.Spring || currentTransitionState == AnimState.TransitionShootToIdle) {
                            ForceCancelTransition();
                        }

                        SetAnimation(currentAnimationState | AnimState.Shoot);

                        fireFramesLeft = 20f;

                        if (!wasFirePressed) {
                            wasFirePressed = true;
                            //SetTransition(currentAnimationState | AnimState.TRANSITION_IDLE_TO_SHOOT, false);
                        }

                        weaponInUse = FireWeapon(currentWeapon);
                    }
                }
            } else if (wasFirePressed) {
                wasFirePressed = false;

                weaponCooldown = 0f;
            }

            if (!weaponInUse) {
                if (weaponToasterSound != null) {
                    weaponToasterSound.Stop();
                    weaponToasterSound = null;
                }
            }
#endif
        }

        protected override void OnHitFloor()
        {
            Vector3 pos = Transform.Pos;
            if (levelHandler.EventMap.IsHurting(pos.X, pos.Y + 24)) {
                TakeDamage(1, speedX * 0.25f);
            } else if (!inWater && activeModifier == Modifier.None) {
                if (!canJump) {
                    PlaySound("Land", 0.8f);

                    if (MathF.Rnd.NextFloat() < 0.6f) {
                        Explosion.Create(levelHandler, pos + new Vector3(0f, 20f, 0f), Explosion.TinyDark);
                    }
                }
            } else {
                // Prevent stucking with water/airboard
                canJump = false;
                if (speedY > 0f) {
                    speedY = 0f;
                }
            }

            canDoubleJump = true;
            isFreefall = false;

            CollisionFlags |= CollisionFlags.IsSolidObject;
        }

        protected override void OnHitCeiling()
        {
            Vector3 pos = Transform.Pos;
            if (levelHandler.EventMap.IsHurting(pos.X, pos.Y - 4f)) {
                TakeDamage(1, speedX * 0.25f);
            }
        }

        protected override void OnHitWall()
        {
            // Reset speed and show Push animation
            speedX = 0f;
            pushFramesLeft = 2f;
            keepRunningTime = 0f;

            Vector3 pos = Transform.Pos;
            if (levelHandler.EventMap.IsHurting(pos.X + (speedX > 0f ? 1f : -1f) * 16f, pos.Y)) {
                TakeDamage(1, speedX * 0.25f);
            } else {

                if (SettingsCache.EnableLedgeClimb && isActivelyPushing && suspendType == SuspendType.None && activeModifier == Modifier.None && !canJump &&
                    currentSpecialMove == SpecialMoveType.None && currentTransitionState != AnimState.TransitionUppercutEnd &&
                    speedY >= -1f && externalForceY <= 0f && copterFramesLeft <= 0f && keepRunningTime <= 0f) {

                    // Character supports ledge climbing
                    if (FindAnimationCandidates(AnimState.TransitionLedgeClimb).Count > 0) {
                        const int MaxTolerancePixels = 6;

                        float x = (IsFacingLeft ? -8f : 8f);
                        AABB hitbox1 = AABBInner + new Vector2(x, -42f - MaxTolerancePixels);   // Empty space to climb to
                        AABB hitbox2 = AABBInner + new Vector2(x, -42f + 2f);                   // Wall below the empty space
                        AABB hitbox3 = AABBInner + new Vector2(x, -42f + 2f + 24f);             // Wall between the player and the wall above (vertically)
                        AABB hitbox4 = AABBInner + new Vector2(x,  20f);                        // Wall below the player
                        AABB hitbox5 = new AABB(AABBInner.LowerBound.X + 2, hitbox1.LowerBound.Y, AABBInner.UpperBound.X - 2, AABBInner.UpperBound.Y); // Player can't climb through walls
                        if ( levelHandler.IsPositionEmpty(this, ref hitbox1, false) &&
                            !levelHandler.IsPositionEmpty(this, ref hitbox2, false) &&
                            !levelHandler.IsPositionEmpty(this, ref hitbox3, false) &&
                            !levelHandler.IsPositionEmpty(this, ref hitbox4, false) &&
                             levelHandler.IsPositionEmpty(this, ref hitbox5, false)) {

                            ushort[] wallParams = null;
                            if (levelHandler.EventMap.GetEventByPosition(IsFacingLeft ? hitbox2.LowerBound.X : hitbox2.UpperBound.X, hitbox2.UpperBound.Y, ref wallParams) != EventType.ModifierNoClimb) {
                                // Move the player upwards, if it is in tolerance, so the animation will look better
                                for (int y = 0; y >= -MaxTolerancePixels; y -= 2) {
                                    AABB aabb = AABBInner + new Vector2(x, -42f + y);
                                    if (levelHandler.IsPositionEmpty(this, ref aabb, false)) {
                                        MoveInstantly(new Vector2(0f, y), MoveType.Relative, true);
                                        break;
                                    }
                                }

                                // Prepare the player for animation
                                controllable = false;
                                CollisionFlags &= ~(CollisionFlags.ApplyGravitation | CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithSolidObjects);

                                speedX = externalForceX = externalForceY = 0f;
                                speedY = -1.36f;
                                pushFramesLeft = fireFramesLeft = copterFramesLeft = 0f;

                                // Stick the player to wall
                                MoveInstantly(new Vector2(IsFacingLeft ? -6f : 6f, 0f), MoveType.Relative, true);

                                SetAnimation(AnimState.Idle);
                                SetTransition(AnimState.TransitionLedgeClimb, false, delegate {
                                    // Reset the player to normal state
                                    canJump = true;
                                    controllable = true;
                                    CollisionFlags |= CollisionFlags.ApplyGravitation | CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithSolidObjects;
                                    pushFramesLeft = 0f;
                                    fireFramesLeft = 0f;
                                    copterFramesLeft = 0f;

                                    speedY = 0f;

                                    // Move it far from the ledge
                                    MoveInstantly(new Vector2(IsFacingLeft ? -4f : 4f, 0f), MoveType.Relative);

                                    // Move the player upwards, so it will not be stuck in the wall
                                    for (int y = -2; y > -24; y -= 2) {
                                        if (MoveInstantly(new Vector2(0f, y), MoveType.Relative)) {
                                            break;
                                        }
                                    }
                                });
                            }
                        }
                    }
                }
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

            AABBInner = new AABB(pos.X - 11f, pos.Y + 8f - 12f, pos.X + 11f, pos.Y + 8f + 12f);
        }

        public override bool OnTileDeactivate(int tx1, int ty1, int tx2, int ty2)
        {
            // Player can never be deactivated
            return false;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            if (currentTransitionState == AnimState.TransitionDeath) {
                return false;
            }

            isInvulnerable = true;

            ForceCancelTransition();

            if (playerType == PlayerType.Frog) {
                playerType = playerTypeOriginal;

                // Load original metadata
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
                    case PlayerType.Frog:
                        RequestMetadata("Interactive/PlayerFrog");
                        break;
                }

                // Refresh animation state
                currentSpecialMove = SpecialMoveType.None;
                currentAnimation = null;
                SetAnimation(currentAnimationState);

                // Morph to original type with animation and then trigger death
                SetPlayerTransition(AnimState.TransitionFromFrog, false, true, SpecialMoveType.None, delegate {
                    OnPerishInner();
                });
            } else {
                OnPerishInner();
            }

            return false;
        }

        private void OnPerishInner()
        {
            SetPlayerTransition(AnimState.TransitionDeath, false, true, SpecialMoveType.None, delegate {
                if (lives > 1 || levelHandler.Difficulty == GameDifficulty.Multiplayer) {
                    if (lives > 1) {
                        lives--;
                    }

                    // Remove fast fires
                    weaponUpgrades[(int)WeaponType.Blaster] = (byte)(weaponUpgrades[(int)WeaponType.Blaster] & 0x1);

                    canJump = false;
                    speedX = 0;
                    speedY = 0;
                    externalForceX = 0f;
                    externalForceY = 0f;
                    internalForceY = 0f;
                    fireFramesLeft = 0f;
                    copterFramesLeft = 0f;
                    pushFramesLeft = 0f;
                    weaponCooldown = 0f;
                    controllable = true;
                    inShallowWater = -1;
                    SetModifier(Modifier.None);

#if MULTIPLAYER && !SERVER
                    if (!(levelHandler is MultiplayerLevelHandler)) {
#endif
                    // Spawn corpse
                    PlayerCorpse corpse = new PlayerCorpse();
                    corpse.OnActivated(new ActorActivationDetails {
                        LevelHandler = levelHandler,
                        Pos = Transform.Pos,
                        Params = new[] { (ushort)playerType, (ushort)(IsFacingLeft ? 1 : 0) }
                    });
                    levelHandler.AddActor(corpse);
#if MULTIPLAYER && !SERVER
                    }
#endif

                    SetAnimation(AnimState.Idle);

                    if (levelHandler.HandlePlayerDied(this)) {
                        // Reset health
                        health = maxHealth;

                        // Player can be respawned immediately
                        isInvulnerable = false;
                        CollisionFlags |= CollisionFlags.ApplyGravitation;

                        // Return to the last save point
                        MoveInstantly(checkpointPos, MoveType.Absolute, true);
                        levelHandler.AmbientLightCurrent = checkpointLight;
                        levelHandler.LimitCameraView(0, 0);
                        levelHandler.WarpCameraToTarget(this);

                        if (levelHandler.Difficulty != GameDifficulty.Multiplayer) {
                            levelHandler.EventMap.RollbackToCheckpoint();
                        }

                    } else {
                        // Respawn is delayed
                        controllable = false;
                        renderer.AnimHidden = true;

                        // ToDo: Turn off collisions
                    }
                } else {
                    controllable = false;
                    renderer.AnimHidden = true;

                    levelHandler.HandleGameOver();
                }
            });

            PlaySound("Die", 1.3f);
        }

#if !SERVER
        protected override void OnAnimationStarted()
        {
            if (sugarRushLeft <= 0f) {
                // Reset renderer
                renderer.CustomMaterial = null;
            } else {
                // Refresh temporary material
                renderer.CustomMaterial = new BatchInfo(renderer.SharedMaterial.Res.Info) {
                    Technique = ContentResolver.Current.RequestShader("Colorize"),
                    MainColor = new ColorRgba(0.7f, 0.5f)
                };
            }
        }
#endif

        private void UpdateAnimation(float timeMult)
        {
            if (!controllable) {
                return;
            }

            float posX = Transform.Pos.X;

            AnimState oldState = currentAnimationState;
            AnimState newState;
            if (inWater) {
                newState = AnimState.Swim;
            } else if (activeModifier == Modifier.Airboard) {
                newState = AnimState.Airboard;
            } else if (activeModifier == Modifier.Copter) {
                newState = AnimState.Copter;
            } else if (activeModifier == Modifier.LizardCopter) {
                newState = AnimState.Hook;
            } else if (suspendType == SuspendType.SwingingVine) {
                newState = AnimState.Swing;
            } else if (isLifting) {
                newState = AnimState.Lift;
            } else if (canJump && isActivelyPushing && pushFramesLeft > 0f && keepRunningTime <= 0f) {
                newState = AnimState.Push;
            } else {
                // Only certain ones don't need to be preserved from earlier state, others should be set as expected
                AnimState composite = unchecked(currentAnimationState & (AnimState)0xFFF83F60);

                if (isActivelyPushing == wasActivelyPushing) {
                    float absSpeedX = MathF.Abs(speedX);
                    if (absSpeedX > MaxRunningSpeed) {
                        composite |= AnimState.Dash;
                    } else if (keepRunningTime > 0f) {
                        composite |= AnimState.Run;
                    } else if (absSpeedX > 0f) {
                        composite |= AnimState.Walk;
                    }

                    if (inIdleTransition) {
                        CancelTransition();
                    }
                }

                if (fireFramesLeft > 0f) {
                    composite |= AnimState.Shoot;
                }

                if (suspendType != SuspendType.None) {
                    composite |= AnimState.Hook;
                } else {
                    if (canJump) {
                        // Grounded, no vertical speed
                        if (dizzyTime > 0f) {
                            composite |= AnimState.Dizzy;
                        }
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

            if (newState == AnimState.Idle) {
                if (idleTime > 600f) {
                    idleTime = 0f;

                    if (currentTransitionState == AnimState.Idle) {
                        SetPlayerTransition(AnimState.TransitionIdleBored, true, false, SpecialMoveType.None);
                    }
                } else {
                    idleTime += timeMult;
                }
            } else {
                idleTime = 0f;
            }

            SetAnimation(newState);

            switch (oldState) {
                case AnimState.Walk:
                    if (newState == AnimState.Idle) {
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
                    } else if (!inLedgeTransition) {
                        AABB aabbL = new AABB(AABBInner.LowerBound.X + 2, AABBInner.UpperBound.Y - 10, AABBInner.LowerBound.X + 4, AABBInner.UpperBound.Y + 28);
                        AABB aabbR = new AABB(AABBInner.UpperBound.X - 4, AABBInner.UpperBound.Y - 10, AABBInner.UpperBound.X - 2, AABBInner.UpperBound.Y + 28);
                        if (IsFacingLeft
                            ? ( levelHandler.IsPositionEmpty(this, ref aabbL, true) && !levelHandler.IsPositionEmpty(this, ref aabbR, true))
                            : (!levelHandler.IsPositionEmpty(this, ref aabbL, true) &&  levelHandler.IsPositionEmpty(this, ref aabbR, true))) {

                            inLedgeTransition = true;
                            // ToDo: Spaz's and Lori's animation should be continual
                            SetTransition(AnimState.TransitionLedge, true);

                            PlaySound("Ledge");
                        }
                    } else if (newState != AnimState.Idle) {
                        inLedgeTransition = false;

                        if (currentTransitionState == AnimState.TransitionLedge) {
                            CancelTransition();
                        }
                    }
                    break;
            }
        }

        private void PushSolidObjects(float timeMult)
        {
            if (pushFramesLeft > 0f) {
                pushFramesLeft -= timeMult;
            }

            if (canJump && controllable && controllableExternal && isActivelyPushing && MathF.Abs(speedX) > float.Epsilon) {
                AABB hitbox = AABBInner + new Vector2(speedX < 0 ? -2f : 2f, 0f);
                if (!levelHandler.IsPositionEmpty(this, ref hitbox, false, out ActorBase collider)) {
                    SolidObjectBase solidObject = collider as SolidObjectBase;
                    if (solidObject != null) {
                        CollisionFlags &= ~CollisionFlags.IsSolidObject;
                        if (solidObject.Push(speedX < 0, timeMult)) {
                            pushFramesLeft = 3f;
                        }
                        CollisionFlags |= CollisionFlags.IsSolidObject;
                    }
                }
            } else if ((CollisionFlags & CollisionFlags.IsSolidObject) != 0) {
                AABB aabb = AABBInner + new Vector2(0f, -2f);
                if (!levelHandler.IsPositionEmpty(this, ref aabb, false, out ActorBase collider)) {
                    SolidObjectBase solidObject = collider as SolidObjectBase;
                    if (solidObject != null) {

                        if (AABBInner.LowerBound.Y >= solidObject.AABBInner.LowerBound.Y && !isLifting) {
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
                if (suspendType == SuspendType.None && !isSpring) {
                    // ToDo: Refactor this
                    Vector3 pos = Transform.Pos;
                    int tx = (int)pos.X / 32;
                    int ty = ((int)pos.Y + 24) / 32;

                    ushort[] eventParams = null;
                    if (levelHandler.EventMap.GetEventByPosition(tx, ty, ref eventParams) == EventType.GemStomp) {
                        levelHandler.EventMap.StoreTileEvent(tx, ty, EventType.Empty);

                        for (int i = 0; i < 8; i++) {
                            float fx = MathF.Rnd.NextFloat(-18f, 18f);
                            float fy = MathF.Rnd.NextFloat(-8f, 0.2f);

                            ActorBase actor = levelHandler.EventSpawner.SpawnEvent(EventType.Gem, new ushort[] { 0 }, ActorInstantiationFlags.None, pos + new Vector3(fx * 2f, fy * 4f, 10f));
                            actor.AddExternalForce(fx, fy);
                            levelHandler.AddActor(actor);
                        }
                    }

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
            }

            // Sidekick
            if (currentSpecialMove == SpecialMoveType.Sidekick && currentTransitionState == AnimState.Idle /*&& ((currentAnimationState & AnimState.UPPERCUT) != 0)*/ && MathF.Abs(speedX) < 0.01f) {
                EndDamagingMove();
                controllable = true;
                if (suspendType == SuspendType.None) {
                    SetTransition(AnimState.TransitionUppercutEnd, false);
                }
            }

            // Copter Ears
            if (activeModifier != Modifier.Copter && activeModifier != Modifier.LizardCopter) {
                // ToDo: Is this still needed?
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
                        CollisionFlags |= CollisionFlags.ApplyGravitation;
                    }
                }
            }
        }

        private void CheckDestructibleTiles(float timeMult)
        {
            TileMap tiles = levelHandler.TileMap;
            if (tiles == null) {
                return;
            }

            AABB aabb = AABBInner + new Vector2((speedX + externalForceX) * 2f * timeMult, (speedY - externalForceY) * 2f * timeMult);

            // Buttstomp/etc. tiles checking
            if (currentSpecialMove != SpecialMoveType.None || sugarRushLeft > 0f) {
                int destroyedCount = tiles.CheckSpecialDestructible(ref aabb);
                AddScore((uint)(destroyedCount * 50));

                if (!(levelHandler.IsPositionEmpty(this, ref aabb, false, out ActorBase solidObject)) && solidObject != null) {
                    solidObject.OnHandleCollision(this);
                }
            }

            // Speed tiles checking
            if (MathF.Abs(speedX) > float.Epsilon || MathF.Abs(speedY) > float.Epsilon || sugarRushLeft > 0f) {
                int destroyedCount = tiles.CheckSpecialSpeedDestructible(ref aabb,
                    sugarRushLeft > 0f ? 64f : MathF.Max(MathF.Abs(speedX), MathF.Abs(speedY)));

                AddScore((uint)(destroyedCount * 50));
            }

            tiles.CheckCollapseDestructible(ref aabb);
        }

        private void CheckSuspendedStatus()
        {
            if (suspendType == SuspendType.SwingingVine) {
                return;
            }

            TileMap tiles = levelHandler.TileMap;
            if (tiles == null) {
                return;
            }

            Vector3 pos = Transform.Pos;

            AnimState currentState = currentAnimationState;

            SuspendType newSuspendState = tiles.GetTileSuspendState(pos.X, pos.Y - 1f);

            if (newSuspendState == suspendType) {
                if (newSuspendState == SuspendType.None) {
                    const float tolerance = 6f;

                    newSuspendState = tiles.GetTileSuspendState(pos.X - tolerance, pos.Y - 1f);
                    if (newSuspendState != SuspendType.Hook) {
                        newSuspendState = tiles.GetTileSuspendState(pos.X + tolerance, pos.Y - 1f);
                        if (newSuspendState != SuspendType.Hook) {
                            return;
                        } else {
                            MoveInstantly(new Vector2(tolerance, 0f), MoveType.Relative, true);
                        }
                    } else {
                        MoveInstantly(new Vector2(-tolerance, 0f), MoveType.Relative, true);
                    }
                } else {
                    return;
                }
            }

            if (newSuspendState != SuspendType.None && playerType != PlayerType.Frog) {
                if (currentSpecialMove == SpecialMoveType.None) {

                    suspendType = newSuspendState;
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;

                    if (speedY > 0 && newSuspendState == SuspendType.Vine) {
                        PlaySound("HookAttach", 0.8f, 1.2f);
                    }

                    speedY = 0f;
                    externalForceY = 0f;
                    isFreefall = false;
                    isSpring = false;
                    copterFramesLeft = 0f;

                    if (newSuspendState == SuspendType.Hook || wasFirePressed) {
                        speedX = externalForceX = 0f;
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
                    CollisionFlags |= CollisionFlags.ApplyGravitation;
                }
            }
        }

        private void OnHandleWater()
        {
            if (inWater) {
                if (Transform.Pos.Y >= levelHandler.WaterLevel) {
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;

                    if (MathF.Abs(speedX) > 1f || MathF.Abs(speedY) > 1f) {
                        float angle;
                        if (speedX == 0f) {
                            if (IsFacingLeft) {
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

                } else if (waterCooldownLeft <= 0f) {
                    inWater = false;
                    waterCooldownLeft = 20f;

                    CollisionFlags |= CollisionFlags.ApplyGravitation;
                    canJump = true;
                    externalForceY = 0.45f;
                    Transform.Angle = 0;

                    SetAnimation(AnimState.Jump);

                    var pos = Transform.Pos;
                    pos.Y = levelHandler.WaterLevel;
                    pos.Z -= 2f;
                    Explosion.Create(levelHandler, pos, Explosion.WaterSplash);
                    levelHandler.PlayCommonSound("WaterSplash", this, 1f, 0.5f);
                }
            } else {
                if (Transform.Pos.Y >= levelHandler.WaterLevel && waterCooldownLeft <= 0f) {
                    inWater = true;
                    waterCooldownLeft = 20f;

                    controllable = true;
                    EndDamagingMove();

                    var pos = Transform.Pos;
                    pos.Y = levelHandler.WaterLevel;
                    pos.Z -= 2f;
                    Explosion.Create(levelHandler, pos, Explosion.WaterSplash);
                    levelHandler.PlayCommonSound("WaterSplash", this, 0.7f, 0.5f);
                }
            }
        }

        private void OnHandleAreaEvents(float timeMult, out bool areaWeaponAllowed, out int areaWaterBlock)
        {
            areaWeaponAllowed = true;
            areaWaterBlock = -1;

            EventMap events = levelHandler.EventMap;
            if (events == null) {
                return;
            }

            Vector3 pos = Transform.Pos;

            ushort[] p = null;
            EventType tileEvent = events.GetEventByPosition(pos.X, pos.Y, ref p);
            switch (tileEvent) {
                case EventType.LightSet: { // Intensity, Red, Green, Blue, Flicker
                    // ToDo: Change only player view, handle splitscreen multiplayer
                    levelHandler.AmbientLightCurrent = p[0] * 0.01f;
                    break;
                }
                case EventType.WarpOrigin: { // Warp ID, Fast, Set Lap
                    if (currentTransitionState == AnimState.Idle || currentTransitionState == (AnimState.Dash | AnimState.Jump) || currentTransitionCancellable) {
#if MULTIPLAYER && !SERVER
                        if (!(levelHandler is MultiplayerLevelHandler))
#endif
                        {
                            Vector2 c = events.GetWarpTarget(p[0]);
                            if (c.X != -1f && c.Y != -1f) {
                                WarpToPosition(c, p[1] != 0);

#if MULTIPLAYER && SERVER
                                if (p[2] != 0) {
                                    ((LevelHandler)levelHandler).OnPlayerIncrementLaps(this);
                                }
#endif
                            }
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
                case EventType.ModifierTube: { // XSpeed, YSpeed, Wait Time, Trig Sample, Become Noclip, Noclip Only
                    // ToDo: Implement other parameters
                    if (p[4] == 0 && p[5] != 0 && (CollisionFlags & CollisionFlags.CollideWithTileset) != 0) {
                        break;
                    }

                    EndDamagingMove();

                    SetAnimation(AnimState.Dash | AnimState.Jump);

                    controllable = false;
                    canJump = false;
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;

                    speedX = unchecked((short)p[0]);
                    speedY = unchecked((short)p[1]);

                    if (speedX == 0f) {
                        pos.X = (MathF.Floor(pos.X / 32) * 32) + 16;
                        Transform.Pos = pos;
                        OnUpdateHitbox();
                    } else if (speedY == 0f) {
                        pos.Y = (MathF.Floor(pos.Y / 32) * 32) + 8;
                        Transform.Pos = pos;
                        OnUpdateHitbox();
                    } else if (inTubeTime <= 0f) {
                        pos.X = (MathF.Floor(pos.X / 32) * 32) + 16;
                        pos.Y = (MathF.Floor(pos.Y / 32) * 32) + 8;
                        Transform.Pos = pos;
                        OnUpdateHitbox();
                    }

                    if (p[4] != 0) { // Become Noclip
                        CollisionFlags &= ~CollisionFlags.CollideWithTileset;
                        inTubeTime = 60f;
                    } else {
                        inTubeTime = 10f;
                    }
                    break;
                }
                case EventType.AreaEndOfLevel: { // ExitType, Fast (No score count, only black screen), TextID, TextOffset, Coins
                    if (levelExiting == LevelExitingState.None) {
#if MULTIPLAYER && !SERVER
                        if (!(levelHandler is MultiplayerLevelHandler))
#endif
                        {
                            // ToDo: Implement Fast parameter
                            if (p[4] <= coins) {
                                coins -= p[4];

                                string nextLevel;
                                if (p[2] == 0) {
                                    nextLevel = null;
                                } else {
                                    nextLevel = levelHandler.GetLevelText(p[2]).SubstringByOffset('|', p[3]);
                                }
                                levelHandler.InitLevelChange((ExitType)p[0], nextLevel);
                                PlaySound("EndOfLevel");
                            } else if (bonusWarpTimer <= 0f) {
#if !SERVER
                                attachedHud?.ShowCoins(coins);
#endif
                                PlaySound("BonusWarpNotEnoughCoins");

                                bonusWarpTimer = 400f;
                            }
                        }
                    }
                    break;
                }
                case EventType.AreaText: { // Text, TextOffset, Vanish
                    string text = levelHandler.GetLevelText(p[0]);
                    if (p[1] != 0) {
                        text = text.SubstringByOffset('|', p[1]);
                    }
#if !SERVER
                    if (!string.IsNullOrEmpty(text)) {
                        attachedHud?.ShowLevelText(text, false);
                    }
#endif
                    if (p[2] != 0) {
                        levelHandler.EventMap.StoreTileEvent((int)(pos.X / 32), (int)(pos.Y / 32), EventType.Empty);
                    }
                    break;
                }
                case EventType.AreaCallback: { // Function, Param, Vanish
#if !SERVER
                    // ToDo: Call function #{p[0]}(sender, p[1]); implement level extensions
                    attachedHud?.ShowLevelText("\f[s:75]\f[w:95]\f[c:6]\n\n\n\nWARNING: Callbacks aren't implemented yet. (" + p[0] + ", " + p[1] + ")", false);
#endif
                    if (p[2] != 0) {
                        levelHandler.EventMap.StoreTileEvent((int)(pos.X / 32), (int)(pos.Y / 32), EventType.Empty);
                    }
                    break;
                }
                case EventType.AreaActivateBoss: { // Music
                    levelHandler.BroadcastTriggeredEvent(tileEvent, p);

                    // Deactivate sugar rush if it's active
                    if (sugarRushLeft > 1f) {
                        sugarRushLeft = 1f;
                    }
                    break;
                }
                case EventType.AreaFlyOff: {
                    if (activeModifier == Modifier.Airboard) {
#if MULTIPLAYER && !SERVER
                        if (!(levelHandler is MultiplayerLevelHandler))
#endif
                        {
                            SetModifier(Modifier.None);
                        }
                    }
                    break;
                }
                case EventType.AreaRevertMorph: {
                    if (playerType != playerTypeOriginal) {
#if MULTIPLAYER && !SERVER
                        if (!(levelHandler is MultiplayerLevelHandler))
#endif
                        {
                            MorphRevent();
                        }
                    }
                    break;
                }
                case EventType.AreaMorphToFrog: {
                    if (playerType != PlayerType.Frog) {
#if MULTIPLAYER && !SERVER
                        if (!(levelHandler is MultiplayerLevelHandler))
#endif
                        {
                            MorphTo(PlayerType.Frog);
                        }
                    }
                    break;
                }
                case EventType.AreaNoFire: {
                    switch (p[0]) {
                        case 0: areaWeaponAllowed = false; break;
                        case 1: weaponAllowed = true; break;
                        case 2: weaponAllowed = false; break;
                    }
                    break;
                }
                case EventType.TriggerZone: { // Trigger ID, Turn On, Switch
                    TileMap tiles = levelHandler.TileMap;
                    if (tiles != null) {
#if MULTIPLAYER && !SERVER
                        if (!(levelHandler is MultiplayerLevelHandler))
#endif
                        {
                            // ToDo: Implement Switch parameter
                            tiles.SetTrigger(p[0], p[1] != 0);
                        }
                    }
                    break;
                }

                case EventType.ModifierDeath: {
#if MULTIPLAYER && !SERVER
                    if (!(levelHandler is MultiplayerLevelHandler))
#endif
                    {
                        DecreaseHealth(int.MaxValue);
                    }
                    break;
                }
                case EventType.ModifierSetWater: { // Height, Instant, Lighting
                    // ToDo: Implement Instant (non-instant transition), Lighting
                    levelHandler.WaterLevel = p[0];
                    break;
                }
                case EventType.ModifierLimitCameraView: { // Left, Width
                    levelHandler.LimitCameraView((p[0] == 0 ? (int)(pos.X / 32) : p[0]) * 32, p[1] * 32);
                    break;
                }

                case EventType.RollingRockTrigger: { // Rock ID
                    levelHandler.BroadcastTriggeredEvent(tileEvent, p);
                    break;
                }

                case EventType.AreaWaterBlock: {
                    areaWaterBlock = ((int)pos.Y / 32) * 32 + p[0];
                    break;
                }
            }

            // ToDo: Implement Slide modifier with JJ2+ parameter

            // Check floating from each corner of an extended hitbox
            // Player should not pass from a single tile wide gap if the columns left or right have
            // float events, so checking for a wider box is necessary.
            const float ExtendedHitbox = 2f;

            if (currentSpecialMove != SpecialMoveType.Buttstomp) {
                if ((events.GetEventByPosition(pos.X, pos.Y, ref p) == EventType.AreaFloatUp) ||
                    (events.GetEventByPosition(AABBInner.LowerBound.X - ExtendedHitbox, AABBInner.LowerBound.Y - ExtendedHitbox, ref p) == EventType.AreaFloatUp) ||
                    (events.GetEventByPosition(AABBInner.UpperBound.X + ExtendedHitbox, AABBInner.LowerBound.Y - ExtendedHitbox, ref p) == EventType.AreaFloatUp) ||
                    (events.GetEventByPosition(AABBInner.UpperBound.X + ExtendedHitbox, AABBInner.UpperBound.Y + ExtendedHitbox, ref p) == EventType.AreaFloatUp) ||
                    (events.GetEventByPosition(AABBInner.LowerBound.X - ExtendedHitbox, AABBInner.UpperBound.Y + ExtendedHitbox, ref p) == EventType.AreaFloatUp)
                ) {
                    if ((CollisionFlags & CollisionFlags.ApplyGravitation) != 0) {
                        float gravity = levelHandler.Gravity;

                        externalForceY = gravity * 2f * timeMult;
                        speedY = MathF.Min(gravity * timeMult, speedY);
                    } else {
                        speedY -= levelHandler.Gravity * 1.2f * timeMult;
                    }
                }
            }

            if ((events.GetEventByPosition(pos.X, pos.Y, ref p) == EventType.AreaHForce) ||
                (events.GetEventByPosition(AABBInner.LowerBound.X - ExtendedHitbox, AABBInner.LowerBound.Y - ExtendedHitbox, ref p) == EventType.AreaHForce) ||
                (events.GetEventByPosition(AABBInner.UpperBound.X + ExtendedHitbox, AABBInner.LowerBound.Y - ExtendedHitbox, ref p) == EventType.AreaHForce) ||
                (events.GetEventByPosition(AABBInner.UpperBound.X + ExtendedHitbox, AABBInner.UpperBound.Y + ExtendedHitbox, ref p) == EventType.AreaHForce) ||
                (events.GetEventByPosition(AABBInner.LowerBound.X - ExtendedHitbox, AABBInner.UpperBound.Y + ExtendedHitbox, ref p) == EventType.AreaHForce)
               ) {
                if ((p[5] != 0 || p[4] != 0)) {
                    MoveInstantly(new Vector2((p[5] - p[4]) * 0.4f * timeMult, 0), MoveType.Relative);
                }
            }

            //
            if (canJump) {
                // Floor events
                tileEvent = events.GetEventByPosition(pos.X, pos.Y + 32, ref p);
                switch (tileEvent) {
                    case EventType.AreaHForce: {
                        if (p[1] != 0 || p[0] != 0) {
                            MoveInstantly(new Vector2((p[1] - p[0]) * 0.4f * timeMult, 0), MoveType.Relative);
                        }
                        if (p[3] != 0 || p[2] != 0) {
                            speedX += (p[3] - p[2]) * 0.1f;
                        }
                        break;
                    }
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            //base.OnHandleCollision(other);

            bool removeSpecialMove = false;

            switch (other) {
                case TurtleShell collider: {
                    if (currentSpecialMove != SpecialMoveType.None || sugarRushLeft > 0f) {
                        collider.DecreaseHealth(int.MaxValue, this);

                        if ((currentAnimationState & AnimState.Buttstomp) != 0) {
                            removeSpecialMove = true;
                            speedY *= -0.6f;
                            canJump = true;
                        }
                    }
                    break;
                }

                case EnemyBase collider: {
                    if (currentSpecialMove != SpecialMoveType.None || sugarRushLeft > 0f || shieldTime > 0f) {
                        if (!collider.IsInvulnerable) {
                            collider.DecreaseHealth(4, this);

                            Explosion.Create(levelHandler, collider.Transform.Pos, Explosion.Small);

                            if (sugarRushLeft > 0f) {
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
                            } else if (currentSpecialMove != SpecialMoveType.None && collider.Health >= 0) {
                                removeSpecialMove = true;
                                externalForceX = 0f;
                                externalForceY = 0f;

                                if (currentSpecialMove == SpecialMoveType.Sidekick) {
                                    speedX *= 0.5f;
                                }
                            }
                        }

                        // Decrease remaining shield time by 5 secs
                        if (shieldTime > 0f) {
                            shieldTime = Math.Max(1f, shieldTime - 5f * Time.FramesPerSecond);
                        }
                    } else if (collider.CanHurtPlayer) {
                        TakeDamage(1, 4 * (Transform.Pos.X > collider.Transform.Pos.X ? 1 : -1));
                    }
                    break;
                }

                case Spring spring: {
                    // Collide only with hitbox
                    if (controllableExternal && AABB.TestOverlap(ref spring.AABBInner, ref AABBInner)) {
                        Vector2 force = spring.Activate();
                        OnSpringActivated(force, spring.KeepSpeedX, spring.KeepSpeedY);

#if MULTIPLAYER && SERVER
                        ((LevelHandler)levelHandler).OnPlayerSpringActivated(this, force, spring.KeepSpeedX, spring.KeepSpeedY);
#endif
                    }
                    break;
                }

                case PinballBumper bumper: {
                    Vector2 force = bumper.Activate(this);
                    OnPinballBumperActivated(force);

#if MULTIPLAYER && SERVER
                    ((LevelHandler)levelHandler).OnPlayerPinballBumperActivated(this, force);
#endif
                    break;
                }

                case PinballPaddle paddle: {
                    Vector2 force = paddle.Activate(this);
                    OnPinballPaddleActivated(force);

#if MULTIPLAYER && SERVER
                    ((LevelHandler)levelHandler).OnPlayerPinballPaddleActivated(this, force);
#endif
                    break;
                }

                case SwingingVine vine: {
                    if (currentVine == null && speedY > 1f) {
                        currentVine = vine;
                        suspendType = SuspendType.SwingingVine;
                        CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                    }
                    break;
                }

                case BonusWarp warp: {
                    if (currentTransitionState == AnimState.Idle || currentTransitionCancellable) {
                        if (warp.Cost <= coins) {
                            coins -= warp.Cost;
                            warp.Activate(this);

                            // Convert remaing coins to gems
                            gems += coins;
                            coins = 0;
                        } else if (bonusWarpTimer <= 0f) {
#if !SERVER
                            attachedHud?.ShowCoins(coins);
#endif
                            PlaySound("BonusWarpNotEnoughCoins");

                            bonusWarpTimer = 400f;
                        }
                    }
                    break;
                }

#if MULTIPLAYER && SERVER
                case Weapons.AmmoBase ammo: {
                    if (ammo.Owner != this) {
                        // ToDo: Add read damage amount
                        bool damageTaken = TakeDamage(1, 0f);
                        ammo.DecreaseHealth(int.MaxValue, this);

                        if (damageTaken) {
                            ((LevelHandler)levelHandler).OnPlayerHit(this, ammo.Owner, health <= 0);
                        }
                    }
                    break;
                }
#endif
            }

            if (removeSpecialMove) {
                controllable = true;
                EndDamagingMove();
            }
        }

        public void OnSpringActivated(Vector2 force, bool keepSpeedX, bool keepSpeedY)
        {
            bool removeSpecialMove;

            int sign = ((force.X + force.Y) > float.Epsilon ? 1 : -1);
            if (MathF.Abs(force.X) > 0f) {
                removeSpecialMove = true;
                copterFramesLeft = 0f;
                //speedX = force.X;
                speedX = (1 + MathF.Abs(force.X)) * sign;
                externalForceX = force.X * 0.6f;

                wasActivelyPushing = false;

                keepRunningTime = 100f;

                if (!keepSpeedY) {
                    speedY = 0f;
                    externalForceY = 0f;
                }

                SetPlayerTransition(AnimState.Dash | AnimState.Jump, true, false, SpecialMoveType.None);
                controllableTimeout = 2f;
            } else if (MathF.Abs(force.Y) > 0f) {
                copterFramesLeft = 0f;
                speedY = (4 + MathF.Abs(force.Y)) * sign;
                externalForceY = -force.Y;

                if (!keepSpeedX) {
                    speedX = 0f;
                    externalForceX = 0f;
                    keepRunningTime = 0f;
                }

                if (sign > 0) {
                    removeSpecialMove = false;
                    currentSpecialMove = SpecialMoveType.Buttstomp;
                    SetAnimation(AnimState.Buttstomp);
                } else {
                    removeSpecialMove = true;
                    isSpring = true;
                }

                PlaySound("Spring");
            } else {
                return;
            }

            canJump = false;
            if (removeSpecialMove) {
                controllable = true;
                EndDamagingMove();
            }
        }

        public void OnPinballBumperActivated(Vector2 force)
        {
            if (force != Vector2.Zero) {
                canJump = false;

                speedX += force.X * 0.4f;
                speedY += force.Y * 0.4f;
                externalForceX += force.X * 0.04f;
                externalForceY -= force.Y * 0.04f;

                controllable = true;
                EndDamagingMove();

                // ToDo: Check this...
                AddScore(500);
            }
        }

        public void OnPinballPaddleActivated(Vector2 force)
        {
            if (force != Vector2.Zero) {
                copterFramesLeft = 0f;
                canJump = false;

                speedX = force.X;
                speedY = force.Y;

                controllable = true;
                EndDamagingMove();
            }
        }

        private void EndDamagingMove()
        {
            CollisionFlags |= CollisionFlags.ApplyGravitation;
            SetAnimation(currentAnimationState & ~(AnimState.Uppercut | AnimState.Buttstomp));

            if (currentSpecialMove == SpecialMoveType.Uppercut) {
                if (suspendType == SuspendType.None) {
                    SetTransition(AnimState.TransitionUppercutEnd, false);
                }
                controllable = true;

                if (externalForceY > 0f) {
                    externalForceY = 0f;
                }
            } else if (currentSpecialMove == SpecialMoveType.Sidekick) {
                CancelTransition();
                controllable = true;
                controllableTimeout = 10;
            }

            currentSpecialMove = SpecialMoveType.None;
        }

        private bool SetPlayerTransition(AnimState state, bool cancellable, bool removeControl, SpecialMoveType specialMove, Action callback = null)
        {
            if (removeControl) {
                controllable = false;
                controllableTimeout = 0f;
            }

            currentSpecialMove = specialMove;
            return SetTransition(state, cancellable, callback);
        }

        public bool TakeDamage(int amount, float pushForce)
        {
            if (isInvulnerable || levelExiting != LevelExitingState.None) {
                return false;
            }

            // Cancel active climbing
            if (currentTransitionState == AnimState.TransitionLedgeClimb) {
                ForceCancelTransition();

                MoveInstantly(new Vector2(IsFacingLeft ? 6f : -6f, 0f), MoveType.Relative, true);
            }

            DecreaseHealth(amount, null);

            internalForceY = 0f;
            speedX = 0f;
            canJump = false;
            isAttachedToPole = false;

            fireFramesLeft = copterFramesLeft = pushFramesLeft = 0f;

            if (activeBird != null) {
                activeBird.FlyAway();
                activeBird = null;
            }

            if (health > 0) {
                externalForceX = pushForce;

                if (!inWater && activeModifier == Modifier.None) {
                    speedY = -6.5f;

                    CollisionFlags |= CollisionFlags.ApplyGravitation;
                    SetAnimation(AnimState.Idle);
                } else {
                    speedY = -1f;
                }

                SetPlayerTransition(AnimState.Hurt, false, true, SpecialMoveType.None, delegate {
                    controllable = true;
                });

                if (levelHandler.Difficulty == GameDifficulty.Multiplayer) {
                    SetInvulnerability(80f, false);
                } else {
                    SetInvulnerability(180f, false);
                }

                PlaySound("Hurt");
            } else {
                externalForceX = 0f;
                speedY = 0f;

                PlaySound("Die", 1.3f);
            }

#if MULTIPLAYER && SERVER
            ((LevelHandler)levelHandler).OnPlayerTakeDamage(this, pushForce);
#endif
            return true;
        }

        public void TakeDamageFromServer(int healthAfter, float pushForce)
        {
            // Cancel active climbing
            if (currentTransitionState == AnimState.TransitionLedgeClimb) {
                ForceCancelTransition();

                MoveInstantly(new Vector2(IsFacingLeft ? 6f : -6f, 0f), MoveType.Relative, true);
            }

            health = healthAfter;

            internalForceY = 0f;
            speedX = 0f;
            canJump = false;
            isAttachedToPole = false;

            fireFramesLeft = copterFramesLeft = pushFramesLeft = 0f;

            if (activeBird != null) {
                activeBird.FlyAway();
                activeBird = null;
            }

            if (health > 0) {
                externalForceX = pushForce;

                if (!inWater && activeModifier == Modifier.None) {
                    speedY = -6.5f;

                    CollisionFlags |= CollisionFlags.ApplyGravitation;
                    SetAnimation(AnimState.Idle);
                } else {
                    speedY = -1f;
                }

                SetPlayerTransition(AnimState.Hurt, false, true, SpecialMoveType.None, delegate {
                    controllable = true;
                });
                SetInvulnerability(180f, false);

                PlaySound("Hurt");
            } else {
                OnPerish(null);

                externalForceX = 0f;
                speedY = 0f;
            }
        }

        public void Respawn(Vector2? pos = null)
        {
            if (health > 0) {
                return;
            }

            // Reset health
            health = maxHealth;

            if (pos == null) {
                // Return to the last save point
                MoveInstantly(checkpointPos, MoveType.Absolute, true);
                levelHandler.AmbientLightCurrent = checkpointLight;
                levelHandler.LimitCameraView(0, 0);
                levelHandler.WarpCameraToTarget(this);
            } else {
                MoveInstantly(pos.Value, MoveType.Absolute, true);
                levelHandler.WarpCameraToTarget(this);
            }

            isInvulnerable = false;
            CollisionFlags |= CollisionFlags.ApplyGravitation;

            controllable = true;
            renderer.Active = true;
            renderer.AnimHidden = false;
        }

        public void WarpToPosition(Vector2 pos, bool fast)
        {
            if (fast) {
                Vector3 posOld = Transform.Pos;

                MoveInstantly(pos, MoveType.Absolute, true);

                if (new Vector2(posOld.X - pos.X, posOld.Y - pos.Y).Length > 250) {
                    levelHandler.WarpCameraToTarget(this);
                }
            } else {
                EndDamagingMove();
                isInvulnerable = true;
                CollisionFlags &= ~CollisionFlags.ApplyGravitation;

                SetAnimation(currentAnimationState & ~(AnimState.Uppercut | AnimState.Buttstomp));

                speedX = 0f;
                speedY = 0f;
                externalForceX = 0f;
                externalForceY = 0f;
                internalForceY = 0f;
                fireFramesLeft = 0f;
                copterFramesLeft = 0f;
                pushFramesLeft = 0f;

                // For warping from the water
                Transform.Angle = 0f;

                PlaySound("WarpIn");

                SetPlayerTransition(isFreefall ? AnimState.TransitionWarpInFreefall : AnimState.TransitionWarpIn, false, true, SpecialMoveType.None, delegate {
                    Vector3 posOld = Transform.Pos;

                    MoveInstantly(pos, MoveType.Absolute, true);
                    PlaySound("WarpOut");

                    if (new Vector2(posOld.X - pos.X, posOld.Y - pos.Y).Length > 250) {
                        levelHandler.WarpCameraToTarget(this);
                    }

                    isFreefall = isFreefall || CanFreefall();
                    SetPlayerTransition(isFreefall ? AnimState.TransitionWarpOutFreefall : AnimState.TransitionWarpOut, false, true, SpecialMoveType.None, delegate {
                        isInvulnerable = false;
                        CollisionFlags |= CollisionFlags.ApplyGravitation;
                        controllable = true;
                    });
                });
            }

#if MULTIPLAYER && SERVER
            ((LevelHandler)levelHandler).OnPlayerWarpToPosition(this, pos, fast);
#endif
        }

        private void InitialPoleStage(bool horizontal)
        {
            if (isAttachedToPole || playerType == PlayerType.Frog) {
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

            float activeForce, lastSpeed;
            if (horizontal) {
                activeForce = (Math.Abs(externalForceX) > 1f ? externalForceX : speedX);
                lastSpeed = speedX;
            } else {
                activeForce = speedY;
                lastSpeed = speedY;
            }
            bool positive = (activeForce >= 0);

            pos.X = x * 32 + 16;
            pos.Y = y * 32 + 16;
            Transform.Pos = pos;

            OnUpdateHitbox();

            speedX = 0f;
            speedY = 0f;
            externalForceX = 0f;
            externalForceY = 0f;
            internalForceY = 0f;
            CollisionFlags &= ~CollisionFlags.ApplyGravitation;
            isAttachedToPole = true;
            inIdleTransition = false;

            keepRunningTime = 0f;

            SetAnimation(currentAnimationState & ~(AnimState.Uppercut /*| AnimState.Sidekick*/ | AnimState.Buttstomp));

            AnimState poleAnim = (horizontal ? AnimState.TransitionPoleHSlow : AnimState.TransitionPoleVSlow);
            SetPlayerTransition(poleAnim, false, true, SpecialMoveType.None, delegate {
                NextPoleStage(horizontal, positive, 2, lastSpeed);
            });

            controllableTimeout = 80f;

            PlaySound("Pole", 0.8f, 0.6f);
        }

        private void NextPoleStage(bool horizontal, bool positive, int stagesLeft, float lastSpeed)
        {
            if (stagesLeft > 0) {
                AnimState poleAnim = (horizontal ? AnimState.TransitionPoleH : AnimState.TransitionPoleV);
                SetPlayerTransition(poleAnim, false, true, SpecialMoveType.None, delegate {
                    NextPoleStage(horizontal, positive, stagesLeft - 1, lastSpeed);
                });

                inIdleTransition = false;
                controllableTimeout = 80f;

                PlaySound("Pole", 1f, 0.6f);
            } else {
                int sign = (positive ? 1 : -1);
                if (horizontal) {
                    // To prevent stucking
                    for (int i = -1; i > -6; i--) {
                        if (MoveInstantly(new Vector2(speedX, i), MoveType.Relative)) {
                            break;
                        }
                    }

                    speedX = 10 * sign + lastSpeed * 0.2f;
                    externalForceX = 10 * sign;
                    IsFacingLeft = !positive;

                    keepRunningTime = 60f;

                    SetPlayerTransition(AnimState.Dash | AnimState.Jump, true, true, SpecialMoveType.None);
                } else {
                    MoveInstantly(new Vector2(0, sign * 16), MoveType.Relative, true);

                    speedY = 4 * sign + lastSpeed * 1.4f;
                    externalForceY = (-1.3f * sign);
                }

                CollisionFlags |= CollisionFlags.ApplyGravitation;
                isAttachedToPole = false;
                wasActivelyPushing = false;
                inIdleTransition = false;

                controllableTimeout = 4f;
                lastPoleTime = 10f;

                PlaySound("HookAttach", 0.8f, 1.2f);
            }
        }

        private bool CanFreefall()
        {
            Vector3 pos = Transform.Pos;
            AABB aabb = new AABB(pos.X - 14, pos.Y + 8 - 12, pos.X + 14, pos.Y + 8 + 12 + 100);
            return levelHandler.IsPositionEmpty(this, ref aabb, true);
        }

        public void SetCarryingPlatform(MovingPlatform platform)
        {
            if (speedY < 0f || inWater || activeModifier != Modifier.None) {
                return;
            }

            carryingObject = platform;
            canJump = true;
            internalForceY = 0f;
            speedY = 0f;
        }

        private void FollowCarryingPlatform()
        {
            if (carryingObject != null) {
                if (!canJump || !controllable || (CollisionFlags & CollisionFlags.ApplyGravitation) == 0) {
                    carryingObject = null;
                } else {
                    Vector2 delta = carryingObject.GetLocationDelta();

                    // Try to adjust Y, because it collides with carrying platform sometimes
                    for (int i = 0; i < 4; i++) {
                        delta.Y -= 1f;
                        if (MoveInstantly(delta, MoveType.Relative)) {
                            return;
                        }
                    }

                    MoveInstantly(new Vector2(0f, delta.Y), MoveType.Relative);
                }
            } else if (currentVine != null) {
                Vector3 pos = Transform.Pos;
                Vector2 newPos = currentVine.AttachPoint + new Vector2(0f, 14f);
                MoveInstantly(newPos, MoveType.Absolute);

                if (IsFacingLeft) {
                    if (newPos.X > pos.X) {
                        renderer.AnimTime = 0;
                        IsFacingLeft = false;
                    }
                } else {
                    if (newPos.X < pos.X) {
                        renderer.AnimTime = 0;
                        IsFacingLeft = true;
                    }
                }
            }
        }

        public void SetInvulnerability(float time, bool withCircleEffect)
        {
            if (time <= 0f) {
                isInvulnerable = false;
                invulnerableTime = 0;

                renderer.AnimHidden = false;

                SetCircleEffect(false);

#if MULTIPLAYER && SERVER
                ((LevelHandler)levelHandler).OnPlayerSetInvulnerability(this, 0f, false);
#endif
                return;
            }

            isInvulnerable = true;
            invulnerableTime = time;

            if (withCircleEffect) {
                SetCircleEffect(true);
            }

#if MULTIPLAYER && SERVER
            ((LevelHandler)levelHandler).OnPlayerSetInvulnerability(this, time, withCircleEffect);
#endif
        }

#if !SERVER
        public void AttachToHud(Hud hud)
        {
            if (attachedHud != null) {
                attachedHud.Owner = null;
            }

            attachedHud = hud;

            if (attachedHud != null) {
                attachedHud.Owner = this;
            }
        }
#endif
    }
}