using Duality;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    public class AmmoPepper : AmmoBase
    {
        public override WeaponType WeaponType => WeaponType.Pepper;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            strength = 1;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;
            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Weapon/Pepper");
        }

        public void OnFire(Player owner, Vector3 speed, float angle, bool isFacingLeft, byte upgrades)
        {
            base.owner = owner;
            base.isFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            float baseSpeed = ((upgrades & 0x1) != 0 ? MathF.Rnd.NextFloat(5f, 7.2f) : MathF.Rnd.NextFloat(3f, 7f));
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;
            speedY += MathF.Abs(speed.Y) * speedY;

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = MathF.Rnd.NextFloat(32, 40);
                state |= (AnimState)1;

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 1f;
                light.Brightness = 1f;
                light.RadiusNear = 4f;
                light.RadiusFar = 16f;
            } else {
                timeLeft = MathF.Rnd.NextFloat(26, 36);
            }

            Transform.Angle = angle;

            SetAnimation(state);
            PlaySound("Fire");
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            CheckCollisions();
            TryStandardMovement();

            base.OnUpdate();
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(api, Transform.Pos + Speed, Explosion.Pepper);

            return base.OnPerish(collider);
        }

        protected override void OnHitFloorHook()
        {
            DecreaseHealth(int.MaxValue);
        }

        protected override void OnHitWallHook()
        {
            DecreaseHealth(int.MaxValue);
        }

        protected override void OnHitCeilingHook()
        {
            DecreaseHealth(int.MaxValue);
        }

        protected override void OnRicochet()
        {
            base.OnRicochet();

            Transform.Angle = MathF.Atan2(speedY, speedX);
        }
    }
}