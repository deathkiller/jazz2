using Duality;
using Jazz2.Game.Collisions;

namespace Jazz2.Actors.Collectibles
{
    public class GemRing : Collectible
    {
        private float speed;
        private GemPart[] parts;
        private float phase;

        private bool collected;
        private float collectedPhase;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            int length = (details.Params[0] > 0 ? details.Params[0] : 8);
            speed = (details.Params[1] > 0 ? details.Params[1] : 8) * 0.00625f;
            // "Event" parameter will not be implemented

            collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.SkipPerPixelCollisions;

            untouched = false;

            RequestMetadata("Collectible/Gems");

            parts = new GemPart[length];
            for (int i = 0; i < parts.Length; i++) {
                ref GemPart part = ref parts[i];
                part = new GemPart();
                part.OnActivated(details);
                part.Parent = this;
                part.Transform.Scale = 0.8f;
            }

            OnUpdateHitbox();
        }

        protected override void OnUpdate()
        {
            //base.OnUpdate();

            float timeMult = Time.TimeMult;

            if (collected) {
                if (collectedPhase > 100f) {
                    DecreaseHealth(int.MaxValue);
                    return;
                }

                for (int i = 0; i < parts.Length; i++) {
                    float angle = phase * (1f + collectedPhase * 0.001f) + (i * MathF.TwoPi / parts.Length);
                    float distance = 8 * 4 + collectedPhase * 3.6f;
                    parts[i].Transform.RelativePos = new Vector3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0f);
                    parts[i].Transform.Angle = angle + MathF.PiOver2;
                    parts[i].Transform.Scale += 0.02f;
                }

                phase += timeMult * speed * 3f;
                collectedPhase += timeMult;
            } else {
                for (int i = 0; i < parts.Length; i++) {
                    float angle = phase + (i * MathF.TwoPi / parts.Length);
                    float distance = 8 * (4 + MathF.Sin(phase * 1.1f));
                    parts[i].Transform.RelativePos = new Vector3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0f);
                    parts[i].Transform.Angle = angle + MathF.PiOver2;
                }

                phase += timeMult * speed;
            }
        }

        protected override void OnUpdateHitbox()
        {
            Vector3 pos = Transform.Pos;
            AABBInner = new AABB(
                pos.X - 20,
                pos.Y - 20,
                pos.X + 20,
                pos.Y + 20
            );
        }

        protected override void Collect(Player player)
        {
            if (!collected) {
                collected = true;
                collisionFlags &= ~CollisionFlags.CollideWithOtherActors;

                player.AddGems(parts.Length);

                // ToDo: Add correct score value
                //player.AddScore(scoreValue);
            }
        }

        public class GemPart : ActorBase
        {
            public override void OnActivated(ActorActivationDetails details)
            {
                base.OnActivated(details);

                collisionFlags = CollisionFlags.ForceDisableCollisions;

                RequestMetadata("Collectible/Gems");
                SetAnimation("GemRed");
            }

            protected override void OnUpdate()
            {
                // Nothing to do...
            }
        }
    }
}