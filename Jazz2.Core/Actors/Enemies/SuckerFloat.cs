using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class SuckerFloat : EnemyBase
    {
        private float phase;
        private Vector2 originPos;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            originPos = new Vector2(details.Pos.X, details.Pos.Y);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            RequestMetadata("Enemy/SuckerFloat");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            if (frozenTimeLeft <= 0) {
                phase = (phase + 0.05f) % MathF.TwoPi;
                MoveInstantly(new Vector2(originPos.X + 10 * MathF.Cos(phase), originPos.Y + 10 * MathF.Sin(phase)), MoveType.Absolute, true);

                isFacingLeft = (phase < MathF.PiOver2 || phase > 3 * MathF.PiOver2);
            }

            base.OnUpdate();
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Sucker sucker = new Sucker();
            sucker.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = Transform.Pos,
                Params = new[] { (ushort)lastHitDir }
            });
            api.AddActor(sucker);

            return base.OnPerish(collider);
        }
    }
}