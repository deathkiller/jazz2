using System;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Weapons;

namespace Jazz2.Actors.Collectibles
{
    public abstract class Collectible : ActorBase
    {
        protected bool untouched;
        protected int scoreValue;

        private float phase;
        private float startingY;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            elasticity = 0.6f;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            Vector3 pos = Transform.Pos;
            phase = ((pos.X / 32) + (pos.Y / 32));

            if ((flags & (ActorInstantiationFlags.IsCreatedFromEventMap | ActorInstantiationFlags.IsFromGenerator)) != 0) {
                untouched = true;
                startingY = pos.Y;
                collisionFlags &= ~CollisionFlags.ApplyGravitation;
            } else {
                untouched = false;
                collisionFlags |= CollisionFlags.ApplyGravitation;
            }

            RequestMetadata("Object/Collectible");
        }

        protected void SetFacingDirection()
        {
            Vector3 pos = Transform.Pos;
            if ((Math.Round(pos.X + pos.Y) / 32) % 2 == 0) {
                isFacingLeft = true;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (untouched) {
                phase += Time.TimeMult * 0.15f;

                float waveOffset = 3f * MathF.Cos((phase * 0.25f) * MathF.Pi) + 1f;
                Vector3 pos = Transform.Pos;
                pos.Y = startingY + waveOffset;
                Transform.Pos = pos;
            }
        }

        public override void HandleCollision(ActorBase other)
        {
            bool impactable = (other is AmmoBase || other is AmmoTNT || other is TurtleShell);

            if (impactable) {
                if (untouched) {
                    Vector3 speed = other.Speed;
                    externalForceX +=  speed.X / 2f * (0.9f + (MathF.Rnd.Next() % 2000) / 10000f);
                    externalForceY += -speed.Y / 4f * (0.9f + (MathF.Rnd.Next() % 2000) / 10000f);

                    untouched = false;
                    collisionFlags |= CollisionFlags.ApplyGravitation;
                }
            }
        }

        public virtual void Collect(Player player)
        {
            player.AddScore(scoreValue);

            Explosion.Create(api, Transform.Pos, Explosion.Generator);

            DecreaseHealth(int.MaxValue);
        }
    }
}