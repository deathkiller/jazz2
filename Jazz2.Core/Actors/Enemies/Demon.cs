using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Demon : EnemyBase
    {
        private float attackTime = 80f;
        private bool attacking;
        private bool stuck;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            SetHealthByDifficulty(2);
            scoreValue = 100;

            await RequestMetadataAsync("Enemy/Demon");
            SetAnimation(AnimState.Idle);

            IsFacingLeft = MathF.Rnd.NextBool();
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(28, 26);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            if (currentTransitionState == AnimState.Idle) {
                if (attacking) {
                    if (attackTime <= 0f) {
                        attacking = false;
                        attackTime = MathF.Rnd.NextFloat(60, 90);

                        speedX = 0f;

                        SetAnimation(AnimState.Idle);
                        SetTransition((AnimState)1073741826, false);
                    } else if (canJump) {
                        if (!CanMoveToPosition(speedX * 4, 0)) {
                            if (stuck) {
                                MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                            } else {
                                IsFacingLeft ^= true;
                                speedX = (IsFacingLeft ? -1.8f : 1.8f);
                                stuck = true;
                            }
                        } else {
                            stuck = false;
                        }

                        attackTime -= Time.TimeMult;
                    }
                } else {
                    if (attackTime <= 0f) {
                        Vector3 pos = Transform.Pos;

                        List<Player> players = api.Players;
                        for (int i = 0; i < players.Count; i++) {
                            Vector3 newPos = players[i].Transform.Pos;
                            if ((newPos - pos).Length <= 300f) {
                                attacking = true;
                                attackTime = MathF.Rnd.NextFloat(130, 180);

                                IsFacingLeft = (newPos.X < pos.X);
                                speedX = (IsFacingLeft ? -1.8f : 1.8f);

                                SetAnimation((AnimState)1073741824);
                                SetTransition((AnimState)1073741825, false);
                                break;
                            }
                        }
                    } else {
                        attackTime -= Time.TimeMult;
                    }
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
    }
}