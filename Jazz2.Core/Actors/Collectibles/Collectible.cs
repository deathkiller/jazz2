using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Weapons;

namespace Jazz2.Actors.Collectibles
{
    public abstract class Collectible : ActorBase
    {
        protected bool untouched;
        protected int scoreValue;

        private float phase, timeLeft;
        private float startingY;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            elasticity = 0.6f;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            Vector3 pos = Transform.Pos;
            phase = ((pos.X / 32) + (pos.Y / 32)) * 2f;

            if ((flags & (ActorInstantiationFlags.IsCreatedFromEventMap | ActorInstantiationFlags.IsFromGenerator)) != 0) {
                untouched = true;
                collisionFlags &= ~CollisionFlags.ApplyGravitation;

                startingY = pos.Y;
            } else {
                untouched = false;
                collisionFlags |= CollisionFlags.ApplyGravitation;

                timeLeft = 90f * Time.FramesPerSecond;
            }

            if ((details.Flags & ActorInstantiationFlags.Illuminated) != 0) {
                Illuminate();
            }
        }

        protected void SetFacingDirection()
        {
            Vector3 pos = Transform.Pos;
            if ((((int)(pos.X + pos.Y) / 32) & 1) == 0) {
                IsFacingLeft = true;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (untouched) {
                phase += Time.TimeMult * 0.15f;

                float waveOffset = 3.2f * MathF.Cos((phase * 0.25f) * MathF.Pi) + 0.6f;
                Vector3 pos = Transform.Pos;
                pos.Y = startingY + waveOffset;
                Transform.Pos = pos;
            } else if (timeLeft > 0f) {
                timeLeft -= Time.TimeMult;

                if (timeLeft <= 0f) {
                    Explosion.Create(api, Transform.Pos, Explosion.Generator);

                    DecreaseHealth(int.MaxValue);
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player:
                    Collect(player);
                    break;

                default:
                    if (other is AmmoBase || other is AmmoTNT || other is TurtleShell) {
                        if (untouched) {
                            Vector3 speed = other.Speed;
                            externalForceX +=  speed.X / 2f * (0.9f + MathF.Rnd.NextFloat(0.2f));
                            externalForceY += -speed.Y / 4f * (0.9f + MathF.Rnd.NextFloat(0.2f));

                            untouched = false;
                            collisionFlags |= CollisionFlags.ApplyGravitation;
                        }
                    }
                    break;
            }
        }

        protected virtual void Collect(Player player)
        {
            player.AddScore(scoreValue);

            Explosion.Create(api, Transform.Pos, Explosion.Generator);

            DecreaseHealth(int.MaxValue);
        }
    }
}