using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Crab : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        private float noiseCooldown = 80f;
        private float stepCooldown = 8f;
        private bool canJumpPrev;
        private bool stuck;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            SetHealthByDifficulty(3);
            scoreValue = 300;

            await RequestMetadataAsync("Enemy/Crab");
            SetAnimation(AnimState.Fall);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;

            canJumpPrev = canJump;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(26, 20);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            if (canJump) {
                if (!canJumpPrev) {
                    canJumpPrev = true;
                    SetAnimation(AnimState.Walk);
                    SetTransition(AnimState.TransitionFallToIdle, false);
                }

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

                if (noiseCooldown <= 0f) {
                    noiseCooldown = MathF.Rnd.NextFloat(60, 160);
                    PlaySound("Noise", 0.4f);
                } else {
                    noiseCooldown -= Time.TimeMult;
                }

                if (stepCooldown <= 0f) {
                    stepCooldown = MathF.Rnd.NextFloat(7, 10);
                    PlaySound("Step", 0.15f);
                } else {
                    stepCooldown -= Time.TimeMult;
                }
            } else {
                if (canJumpPrev) {
                    canJumpPrev = false;
                    SetAnimation(AnimState.Fall);
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            Explosion.Create(api, Transform.Pos, Explosion.Large);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}