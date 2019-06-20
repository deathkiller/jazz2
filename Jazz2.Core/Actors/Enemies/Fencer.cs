using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Fencer : EnemyBase
    {
        private double stateTime;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            SetHealthByDifficulty(3);
            scoreValue = 400;

            await RequestMetadataAsync("Enemy/Fencer");
            SetAnimation(AnimState.Idle);

            IsFacingLeft = true;
        }

        protected override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

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
                        speedX = 0f;
                        speedY = 0f;
                    });

                    stateTime = MathF.Rnd.NextFloat(180f, 300f);
                } else {
                    stateTime -= timeMult;
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