using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Bosses;
using Jazz2.Game.Components;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    public partial class AmmoBouncer : AmmoBase
    {
        private Vector2 gunspotPos;
        private bool fired;

        private float targetSpeedX;
        private float hitLimit;

        public override WeaponType WeaponType => WeaponType.Bouncer;

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new AmmoBouncer();
            actor.OnActivated(details);
            return actor;
        }

        public AmmoBouncer()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            base.upgrades = (byte)details.Params[0];

            strength = 1;

            await RequestMetadataAsync("Weapon/Bouncer");

            ushort upgrades = details.Params[0];

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 130;
                state |= (AnimState)1;
                PlaySound(Transform.Pos, "FireUpgraded", 1f, 0.5f);
            } else {
                timeLeft = 90;
                PlaySound(Transform.Pos, "Fire", 1f, 0.5f);
            }

            SetAnimation(state);

            renderer.Active = false;

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.Brightness = 0.2f;
            light.RadiusNear = 0f;
            light.RadiusFar = 12f;
        }

        public void OnFire(Player owner, Vector3 gunspotPos, Vector3 speed, float angle, bool isFacingLeft)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;

            this.gunspotPos = gunspotPos.Xy;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            const float baseSpeed = 6f;
            if (isFacingLeft) {
                targetSpeedX = speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                targetSpeedX = speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;

            elasticity = 0.9f;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            TryStandardMovement(timeMult);
            OnUpdateHitbox();
            CheckCollisions(timeMult);

            base.OnUpdate();

            if (hitLimit > 0f) {
                hitLimit -= timeMult;
            }

            if ((upgrades & 0x1) != 0 && targetSpeedX != 0f) {
                if (speedX != targetSpeedX) {
                    float step = timeMult * 0.2f;
                    if (MathF.Abs(speedX - targetSpeedX) < step) {
                        speedX = targetSpeedX;
                    } else {
                        speedX += step * ((targetSpeedX < speedX) ? -1 : 1);
                    }
                }
            }

            if (!fired) {
                fired = true;

                MoveInstantly(gunspotPos, MoveType.Absolute, true);
                renderer.Active = true;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(levelHandler, Transform.Pos + Speed, Explosion.SmallDark);

            return base.OnPerish(collider);
        }

        protected override void OnHitWall()
        {
            if (hitLimit > 3f) {
                DecreaseHealth(int.MaxValue);
                return;
            }

            hitLimit += 2f;
            PlaySound(Transform.Pos, "Bounce", 0.5f);
        }

        protected override void OnHitFloor()
        {
            if (hitLimit > 3f) {
                DecreaseHealth(int.MaxValue);
                return;
            }

            hitLimit += 2f;
            PlaySound(Transform.Pos, "Bounce", 0.5f);
        }

        protected override void OnHitCeiling()
        {
            if (hitLimit > 3f) {
                DecreaseHealth(int.MaxValue);
                return;
            }

            hitLimit += 2f;
            PlaySound(Transform.Pos, "Bounce", 0.5f);
        }

        protected override void OnRicochet()
        {
            speedX = -speedX;
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Queen queen:
                    if (queen.IsInvulnerable) {
                        OnRicochet();
                    } else {
                        base.OnHandleCollision(other);
                    }
                    break;

                default:
                    base.OnHandleCollision(other);
                    break;
            }
        }
    }
}