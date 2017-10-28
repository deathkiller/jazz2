using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Crab : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        private float noiseCooldown = 70f;
        private bool canJumpPrev;
        private bool stuck;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(3);
            scoreValue = 300;

            RequestMetadata("Enemy/Crab");
            SetAnimation(AnimState.Fall);

            isFacingLeft = MathF.Rnd.NextBool();
            speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;

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
                        isFacingLeft = !(isFacingLeft);
                        speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
                        stuck = true;
                    }
                } else {
                    stuck = false;
                }
            } else {
                if (canJumpPrev) {
                    canJumpPrev = false;
                    SetAnimation(AnimState.Fall);
                }
            }

            if (noiseCooldown <= 0f) {
                noiseCooldown = MathF.Rnd.NextFloat(60, 100);
                PlaySound("Noise", 0.4f);
            } else {
                noiseCooldown -= Time.TimeMult;
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