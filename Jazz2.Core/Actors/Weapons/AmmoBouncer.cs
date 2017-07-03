using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    public class AmmoBouncer : AmmoBase
    {
        private float targetSpeedX;
        private float hitLimit;

        public override WeaponType WeaponType => WeaponType.Bouncer;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            strength = 1;

            RequestMetadata("Weapon/Bouncer");
        }

        public void OnFire(Player owner, Vector3 speed, float angle, bool isFacingLeft, byte upgrades)
        {
            base.owner = owner;
            base.isFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            const float baseSpeed = 6f;
            if (isFacingLeft) {
                targetSpeedX = speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                targetSpeedX = speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;
            speedY += MathF.Abs(speed.Y) * speedY;

            elasticity = 0.9f;

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 130;
                state |= (AnimState)1;
                PlaySound("FireUpgraded");
            } else {
                timeLeft = 90;
                PlaySound("Fire");
            }

            SetAnimation(state);

            OnUpdateHitbox();
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            CheckCollisions();
            TryStandardMovement();

            base.OnUpdate();

            if (hitLimit > 0f) {
                hitLimit -= Time.TimeMult;
            }

            if ((upgrades & 0x1) != 0 && targetSpeedX != 0f) {
                if (speedX != targetSpeedX) {
                    float step = Time.TimeMult * 0.2f;
                    if (MathF.Abs(speedX - targetSpeedX) < step) {
                        speedX = targetSpeedX;
                    } else {
                        speedX += step * ((targetSpeedX < speedX) ? -1 : 1);
                    }
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(api, Transform.Pos + Speed, Explosion.SmallDark);

            return base.OnPerish(collider);
        }

        protected override void OnHitWallHook()
        {
            if (hitLimit > 3f) {
                DecreaseHealth(int.MaxValue);
                return;
            }

            hitLimit += 2f;
            PlaySound("Bounce", 0.5f);
        }

        protected override void OnHitFloorHook()
        {
            if (hitLimit > 3f) {
                DecreaseHealth(int.MaxValue);
                return;
            }

            hitLimit += 2f;
            PlaySound("Bounce", 0.5f);
        }

        protected override void OnHitCeilingHook()
        {
            if (hitLimit > 3f) {
                DecreaseHealth(int.MaxValue);
                return;
            }

            hitLimit += 2f;
            PlaySound("Bounce", 0.5f);
        }

        protected override void OnRicochet()
        {
            // Nothing to do...

            speedX = -speedX;
        }
    }
}