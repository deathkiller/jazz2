using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Helmut : EnemyBase
    {
        private const float DefaultSpeed = 0.9f;

        private bool idling;
        private double stateTime;
        private bool stuck;

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
                } else if (canJump) {
                    if (!CanMoveToPosition(speedX * 4, 0)) {
                        if (stuck) {
                            MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                        } else {
                            isFacingLeft = !(isFacingLeft);
                            speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
                            stuck = true;
                        }
                    } else {
                        stuck = false;
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