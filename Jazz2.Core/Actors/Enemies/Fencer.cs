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

            isFacingLeft = true;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                if ((pos - targetPos).Length < 100f) {
                    goto PLAYER_IS_CLOSE;
                }
            }

            return;

        PLAYER_IS_CLOSE:
            isFacingLeft = (pos.X > targetPos.X);

            if (stateTime <= 0f) {
                speedX = (isFacingLeft ? 1 : -1) * 1.2f;
                speedY = -4.5f;

                PlaySound("ATTACK");

                SetTransition(AnimState.TransitionAttack, false, delegate {
                    speedX = speedY = 0;
                });

                stateTime = MathF.Rnd.NextFloat(240f, 300f);
            } else {
                stateTime -= Time.TimeMult;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "COMMON_SPLAT");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}