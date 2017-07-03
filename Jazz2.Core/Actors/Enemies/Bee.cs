using System.Collections.Generic;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Bee : EnemyBase
    {
        private Vector3 originPos, lastPos, targetPos, lastSpeed;
        private float anglePhase;
        private float attackTime = 80f;
        private bool attacking;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            originPos = lastPos = targetPos = details.Pos;

            RequestMetadata("Enemy/Bee");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            HandleBlinking();

            if (frozenTimeLeft > 0) {
                frozenTimeLeft -= Time.TimeMult;
                return;
            }

            if (attackTime > 0f) {
                attackTime -= Time.TimeMult;
            } else {
                if (attacking) {
                    targetPos = originPos;

                    attackTime = 90f;
                    attacking = false;
                } else {
                    AttackNearestPlayer();
                }
            }

            anglePhase += Time.TimeMult * 0.05f;

            Vector3 speed = ((targetPos - lastPos) / 30f + lastSpeed * 1.4f) / 2.4f;
            lastPos.X += speed.X;
            lastPos.Y += speed.Y;
            lastSpeed = speed;

            bool willFaceLeft = (speed.X < 0f);
            if (isFacingLeft != willFaceLeft) {
                isFacingLeft = willFaceLeft;
                SetTransition(AnimState.TransitionTurn, false, delegate {
                    RefreshFlipMode();
                });
            }

            Transform.Pos = lastPos + new Vector3(MathF.Cos(anglePhase) * 16f, MathF.Sin(anglePhase) * -16f, 0f);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();
            api.PlayCommonSound(this, "COMMON_SPLAT");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        private void AttackNearestPlayer()
        {
            bool found = false;
            targetPos = new Vector3(float.MaxValue, float.MaxValue, lastPos.Z);

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((lastPos - newPos).Length < (lastPos - targetPos).Length) {
                    targetPos = newPos;
                    found = true;
                }
            }

            Vector3 diff = (targetPos - lastPos);
            if (found && diff.Length <= 250f) {
                attackTime = 90f;
                attacking = true;

                PlaySound("NOISE", 0.5f);
            } else {
                targetPos = originPos;
            }
        }
    }
}