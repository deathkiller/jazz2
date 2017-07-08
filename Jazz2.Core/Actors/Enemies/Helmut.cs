using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Helmut : EnemyBase
    {
        private const float DefaultSpeed = 0.9f;

        private bool idling;
        private double stateTime;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Vector3 pos = Transform.Pos;
            pos.Y -= 6f;
            Transform.Pos = pos;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            RequestMetadata("Enemy/Helmut");
            SetAnimation(AnimState.Walk);

            isFacingLeft = MathF.Rnd.NextBool();
            speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(28, 26);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            if (idling) {
                if (stateTime <= 0f) {
                    idling = false;
                    isFacingLeft = !(isFacingLeft);
                    SetAnimation(AnimState.Walk);
                    speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;

                    stateTime = MathF.Rnd.NextFloat(280f, 360f);
                }
            } else {
                if (stateTime <= 0f) {
                    speedX = 0;
                    idling = true;
                    SetAnimation(AnimState.Idle);

                    stateTime = MathF.Rnd.NextFloat(70f, 190f);
                } else {
                    if (!CanMoveToPosition(speedX, 0)) {
                        isFacingLeft = !(isFacingLeft);
                        speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
                    }
                }
            }

            stateTime -= Time.TimeMult;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}