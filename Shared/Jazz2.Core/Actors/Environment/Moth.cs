using System.Threading.Tasks;
using Duality;

namespace Jazz2.Actors.Environment
{
    public class Moth : ActorBase
    {
        private float timer;
        private int direction;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/Moth");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Moth();
            actor.OnActivated(details);
            return actor;
        }

        private Moth()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.Z += 20f;
            Transform.Pos = pos;

            ushort theme = details.Params[0];

            await RequestMetadataAsync("Object/Moth");

            switch (theme) {
                default:
                case 0: SetAnimation("Pink"); break;
                case 1: SetAnimation("Gray"); break;
                case 2: SetAnimation("Green"); break;
                case 3: SetAnimation("Purple"); break;
            }

            renderer.AnimPaused = true;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (timer > 0f) {
                if (canJump) {
                    timer = 0f;
                } else {
                    timer -= timeMult;

                    externalForceX = MathF.Sin((100f - timer) / 6f) * 4f * direction;
                    externalForceY = timer * timer * 0.000046f;

                    IsFacingLeft = (speedX < 0f);
                }
            } else if (canJump) {
                speedX = 0f;
                externalForceY = 0f;
                externalForceX = 0f;

                renderer.AnimTime = 0f;
                renderer.AnimPaused = true;
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player: {
                    if (timer <= 50f) {
                        timer = 100f - timer * 0.2f;

                        canJump = false;

                        direction = (MathF.Rnd.NextBool() ? -1 : 1);
                        speedX = MathF.Rnd.NextFloat(0f, -1.4f) * direction;
                        speedY = MathF.Rnd.NextFloat(0f, -0.4f);

                        renderer.AnimPaused = false;
                    }
                    break;
                }
            }
        }
    }
}