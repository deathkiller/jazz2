using Duality;
using Jazz2.Actors.Bosses;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    public class AmmoBlaster : AmmoBase
    {
        public override WeaponType WeaponType => WeaponType.Blaster;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            RequestMetadata("Weapon/Blaster");

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.Brightness = 0.6f;
            light.RadiusNear = 5f;
            light.RadiusFar = 20f;
        }

        public void OnFire(Player owner, Vector3 speed, float angle, bool isFacingLeft, byte upgrades)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            const float baseSpeed = 10f;
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;
            speedY += MathF.Abs(speed.Y) * speedY;

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 28;
                state |= (AnimState)1;
                strength = 2;
            } else {
                timeLeft = 25;
                strength = 1;
            }

            Transform.Angle = angle;

            SetAnimation(state);
        }

        protected override void OnUpdate()
        {
            float timeMult = Time.TimeMult * 0.5f;

            for (int i = 0; i < 2; i++) {
                TryMovement(timeMult);
                OnUpdateHitbox();
                CheckCollisions(timeMult);
            }

            base.OnUpdate();

            if (timeLeft <= 0f) {
                PlaySound("WallPoof");
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(api, Transform.Pos + Speed, Explosion.Small);

            return base.OnPerish(collider);
        }

        protected override void OnHitWallHook()
        {
            DecreaseHealth(int.MaxValue);

            PlaySound("WallPoof");
        }

        protected override void OnRicochet()
        {
            base.OnRicochet();

            Transform.Angle = MathF.Atan2(speedY, speedX);

            PlaySound("Ricochet");
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Queen queen:
                    if (queen.IsInvulnerable) {
                        if (lastRicochet != other) {
                            lastRicochet = other;
                            OnRicochet();
                        }
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