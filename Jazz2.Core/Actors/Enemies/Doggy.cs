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

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            Vector3 pos = Transform.Pos;
            pos.Y -= 6f;
            Transform.Pos = pos;

            SetHealthByDifficulty(3);
            scoreValue = 200;

            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    RequestMetadata("Enemy/Doggy");
                    attackSpeed = 3.2f;
                    break;

                case 1: // TSF Cat
                    RequestMetadata("Enemy/Cat");
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

        protected override void OnUpdate()
        {
            base.OnUpdate();

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
                    noiseCooldown -= Time.TimeMult;
                }
            } else {
                attackTime -= Time.TimeMult;

                if (noiseCooldown <= 0f) {
                    noiseCooldown = MathF.Rnd.NextFloat(25, 40);
                    PlaySound("Woof");
                } else {
                    noiseCooldown -= Time.TimeMult;
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
            api.PlayCommonSound(this, "Splat");

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