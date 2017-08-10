using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class GemRing : Collectible
    {
        private float speed;
        private GemPart[] parts;
        private float phase;

        private bool collected;
        private float collectedPhase;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            int length = (details.Params[0] > 0 ? details.Params[0] : 8);
            speed = (details.Params[1] > 0 ? details.Params[1] : 8) * 0.00625f;
            // "Event" parameter will not be implemented

            collisionFlags &= ~(CollisionFlags.ApplyGravitation | CollisionFlags.SkipPerPixelCollisions);

            untouched = false;

            // Workaround: Some animation have to be loaded for collision detection
            RequestMetadata("Object/GemGiant");
            SetAnimation(AnimState.Idle);

            renderer.Active = false;

            parts = new GemPart[length];
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
                    float distance = 8 * 4 + collectedPhase * 3.6f;
                    parts[i].Transform.RelativePos = new Vector3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0f);
                    parts[i].Transform.Angle = angle + MathF.PiOver2;
                    parts[i].Transform.Scale += 0.02f;
                }

                phase += Time.TimeMult * speed * 3f;
                collectedPhase += Time.TimeMult;
            } else {
                for (int i = 0; i < parts.Length; i++) {
                    float angle = phase + i * MathF.TwoPi / parts.Length;
                    float distance = 8 * (4 + MathF.Sin(phase * 1.1f));
                    parts[i].Transform.RelativePos = new Vector3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0f);
                    parts[i].Transform.Angle = angle + MathF.PiOver2;
                }

                phase += Time.TimeMult * speed;
            }
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