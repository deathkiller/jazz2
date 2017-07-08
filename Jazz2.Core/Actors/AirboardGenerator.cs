using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    public class AirboardGenerator : ActorBase
    {
        private ushort delay;

        private float timeLeft;
        private bool active;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            delay = details.Params[0];

            RequestMetadata("Object/Airboard");
            SetAnimation(AnimState.Idle);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            if (delay > 0) {
                renderer.Active = false;

                timeLeft = delay * Time.FramesPerSecond;
            } else {
                active = true;
            }
        }

        protected override void OnUpdate()
        {
            //base.OnUpdate();

            if (!active) {
                if (timeLeft <= 0f) {
                    active = true;
                    renderer.Active = true;
                }

                timeLeft -= Time.TimeMult;
            }
        }

        public bool Activate()
        {
            if (active) {
                active = false;
                renderer.Active = false;

                timeLeft = delay * Time.FramesPerSecond;

                Explosion.Create(api, Transform.Pos, Explosion.Generator);
                return true;
            }

            return false;
        }
    }
}