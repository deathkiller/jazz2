using System;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game;
using Jazz2.Game.Structs;
using MathF = Duality.MathF;

namespace Jazz2.Actors
{
    partial class Player
    {
        private Bird activeBird;

        public void AddScore(uint plus)
        {
            score = Math.Min(score + plus, 999999999u);
        }

        public bool AddHealth(int amount)
        {
            const int HealthLimit = 5;

            if (health >= HealthLimit) {
                return false;
            }

            if (amount < 0) {
                health = Math.Max(maxHealth, HealthLimit);
                PlaySound("PickupMaxCarrot");
            } else {
                health = Math.Min(health + amount, HealthLimit);
                if (maxHealth < health) {
                    maxHealth = health;
                }
                PlaySound("PickupFood");
            }

#if MULTIPLAYER && SERVER
            ((LevelHandler)levelHandler).OnPlayerAddHealth(this, amount);
#endif

            return true;
        }

        public void AddLives(int count)
        {
            lives += count;

            PlaySound("PickupOneUp");
        }

        public void AddCoins(int count)
        {
            coins += count;

#if !SERVER
            attachedHud?.ShowCoins(coins);
#endif
            PlaySound("PickupCoin");
        }

        public void AddGems(int count)
        {
            gems += count;
#if !SERVER
            attachedHud?.ShowGems(gems);
#endif
            PlaySound("PickupGem", 1f, MathF.Min(0.7f + gemsPitch * 0.05f, 1.3f));

            gemsTimer = 120f;
            gemsPitch++;

#if MULTIPLAYER && SERVER
            ((LevelHandler)levelHandler).OnPlayerAddGems(this, count);
#endif
        }

        public void ConsumeFood(bool isDrinkable)
        {
            if (isDrinkable) {
                PlaySound("PickupDrink");
            } else {
                PlaySound("PickupFood");
            }

            foodEaten++;
            if (foodEaten >= 100) {
                foodEaten = foodEaten % 100;
                BeginSugarRush();
            }
        }

        public bool SetDizzyTime(float time)
        {
            bool wasNotDizzy = (dizzyTime <= 0f);

            dizzyTime = time;

            return wasNotDizzy;
        }

        public void ShowLevelText(string text, bool bigger)
        {
#if !SERVER
            attachedHud?.ShowLevelText(text, bigger);
#endif
        }

        public void MorphTo(PlayerType type)
        {
            if (playerType == type) {
                return;
            }

            PlayerType playerTypePrevious = playerType;

            playerType = type;

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
                case PlayerType.Frog:
                    RequestMetadata("Interactive/PlayerFrog");
                    break;
            }

            // Refresh animation state
            if ((currentSpecialMove == SpecialMoveType.None) ||
                (currentSpecialMove == SpecialMoveType.Buttstomp && (type == PlayerType.Jazz || type == PlayerType.Spaz || type == PlayerType.Lori))) {
                currentAnimation = null;
                SetAnimation(currentAnimationState);
            } else {
                currentAnimation = null;
                SetAnimation(AnimState.Fall);

                CollisionFlags |= CollisionFlags.ApplyGravitation;
                controllable = true;

                if (currentSpecialMove == SpecialMoveType.Uppercut && externalForceY > 0f) {
                    externalForceY = 0f;
                }

                currentSpecialMove = SpecialMoveType.None;
            }

            // Set transition
            if (type == PlayerType.Frog) {
                PlaySound("Transform");

                controllable = false;
                controllableTimeout = 120f;

                switch (playerTypePrevious) {
                    case PlayerType.Jazz:
                        SetTransition((AnimState)0x60000000, false, delegate {
                            controllable = true;
                            controllableTimeout = 0f;
                        });
                        break;
                    case PlayerType.Spaz:
                        SetTransition((AnimState)0x60000001, false, delegate {
                            controllable = true;
                            controllableTimeout = 0f;
                        });
                        break;
                    case PlayerType.Lori:
                        SetTransition((AnimState)0x60000002, false, delegate {
                            controllable = true;
                            controllableTimeout = 0f;
                        });
                        break;
                }
            } else if (playerTypePrevious == PlayerType.Frog) {
                controllable = false;
                controllableTimeout = 120f;

                SetTransition(AnimState.TransitionFromFrog, false, delegate {
                    controllable = true;
                    controllableTimeout = 0f;
                });
            } else {
                Explosion.Create(levelHandler, Transform.Pos + new Vector3(-12f, -6f, -4f), Explosion.SmokeBrown);
                Explosion.Create(levelHandler, Transform.Pos + new Vector3(-8f, 28f, -4f), Explosion.SmokeBrown);
                Explosion.Create(levelHandler, Transform.Pos + new Vector3(12f, 10f, -4f), Explosion.SmokeBrown);

                Explosion.Create(levelHandler, Transform.Pos + new Vector3(0f, 12f, -6f), Explosion.SmokePoof);
            }
        }

        public void MorphRevent()
        {
            MorphTo(playerTypeOriginal);
        }

        public bool SetModifier(Modifier modifier)
        {
            if (activeModifier == modifier) {
                return false;
            }

            switch (modifier) {
                case Modifier.Airboard: {
                    controllable = true;
                    EndDamagingMove();
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;

                    speedY = 0f;
                    externalForceY = 0f;
                    internalForceY = 0f;

                    activeModifier = Modifier.Airboard;

                    MoveInstantly(new Vector2(0f, -16f), MoveType.Relative);
                    break;
                }
                case Modifier.Copter: {
                    controllable = true;
                    EndDamagingMove();
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;

                    speedY = 0f;
                    externalForceY = 0f;

                    activeModifier = Modifier.Copter;

                    copterFramesLeft = 350;
                    break;
                }
                case Modifier.LizardCopter: {
                    controllable = true;
                    EndDamagingMove();
                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;

                    speedY = 0f;
                    externalForceY = 0f;
                    internalForceY = 0f;

                    activeModifier = Modifier.LizardCopter;

                    copterFramesLeft = 150;

                    CopterDecor copter = new CopterDecor();
                    copter.OnActivated(new ActorActivationDetails {
                        LevelHandler = levelHandler
                    });
                    copter.Parent = this;
                    break;
                }

                default: {
                    activeModifier = Modifier.None;

                    CopterDecor copterDecor = GetFirstChild<CopterDecor>();
                    if (copterDecor != null) {
                        copterDecor.DecreaseHealth(int.MaxValue);
                    }

                    CollisionFlags |= CollisionFlags.ApplyGravitation;
                    canJump = true;

                    SetAnimation(AnimState.Fall);
                    break;
                }
            }

#if MULTIPLAYER && SERVER
            ((LevelHandler)levelHandler).OnPlayerSetModifier(this, modifier);
#endif

            return true;
        }

        public bool SpawnBird(ushort type, Vector3 pos)
        {
            if (activeBird != null) {
                return false;
            }

            pos.Z = Transform.Pos.Z - 100f;

            activeBird = new Bird();
            activeBird.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = pos,
                Params = new ushort[] { type }
            });
            activeBird.OnLinkWithPlayer(this);
            levelHandler.AddActor(activeBird);
            return true;
        }

        public bool DisableControllable(float timeout)
        {
            if (!controllable) {
                if (timeout <= 0f) {
                    controllable = true;
                    controllableTimeout = 0f;
                    return true;
                } else {
                    return false;
                }
            }

            if (timeout <= 0f) {
                return false;
            }

            controllable = false;
            if (timeout == float.PositiveInfinity) {
                controllableTimeout = 0f;
            } else {
                controllableTimeout = timeout;
            }

            SetAnimation(AnimState.Idle);
            return true;
        }

        public void SetCheckpoint(Vector2 pos)
        {
            checkpointPos = pos + new Vector2(0f, -20f);
            checkpointLight = levelHandler.AmbientLightCurrent;
        }

        public void BeginSugarRush()
        {
            if (sugarRushLeft > 0f) {
                return;
            }

            sugarRushLeft = 1200f;

            OnAnimationStarted();
        }

        private class CopterDecor : ActorBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                CollisionFlags = CollisionFlags.ForceDisableCollisions;

                health = int.MaxValue;

                await RequestMetadataAsync("Enemy/LizardFloat");
                SetAnimation(AnimState.Activated);
            }

            public override void OnFixedUpdate(float timeMult)
            {
                Transform.RelativePos = new Vector3(0f, 0f, 4f);
            }
        }
    }
}