using System.Collections.Generic;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Turtle : EnemyBase
    {
        private float DefaultSpeed = 1f;

        private ushort theme;
        private bool isTurning;
        private bool isWithdrawn;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(1);
            scoreValue = 100;

            theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    RequestMetadata("Enemy/Turtle");
                    break;

                case 1: // Xmas
                    RequestMetadata("Enemy/TurtleXmas");
                    break;
            }

            SetAnimation(AnimState.Walk);

            isFacingLeft = MathF.Rnd.NextBool();
            speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(24, 24);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            if (MathF.Abs(speedX) > float.Epsilon && !CanMoveToPosition(speedX, 0)) {
                SetTransition(AnimState.TransitionWithdraw, false, delegate {
                    HandleTurn(true);
                });
                isTurning = true;
                canHurtPlayer = false;
                speedX = 0;
                PlaySound("ENEMY_TURTLE_WITHDRAW", 0.4f);
            }

            if (!isTurning && !isWithdrawn && !isAttacking) {
                Hitbox hitbox = currentHitbox + new Vector2(speedX * 32, 0);
                if (api.TileMap.IsTileEmpty(ref hitbox, true)) {
                    hitbox = currentHitbox + new Vector2(speedX * 32, 0);

                    List<ActorBase> players = api.GetCollidingPlayers(ref hitbox);
                    for (int i = 0; i < players.Count; i++) {
                        if (!players[i].IsInvulnerable) {
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
            shell.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = Transform.Pos,
                Params = new[] { theme }
            });
            api.AddActor(shell);

            Explosion.Create(api, Transform.Pos, Explosion.SmokeGray);

            return base.OnPerish(collider);
        }

        private void HandleTurn(bool isFirstPhase)
        {
            if (isTurning) {
                if (isFirstPhase) {
                    isFacingLeft = !isFacingLeft;
                    SetTransition(AnimState.TransitionWithdrawEnd, false, delegate {
                       HandleTurn(false);
                    });
                    PlaySound("ENEMY_TURTLE_WITHDRAW_END", 0.4f);
                    isWithdrawn = true;
                } else {
                    canHurtPlayer = true;
                    isWithdrawn = false;
                    isTurning = false;
                    speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
                }
            }
        }

        private void Attack()
        {
            SetTransition(AnimState.TransitionAttack, false, delegate {
                speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
                isAttacking = false;
            });
            speedX = 0;
            isAttacking = true;
            PlaySound("ENEMY_TURTLE_ATTACK");

            // ToDo: Play with timer
            //PlaySound("ENEMY_TURTLE_ATTACK_2");
        }
    }
}