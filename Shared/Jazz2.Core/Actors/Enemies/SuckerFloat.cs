using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class SuckerFloat : EnemyBase
    {
        private float phase;
        private Vector2 originPos;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Enemy/SuckerFloat");
            PreloadMetadata("Enemy/Sucker");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new SuckerFloat();
            actor.OnActivated(details);
            return actor;
        }

        private SuckerFloat()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            originPos = new Vector2(details.Pos.X, details.Pos.Y);

            CollisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            await RequestMetadataAsync("Enemy/SuckerFloat");
            SetAnimation(AnimState.Idle);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            if (frozenTimeLeft <= 0) {
                phase = (phase + 0.05f * timeMult) % MathF.TwoPi;
                MoveInstantly(new Vector2(originPos.X + 10 * MathF.Cos(phase), originPos.Y + 10 * MathF.Sin(phase)), MoveType.Absolute, true);

                IsFacingLeft = (phase < MathF.PiOver2 || phase > 3 * MathF.PiOver2);
            }

            base.OnFixedUpdate(timeMult);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Sucker sucker = new Sucker();
            sucker.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = Transform.Pos,
                Params = new[] { (ushort)lastHitDir }
            });
            levelHandler.AddActor(sucker);

            return base.OnPerish(collider);
        }
    }
}