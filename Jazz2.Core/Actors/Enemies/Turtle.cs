using System;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Collisions;
using Jazz2.Game.Structs;
using MathF = Duality.MathF;

namespace Jazz2.Actors.Enemies
{
    public class Turtle : EnemyBase
    {
        private float DefaultSpeed = 1f;

        private ushort theme;
        private bool isAttacking;
        private bool isTurning;
        private bool isWithdrawn;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            SetHealthByDifficulty(1);
            scoreValue = 100;

            theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    await RequestMetadataAsync("Enemy/Turtle");
                    break;

                case 1: // Xmas
                    await RequestMetadataAsync("Enemy/TurtleXmas");
                    break;
            }

            SetAnimation(AnimState.Walk);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(24, 24);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            if (canJump) {
                if (MathF.Abs(speedX) > float.Epsilon && !CanMoveToPosition(speedX * 4, 0)) {
                    SetTransition(AnimState.TransitionWithdraw, false, delegate {
                        HandleTurn(true);
                    });
                    isTurning = true;
                    canHurtPlayer = false;
                    speedX = 0;
                    PlaySound("Withdraw", 0.4f);
                }
            }

            if (!isTurning && !isWithdrawn && !isAttacking) {
                AABB aabb = AABBInner + new Vector2(speedX * 32, 0);
                if (levelHandler.TileMap.IsTileEmpty(ref aabb, true)) {
                    foreach (Player player in levelHandler.GetCollidingPlayers(aabb + new Vector2(speedX * 32, 0))) {
                        if (!player.IsInvulnerable) {
                            Attack();
                            break;
                        }
                    }
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            TurtleShell shell = new TurtleShell(speedX * 1.1f, 1.1f);
            shell.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = Transform.Pos,
                Params = new[] { theme }
            });
            levelHandler.AddActor(shell);

            Explosion.Create(levelHandler, Transform.Pos, Explosion.SmokeGray);

            return base.OnPerish(collider);
        }

        private void HandleTurn(bool isFirstPhase)
        {
            if (isTurning) {
                if (isFirstPhase) {
                    IsFacingLeft = !IsFacingLeft;
                    SetTransition(AnimState.TransitionWithdrawEnd, false, delegate {
                       HandleTurn(false);
                    });
                    PlaySound("WithdrawEnd", 0.4f);
                    isWithdrawn = true;
                } else {
                    canHurtPlayer = true;
                    isWithdrawn = false;
                    isTurning = false;
                    speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
                }
            }
        }

        private void Attack()
        {
            speedX = 0;
            isAttacking = true;
            PlaySound("Attack");

            SetTransition(AnimState.TransitionAttack, false, delegate {
                speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
                isAttacking = false;

                // ToDo: Bad timing
                //PlaySound("Attack2");
            });
        }
    }
}