using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;
using MathF = Duality.MathF;

namespace Jazz2.Actors.Enemies
{
    public class FatChick : EnemyBase
    {
        private const float DefaultSpeed = 0.9f;

        private bool isAttacking;
        private bool stuck;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.Y -= 18f;
            Transform.Pos = pos;

            SetHealthByDifficulty(3);
            scoreValue = 300;

            await RequestMetadataAsync("Enemy/FatChick");
            SetAnimation(AnimState.Walk);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1f : 1f) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(20, 24);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = levelHandler.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                float length = (pos - targetPos).Length;
                if (length > 20f && length < 60f) {
                    IsFacingLeft = (pos.X > targetPos.X);
                    speedX = (IsFacingLeft ? -1f : 1f) * DefaultSpeed;
                    break;
                }
            }

            if (canJump) {
                if (!CanMoveToPosition(speedX * 4, 0)) {
                    if (stuck) {
                        MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                    } else {
                        IsFacingLeft = !IsFacingLeft;
                        speedX = (IsFacingLeft ? -1f : 1f) * DefaultSpeed;
                        stuck = true;
                    }
                } else {
                    stuck = false;
                }
            }

            if (!isAttacking && levelHandler.GetCollidingPlayers(AABBInner + new Vector2(speedX * 28, 0)).Any()) {
                Attack();
            }
        }

        private void Attack()
        {
            // ToDo: Play sound in the middle of transition
            // ToDo: Apply force in the middle of transition
            PlaySound("Attack", 0.8f, 0.6f);

            SetTransition(AnimState.TransitionAttack, false, delegate {
                speedX = (IsFacingLeft ? -1f : 1f) * DefaultSpeed;
                isAttacking = false;
            });
            speedX = 0f;
            isAttacking = true;
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