﻿using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Doggy : EnemyBase
    {
        private float attackSpeed;
        private float attackTime;
        private float noiseCooldown = 120f;
        private bool stuck;

        public static void Preload(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    PreloadMetadata("Enemy/Doggy");
                    break;

                case 1: // TSF Cat
                    PreloadMetadata("Enemy/Cat");
                    break;
            }
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Doggy();
            actor.OnActivated(details);
            return actor;
        }

        private Doggy()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.Y -= 6f;
            Transform.Pos = pos;

            SetHealthByDifficulty(3);
            scoreValue = 200;

            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    await RequestMetadataAsync("Enemy/Doggy");
                    attackSpeed = 3.2f;
                    break;

                case 1: // TSF Cat
                    await RequestMetadataAsync("Enemy/Cat");
                    attackSpeed = 3.8f;
                    break;
            }

            SetAnimation(AnimState.Walk);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * 1f;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(50, 30);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            if (attackTime <= 0f) {
                speedX = (IsFacingLeft ? -1 : 1) * 1f;
                SetAnimation(AnimState.Walk);

                if (noiseCooldown <= 0f) {
                    noiseCooldown = MathF.Rnd.NextFloat(100, 300);
                    PlaySound("Noise", 0.4f);
                } else {
                    noiseCooldown -= timeMult;
                }
            } else {
                attackTime -= timeMult;

                if (noiseCooldown <= 0f) {
                    noiseCooldown = MathF.Rnd.NextFloat(25, 40);
                    PlaySound("Woof");
                } else {
                    noiseCooldown -= timeMult;
                }
            }

            if (canJump) {
                if (!CanMoveToPosition(speedX * 4, 0)) {
                    if (stuck) {
                        MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                    } else {
                        IsFacingLeft ^= true;
                        speedX = (IsFacingLeft ? -1f : 1f) * (attackTime <= 0f ? 1f : attackSpeed);
                        stuck = true;
                    }
                } else {
                    stuck = false;
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        public override void OnHandleCollision(ActorBase other)
        {
            AmmoBase ammo = other as AmmoBase;
            if (ammo != null) {
                DecreaseHealth(ammo.Strength, ammo);

                if (health <= 0) {
                    return;
                }

                HandleAmmoFrozenStateChange(ammo);
                
                if (!(ammo is AmmoFreezer)) {
                    if (attackTime <= 0f) {
                        PlaySound("Attack");

                        speedX = (IsFacingLeft ? -1f : 1f) * attackSpeed;
                        SetAnimation(AnimState.TransitionAttack);
                    }

                    attackTime = 200f;
                    noiseCooldown = 45f;
                }
            }
        }
    }
}