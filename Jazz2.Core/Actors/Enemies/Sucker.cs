using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Sucker : EnemyBase
    {
        private int cycle;
        private float cycleTimer;

        private bool stuck;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            LastHitDirection parentLastHitDir = (LastHitDirection)details.Params[0];

            SetHealthByDifficulty(1);
            scoreValue = 100;

            await RequestMetadataAsync("Enemy/Sucker");
            maxHealth = 4;
            SetAnimation(AnimState.Walk);

            if (parentLastHitDir != LastHitDirection.None) {
                IsFacingLeft = (parentLastHitDir == LastHitDirection.Left);
                health = 1;
                collisionFlags &= ~CollisionFlags.ApplyGravitation;
                SetTransition((AnimState)1073741824, false, delegate {
                    speedX = 0;
                    SetAnimation(AnimState.Walk);
                    collisionFlags |= CollisionFlags.ApplyGravitation;
                });
                if (parentLastHitDir == LastHitDirection.Left || parentLastHitDir == LastHitDirection.Right) {
                    speedX = 3 * (parentLastHitDir == LastHitDirection.Left ? -1 : 1);
                }
                PlaySound("Deflate");
            } else {
                health = 4;
            }
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            if (currentTransitionState == AnimState.Idle && MathF.Abs(speedX) > 0 && canJump) {
                if (!CanMoveToPosition(speedX * 4, 0)) {
                    if (stuck) {
                        MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                    } else {
                        IsFacingLeft = !IsFacingLeft;
                        speedX *= -1;
                        stuck = true;
                    }
                } else {
                    stuck = false;
                }
            }

            if (currentTransitionState == AnimState.Idle && frozenTimeLeft <= 0) {
                if (cycleTimer < 0f) {
                    cycle++;
                    if (cycle == 12) {
                        cycle = 0;
                    }

                    if (cycle == 0) {
                        PlaySound("Walk1", 0.5f);
                    } else if (cycle == 6) {
                        PlaySound("Walk2", 0.5f);
                    } else if (cycle == 2 || cycle == 7) {
                        PlaySound("Walk3", 0.5f);
                    }

                    if ((cycle >= 4 && cycle < 7) || cycle >= 9) {
                        speedX = 0.6f * (IsFacingLeft ? -1 : 1);
                    } else {
                        speedX = 0;
                    }

                    cycleTimer = 5f;
                } else {
                    cycleTimer -= timeMult;
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}