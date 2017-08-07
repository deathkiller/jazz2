using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class GemRing : Collectible
    {
        private GemPart[] parts;
        private float phase;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~(CollisionFlags.ApplyGravitation | CollisionFlags.SkipPerPixelCollisions);

            untouched = false;

            RequestMetadata("Object/GemGiant");
            SetAnimation(AnimState.Idle);

            renderer.Active = false;

            parts = new GemPart[8];
            for (int i = 0; i < parts.Length; i++) {
                parts[i] = new GemPart();
                parts[i].OnAttach(details);
                parts[i].Parent = this;
                parts[i].Transform.Scale = 0.8f;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            for (int i = 0; i < parts.Length; i++) {
                float angle = phase + i * MathF.TwoPi / parts.Length;
                float distance = 32f + MathF.Sin(phase * 1.1f) * 8f;
                parts[i].Transform.RelativePos = new Vector3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0f);
                parts[i].Transform.Angle = angle + MathF.PiOver2;
            }

            phase += Time.TimeMult * 0.05f;
        }

        /*protected override void OnUpdateHitbox()
        {
            Vector3 pos = Transform.Pos;

            currentHitbox = new Hitbox(
                pos.X - 120,
                pos.Y - 120,
                pos.X + 120,
                pos.Y + 120
            );
        }*/

        protected override void Collect(Player player)
        {
            player.AddGems(parts.Length);

            base.Collect(player);
        }

        public class GemPart : ActorBase
        {
            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                collisionFlags = CollisionFlags.None;

                RequestMetadata("Object/Collectible");
                SetAnimation("GemRed");
            }

            protected override void OnUpdate()
            {
                // Nothing to do...
            }
        }
    }
}