using System.Threading.Tasks;
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

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.Y -= 6f;
            Transform.Pos = pos;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            await RequestMetadataAsync("Enemy/Helmut");
            SetAnimation(AnimState.Walk);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(28, 26);
        }

        protected override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            if (idling) {
                if (stateTime <= 0f) {
                    idling = false;
                    IsFacingLeft = !IsFacingLeft;
                    SetAnimation(AnimState.Walk);
                    speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;

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
                            IsFacingLeft = !IsFacingLeft;
                            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
                            stuck = true;
                        }
                    } else {
                        stuck = false;
                    }
                }
            }

            stateTime -= timeMult;
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