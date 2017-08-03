using System.Linq;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class LabRat : EnemyBase
    {
        private const float DefaultSpeed = 1f;

        private bool canAttack = true;
        private bool idling;
        private bool canIdle;

        private double stateTime;
        private double attackTime;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(1);
            scoreValue = 200;

            RequestMetadata("Enemy/LabRat");
            SetAnimation(AnimState.Walk);

            isFacingLeft = MathF.Rnd.NextBool();
            speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;

            stateTime = MathF.Rnd.NextFloat(180, 300);
            attackTime = MathF.Rnd.NextFloat(300, 400);
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(30, 30);
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
                    SetAnimation(AnimState.Walk);
                    speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
                    speedY = 0f;

                    stateTime = MathF.Rnd.NextFloat(420, 540);
                } else {
                    stateTime -= Time.TimeMult;
                }
                return;
            }

            if (!isAttacking) {
                if (!CanMoveToPosition(speedX, 0)) {
                    isFacingLeft = !(isFacingLeft);
                    speedX = (isFacingLeft ? -1f : 1f) * DefaultSpeed;
                }

                if (canAttack) {
                    if (MathF.Abs(speedY) < float.Epsilon) {
                        Hitbox hitbox = currentHitbox.Extend(
                            isFacingLeft ? 128 : 0, 20,
                            isFacingLeft ? 0 : 128, 20
                        );

                        if (api.GetCollidingPlayers(hitbox).Any()) {
                            Attack();
                        }
                    }
                } else {
                    if (attackTime <= 0f) {
                        canAttack = true;
                        attackTime = 180;
                    } else {
                        attackTime -= Time.TimeMult;
                    }
                }

                if (MathF.Rnd.NextFloat() < 0.006f) {
                    PlaySound("Noise", 0.4f);
                }

                if (canIdle) {
                    if (stateTime <= 0f) {
                        speedX = 0;
                        idling = true;
                        SetAnimation(AnimState.Idle);
                        canIdle = false;

                        stateTime = MathF.Rnd.NextFloat(260, 320);

                        // TODO: Play with timer
                        //PlaySound("Idle");
                    }
                } else {
                    if (stateTime <= 0f) {
                        canIdle = true;
                        stateTime = MathF.Rnd.NextFloat(60, 120);
                    }
                }
            } else {
                internalForceY += 0.08f;
            }

            stateTime -= Time.TimeMult;
        }

        private void Attack()
        {
            SetTransition(AnimState.TransitionAttack, false, delegate {
                speedX = (isFacingLeft ? -1f : 1f) * DefaultSpeed;
                isAttacking = false;
                canAttack = false;

                attackTime = 180;
            });

            speedX = (isFacingLeft ? -1f : 1f) * 2f;
            MoveInstantly(new Vector2(0f, -1f), MoveType.Relative);
            speedY = -1;
            internalForceY = 0.5f;
            isAttacking = true;
            canJump = false;

            PlaySound("Attack");
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