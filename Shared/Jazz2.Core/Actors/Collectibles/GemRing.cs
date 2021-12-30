﻿using System.Threading.Tasks;
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

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Collectible/Gems");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new GemRing();
            actor.OnActivated(details);
            return actor;
        }

        private GemRing()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            int length = (details.Params[0] > 0 ? details.Params[0] : 8);
            speed = (details.Params[1] > 0 ? details.Params[1] : 8) * 0.00625f;
            // "Event" parameter will not be implemented

            CollisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.SkipPerPixelCollisions;

            untouched = false;

            await RequestMetadataAsync("Collectible/Gems");

            parts = new GemPart[length];
            for (int i = 0; i < parts.Length; i++) {
                GemPart part = new GemPart();
                part.OnActivated(details);
                part.Parent = this;
                part.Transform.Scale = 0.8f;
                parts[i] = part;
            }
        }

        public override void OnFixedUpdate(float timeMult)
        {
            //base.OnFixedUpdate(timeMult);

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
                    parts[i].Transform.Scale += 0.02f * timeMult;
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
                CollisionFlags &= ~CollisionFlags.CollideWithOtherActors;

                player.AddGems(parts.Length);

                // ToDo: Add correct score value
                //player.AddScore(scoreValue);
            }
        }

        public class GemPart : ActorBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                CollisionFlags = CollisionFlags.ForceDisableCollisions;

                await RequestMetadataAsync("Collectible/Gems");
                SetAnimation("GemRed");
            }

            public override void OnFixedUpdate(float timeMult)
            {
                // Nothing to do...
            }
        }
    }
}