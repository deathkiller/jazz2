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

        public override WeaponType WeaponType => WeaponType.Thunderbolt;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

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
            base.isFacingLeft = isFacingLeft;
            base.upgrades = upgrades;

            //float angleRel = angle * (isFacingLeft ? -1 : 1);

            AnimState state = AnimState.Idle;
            if ((upgrades & 0x1) != 0) {
                timeLeft = 188;
                strength = 2;
            } else {
                timeLeft = 144;
                strength = 1;
            }

            if (MathF.Rnd.NextBool()) {
                state |= (AnimState)1;
            }

            //Transform.Angle = angleRel;

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
            Transform.RelativePos = Transform.Pos - owner.Transform.Pos + new Vector3(0, 0, 4);
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            CheckCollisions();
            //TryStandardMovement();

            //base.OnUpdate();

            if (light.Intensity > 0f) {
                light.Intensity -= Time.TimeMult * 0.05f;
            }

            if (light.Brightness > 0f) {
                light.Brightness -= Time.TimeMult * 0.05f;
            }

            if (sound != null && sound.Pitch > 0.9f) {
                sound.Pitch -= Time.TimeMult * 0.04f;
            }
        }

        protected override void OnUpdateHitbox()
        {
            if (currentAnimation == null) {
                return;
            }

            Vector3 pos = Transform.Pos;
            if (isFacingLeft) {
                currentHitbox = new Hitbox(
                    pos.X - currentAnimation.Hotspot.X - currentAnimation.FrameDimensions.X,
                    pos.Y - currentAnimation.Hotspot.Y,
                    pos.X - currentAnimation.Hotspot.X,
                    pos.Y - currentAnimation.Hotspot.Y + currentAnimation.FrameDimensions.Y
                );
            } else {
                currentHitbox = new Hitbox(
                    pos.X - currentAnimation.Hotspot.X,
                    pos.Y - currentAnimation.Hotspot.Y,
                    pos.X - currentAnimation.Hotspot.X + currentAnimation.FrameDimensions.X,
                    pos.Y - currentAnimation.Hotspot.Y + currentAnimation.FrameDimensions.Y
                );
            }
        }

        protected override void OnRicochet()
        {
        }

        protected override void OnAnimationFinished()
        {
            base.OnAnimationFinished();

            DecreaseHealth(int.MaxValue);
        }

        public override void HandleCollision(ActorBase other)
        {
            // Nothing to do...
        }
    }
}