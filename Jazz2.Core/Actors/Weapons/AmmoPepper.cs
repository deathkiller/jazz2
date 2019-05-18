using Duality;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    public partial class AmmoPepper : AmmoBase
    {
        private Vector2 gunspotPos;
        private bool fired;

        public override WeaponType WeaponType => WeaponType.Pepper;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            base.upgrades = (byte)details.Params[0];

            strength = 1;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;
            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Weapon/Pepper");

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

            SetAnimation(state);
            PlaySound("Fire");

            renderer.Active = false;
        }

        public void OnFire(Player owner, Vector3 gunspotPos, Vector3 speed, float angle, bool isFacingLeft)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;

            this.gunspotPos = gunspotPos.Xy;

            float angleRel = angle * (isFacingLeft ? -1 : 1);

            float baseSpeed = ((upgrades & 0x1) != 0 ? MathF.Rnd.NextFloat(5f, 7.2f) : MathF.Rnd.NextFloat(3f, 7f));
            if (isFacingLeft) {
                speedX = MathF.Min(0, speed.X) - MathF.Cos(angleRel) * baseSpeed;
            } else {
                speedX = MathF.Max(0, speed.X) + MathF.Cos(angleRel) * baseSpeed;
            }
            speedY = MathF.Sin(angleRel) * baseSpeed;

            Transform.Angle = angle;
        }

        protected override void OnUpdate()
        {
            float timeMult = Time.TimeMult * 0.5f;

            for (int i = 0; i < 2; i++) {
                TryMovement(timeMult);
                OnUpdateHitbox();
                CheckCollisions(timeMult);
            }

            if (!fired) {
                fired = true;

                MoveInstantly(gunspotPos, MoveType.Absolute, true);
                renderer.Active = true;
            }

            base.OnUpdate();
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Explosion.Create(api, Transform.Pos + Speed, Explosion.Pepper);

            return base.OnPerish(collider);
        }

        protected override void OnHitWall()
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