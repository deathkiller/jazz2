using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class GemRing : Collectible
    {
        private GemPart[] parts;
        private float phase;

        private bool collected;
        private float collectedPhase;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~(CollisionFlags.ApplyGravitation | CollisionFlags.SkipPerPixelCollisions);

            untouched = false;

            // Workaround: Some animation have to be loaded for collision detection
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
            //base.OnUpdate();

            if (collected) {
                if (collectedPhase > 100f) {
                    DecreaseHealth(int.MaxValue);
                    return;
                }

                for (int i = 0; i < parts.Length; i++) {
                    float angle = phase + i * MathF.TwoPi / parts.Length;
                    float distance = 32f + collectedPhase * 3.2f;
                    parts[i].Transform.RelativePos = new Vector3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0f);
                    parts[i].Transform.Angle = angle + MathF.PiOver2;
                    parts[i].Transform.Scale += 0.02f;
                }

                phase += Time.TimeMult * 0.16f;
                collectedPhase += Time.TimeMult;
            } else {
                for (int i = 0; i < parts.Length; i++) {
                    float angle = phase + i * MathF.TwoPi / parts.Length;
                    float distance = 32f + MathF.Sin(phase * 1.1f) * 8f;
                    parts[i].Transform.RelativePos = new Vector3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0f);
                    parts[i].Transform.Angle = angle + MathF.PiOver2;
                }

                phase += Time.TimeMult * 0.05f;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            return base.OnPerish(collider);
        }

        protected override void Collect(Player player)
        {
            if (!collected) {
                collected = true;
                collisionFlags &= ~CollisionFlags.CollideWithOtherActors;

                player.AddGems(parts.Length);

                //base.Collect(player);

                // ToDo: Add correct score value
                //player.AddScore(scoreValue);
            }
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