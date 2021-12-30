﻿using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Bosses;
using Jazz2.Game.Collisions;
using Jazz2.Game.Components;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    public partial class AmmoBlaster : AmmoBase
    {
        private Vector2 gunspotPos;
        private bool fired;

        public override WeaponType WeaponType => WeaponType.Blaster;

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new AmmoBlaster();
            actor.OnActivated(details);
            return actor;
        }

        public AmmoBlaster()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            base.upgrades = (byte)details.Params[0];

            CollisionFlags = (CollisionFlags & ~CollisionFlags.ApplyGravitation) | CollisionFlags.SkipPerPixelCollisions;

            await RequestMetadataAsync("Weapon/Blaster");

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 28;
                state |= (AnimState)1;
                strength = 2;
            } else {
                timeLeft = 25;
                strength = 1;
            }

            SetAnimation(state);

            LightEmitter light = AddComponent<LightEmitter>();
            light.Intensity = 0.8f;
            light.Brightness = 0.6f;
            light.RadiusNear = 5f;
            light.RadiusFar = 20f;
        }

        public void OnFire(Player owner, Vector3 gunspotPos, Vector3 speed, float angle, bool isFacingLeft)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;

            this.gunspotPos = gunspotPos.Xy;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            const float baseSpeed = 10f;
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;

            Transform.Angle = angle;

            renderer.Active = false;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            float halfTimeMult = timeMult * 0.5f;

            for (int i = 0; i < 2; i++) {
                TryMovement(halfTimeMult);
                OnUpdateHitbox();
                CheckCollisions(halfTimeMult);
            }

            base.OnFixedUpdate(timeMult);

            if (timeLeft <= 0f) {
                PlaySound(Transform.Pos, "WallPoof");
            }

            if (!fired) {
                fired = true;

                MoveInstantly(gunspotPos, MoveType.Absolute, true);
                renderer.Active = true;
            }
        }

        protected override void OnUpdateHitbox()
        {
            Vector3 pos = Transform.Pos;
            AABBInner = new AABB(
                pos.X - 8,
                pos.Y - 8,
                pos.X + 8,
                pos.Y + 8
            );
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(levelHandler, Transform.Pos + Speed, Explosion.Small);

            return base.OnPerish(collider);
        }

        protected override void OnHitWall()
        {
            DecreaseHealth(int.MaxValue);

            PlaySound(Transform.Pos, "WallPoof");
        }

        protected override void OnRicochet()
        {
            base.OnRicochet();

            Transform.Angle = MathF.Atan2(speedY, speedX);

            PlaySound(Transform.Pos, "Ricochet");
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