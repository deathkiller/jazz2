using System.Threading.Tasks;
using Duality;

namespace Jazz2.Actors.Environment
{
    public class AirboardGenerator : ActorBase
    {
        private ushort delay;

        private float timeLeft;
        private bool active;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/Airboard");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new AirboardGenerator();
            actor.OnActivated(details);
            return actor;
        }

        private AirboardGenerator()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            delay = details.Params[0];

            await RequestMetadataAsync("Object/Airboard");
            SetAnimation("Airboard");

            CollisionFlags &= ~CollisionFlags.ApplyGravitation;

            //if (delay > 0) {
            //    renderer.Active = false;
            //
            //    timeLeft = delay * Time.FramesPerSecond;
            //} else {
                active = true;
            //}
        }

        public override void OnFixedUpdate(float timeMult)
        {
            //base.OnFixedUpdate(timeMult);

            if (!active) {
                if (timeLeft <= 0f) {
                    active = true;
                    renderer.Active = true;
                }

                timeLeft -= timeMult;
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player:
                    if (active && player.SetModifier(Player.Modifier.Airboard)) {
                        active = false;
                        renderer.Active = false;

                        timeLeft = delay * Time.FramesPerSecond;

                        Explosion.Create(levelHandler, Transform.Pos, Explosion.Generator);
                    }
                    break;
            }
        }
    }
}