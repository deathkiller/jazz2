using System;
using Duality;
using Jazz2.Game.Structs;
using MathF = Duality.MathF;

namespace Jazz2.Actors
{
    partial class Player
    {
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
                health = Math.Max(maxHealth, healthLimit);
                PlaySound("PickupMaxCarrot");
            } else {
                health = Math.Min(health + count, healthLimit);
                if (maxHealth < health) {
                    maxHealth = health;
                }
                PlaySound("PickupFood");
            }

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

            attachedHud?.ShowCoins(coins);
            PlaySound("PickupCoin");
        }

        public void AddGems(int count)
        {
            gems += count;

            attachedHud?.ShowGems(gems);

            PlaySound("PickupGem", 1f, MathF.Min(0.7f + gemsPitch * 0.05f, 1.3f));

            gemsTimer = 120f;
            gemsPitch++;
        }

        public void ConsumeFood(bool isDrinkable)
        {
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
                PlaySound("PickupDrink");
            } else {
                PlaySound("PickupFood");
            }
        }

        public void SetDizzyTime(float time)
        {
            dizzyTime = time;
        }

        public void ShowLevelText(string text)
        {
            attachedHud?.ShowLevelText(text);
        }

        public void MorphTo(PlayerType type)
        {
            PlayerType playerTypePrevious = playerType;

            playerType = type;

            //if (playerTypePrevious == PlayerType.Frog) {
            //    switch (playerType) {
            //        case PlayerType.Jazz: SetTransition((AnimState)0x60000003, false); break;
            //        case PlayerType.Spaz: SetTransition((AnimState)0x60000004, false); break;
            //        case PlayerType.Lori: SetTransition((AnimState)0x60000005, false); break;
            //    }
            //}

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
            //if (playerTypePrevious != PlayerType.Frog) {
                currentAnimation = null;
                SetAnimation(currentAnimationState);
            //}

            // Set transition
            if (type == PlayerType.Frog) {
                PlaySound("Transform");

                switch (playerTypePrevious) {
                    case PlayerType.Jazz: SetTransition((AnimState)0x60000000, false); break;
                    case PlayerType.Spaz: SetTransition((AnimState)0x60000001, false); break;
                    case PlayerType.Lori: SetTransition((AnimState)0x60000002, false); break;
                }
            //} else if (playerTypePrevious == PlayerType.Frog) {
            //    // Transition was already set
            } else {
                Explosion.Create(api, Transform.Pos + new Vector3(-12f, -6f, -4f), Explosion.SmokeBrown);
                Explosion.Create(api, Transform.Pos + new Vector3(-8f, 28f, -4f), Explosion.SmokeBrown);
                Explosion.Create(api, Transform.Pos + new Vector3(12f, 10f, -4f), Explosion.SmokeBrown);

                Explosion.Create(api, Transform.Pos + new Vector3(0f, 12f, -6f), Explosion.SmokePoof);
            }
        }

        public void MorphToOriginal()
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
                    collisionFlags &= ~CollisionFlags.ApplyGravitation;

                    speedY = 0f;
                    externalForceY = 0f;

                    activeModifier = Modifier.Airboard;

                    MoveInstantly(new Vector2(0f, -16f), MoveType.Relative);
                    return true;
                }
                case Modifier.Copter: {
                    controllable = true;
                    EndDamagingMove();
                    collisionFlags &= ~CollisionFlags.ApplyGravitation;

                    speedY = 0f;
                    externalForceY = 0f;

                    activeModifier = Modifier.Copter;

                    copterFramesLeft = 350;
                    return true;
                }
                case Modifier.LizardCopter: {
                    controllable = true;
                    EndDamagingMove();
                    collisionFlags &= ~CollisionFlags.ApplyGravitation;

                    speedY = 0f;
                    externalForceY = 0f;

                    activeModifier = Modifier.LizardCopter;

                    copterFramesLeft = 150;

                    CopterDecor copter = new CopterDecor();
                    copter.OnAttach(new ActorInstantiationDetails {
                        Api = api
                    });
                    copter.Parent = this;
                    return true;
                }

                default: {
                    activeModifier = Modifier.None;

                    CopterDecor copterDecor = GetFirstChild<CopterDecor>();
                    if (copterDecor != null) {
                        copterDecor.DecreaseHealth(int.MaxValue);
                    }

                    collisionFlags |= CollisionFlags.ApplyGravitation;
                    canJump = true;

                    SetAnimation(AnimState.Fall);
                    return true;
                }
            }
        }

        public void SetCheckpoint(Vector2 pos)
        {
            checkpointPos = pos + new Vector2(0f, -20f);
            checkpointLight = api.AmbientLight;
        }

        public class CopterDecor : ActorBase
        {
            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                collisionFlags = CollisionFlags.None;

                health = int.MaxValue;

                RequestMetadata("Enemy/LizardFloat");
                SetAnimation(AnimState.Activated);
            }

            protected override void OnUpdate()
            {
                Transform.RelativePos = new Vector3(0f, 0f, 4f);
            }
        }
    }
}