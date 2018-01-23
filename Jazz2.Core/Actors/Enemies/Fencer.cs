using System.Collections.Generic;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Fencer : EnemyBase
    {
        private double stateTime;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(3);
            scoreValue = 400;

            RequestMetadata("Enemy/Fencer");
            SetAnimation(AnimState.Idle);

            IsFacingLeft = true;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            Vector3 targetPos;
            if (FindNearestPlayer(out targetPos)) {
                IsFacingLeft = (Transform.Pos.X > targetPos.X);

                if (stateTime <= 0f) {
                    if (MathF.Rnd.NextFloat() < 0.3f) {
                        speedX = (IsFacingLeft ? -1 : 1) * 1.8f;
                    } else {
                        speedX = (IsFacingLeft ? -1 : 1) * -1.2f;
                    }
                    speedY = -4.5f;

                    PlaySound("Attack");

                    SetTransition(AnimState.TransitionAttack, false, delegate {
                        speedX = speedY = 0;
                    });

                    stateTime = MathF.Rnd.NextFloat(180f, 300f);
                } else {
                    stateTime -= Time.TimeMult;
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        private bool FindNearestPlayer(out Vector3 targetPos)
        {
            const float VisionDistance = 100f;

            Vector3 pos = Transform.Pos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                if ((pos - targetPos).Length < VisionDistance) {
                    return true;
                }
            }

            targetPos = Vector3.Zero;
            return false;
        }
    }
}