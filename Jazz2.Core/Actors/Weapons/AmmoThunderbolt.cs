using Duality;
using Duality.Audio;
using Duality.Components.Renderers;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Weapons
{
    public class AmmoThunderbolt : AmmoBase
    {
        private LightEmitter light;
        private SoundInstance sound;

        private float noDamageTimeLeft;

        public override WeaponType WeaponType => WeaponType.Thunderbolt;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            health = int.MaxValue;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            RequestMetadata("Weapon/Thunderbolt");

            light = AddComponent<LightEmitter>();
            light.Intensity = 0.4f;
            light.Brightness = 0.7f;
            light.RadiusNear = 0f;
            light.RadiusFar = 120f;
        }

        public void OnFire(Player owner, Vector3 speed, float angle, bool isFacingLeft, byte upgrades)
        {
            base.owner = owner;
            base.IsFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

            AnimState state = AnimState.Idle;
            //if ((upgrades & 0x1) != 0) {
            //    strength = 2;
            //} else {
            //    strength = 1;
            //}

            if (MathF.Rnd.NextBool()) {
                state |= (AnimState)1;
            }

            Transform.Angle = angle;

            SetAnimation(state);

            sound = PlaySound("Fire", 3f); // Original sound is too silent
            if (sound != null) {
                sound.Pitch = MathF.Rnd.NextFloat(1.2f, 2f);
                sound.Lowpass = MathF.Rnd.NextFloat(0.3f, 0.8f);
            }

            if (isFacingLeft) {
                renderer.Flip |= SpriteRenderer.FlipMode.Horizontal;
            }

            if (MathF.Rnd.NextBool()) {
                renderer.Flip |= SpriteRenderer.FlipMode.Vertical;
            }

            Parent = owner;
            Transform.RelativePos = Transform.Pos - owner.Transform.Pos + new Vector3(0f, 0f, 4f);
        }

        protected override void OnUpdate()
        {
            float timeMult = Time.TimeMult;

            OnUpdateHitbox();
            CheckCollisions(timeMult);
            //TryStandardMovement();

            //base.OnUpdate();

            if (light.Intensity > 0f) {
                light.Intensity -= timeMult * 0.05f;
            }

            if (light.Brightness > 0f) {
                light.Brightness -= timeMult * 0.05f;
            }

            if (sound != null && sound.Pitch > 0.9f) {
                sound.Pitch -= timeMult * 0.04f;
            }

            if (strength > 0) {
                strength = 0;

                noDamageTimeLeft = 2f;
            } else {
                if (noDamageTimeLeft > 0f) {
                    noDamageTimeLeft -= timeMult;
                } else {
                    strength = 1;
                }
            }
        }

        protected override void OnUpdateHitbox()
        {
            if (currentAnimation == null) {
                return;
            }

            Matrix4 transform =
                Matrix4.CreateTranslation(new Vector3(-currentAnimation.Base.Hotspot.X, -currentAnimation.Base.Hotspot.Y, 0f));
            if (IsFacingLeft)
                transform *= Matrix4.CreateScale(-1f, 1f, 1f);
            transform *= Matrix4.CreateRotationZ(Transform.Angle) *
                Matrix4.CreateTranslation(Transform.Pos);

            Vector2 tl = Vector2.Transform(Vector2.Zero, transform);
            Vector2 tr = Vector2.Transform(new Vector2(currentAnimation.Base.FrameDimensions.X, 0f), transform);
            Vector2 bl = Vector2.Transform(new Vector2(0f, currentAnimation.Base.FrameDimensions.Y), transform);
            Vector2 br = Vector2.Transform(new Vector2(currentAnimation.Base.FrameDimensions.X, currentAnimation.Base.FrameDimensions.Y), transform);

            float minX = MathF.Min(tl.X, tr.X, bl.X, br.X);
            float minY = MathF.Min(tl.Y, tr.Y, bl.Y, br.Y);
            float maxX = MathF.Max(tl.X, tr.X, bl.X, br.X);
            float maxY = MathF.Max(tl.Y, tr.Y, bl.Y, br.Y);

            currentHitbox = new Hitbox(
                MathF.Floor(minX),
                MathF.Floor(minY),
                MathF.Ceiling(maxX),
                MathF.Ceiling(maxY));
        }

        protected override void OnRicochet()
        {
        }

        protected override void OnAnimationFinished()
        {
            base.OnAnimationFinished();

            DecreaseHealth(int.MaxValue);
        }

        public override void OnHandleCollision(ActorBase other)
        {
            // Nothing to do...
        }
    }
}