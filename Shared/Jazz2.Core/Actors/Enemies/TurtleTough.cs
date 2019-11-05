using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class TurtleTough : EnemyBase
    {
        private const float DefaultSpeed = 1f;

        private bool stuck;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.Y -= 24f;
            Transform.Pos = pos;

            SetHealthByDifficulty(4);
            scoreValue = 500;

            await RequestMetadataAsync("Enemy/TurtleTough");
            SetAnimation(AnimState.Walk);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(30, 40);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            if (canJump) {
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

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            Explosion.Create(levelHandler, Transform.Pos, Explosion.Large);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}