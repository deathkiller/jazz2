using System.Collections.Generic;
using Duality;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    public class AmmoRF : AmmoBase
    {
        public override WeaponType WeaponType => WeaponType.RF;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            strength = 2;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            RequestMetadata("Weapon/RF");

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.RadiusNear = 4f;
            light.RadiusFar = 9f;
        }

        public void OnFire(Player owner, Vector3 speed, float angle, bool isFacingLeft, byte upgrades)
        {
            base.owner = owner;
            base.isFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            const float baseSpeed = 2.6f;
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;
            speedY += MathF.Abs(speed.Y) * speedY;

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 35;
                state |= (AnimState)1;
            } else {
                timeLeft = 30;
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

            speedX *= 1.06f;
            speedY *= 1.06f;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Vector3 pos = Transform.Pos;

            List<ActorBase> colliders = api.FindCollisionActorsRadius(pos.X, pos.Y, 36);
            for (int i = 0; i < colliders.Count; i++) {
                Player player = colliders[i] as Player;
                if (player != null) {
                    bool pushLeft = (pos.X > player.Transform.Pos.X);
                    player.AddExternalForce(pushLeft ? -4f : 4f, 0f);
                }
            }

            Explosion.Create(api, Transform.Pos + Speed, Explosion.RF);

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
            //base.OnRicochet();

            //Transform.Angle = MathF.Atan2(speedY, speedX);
        }
    }
}