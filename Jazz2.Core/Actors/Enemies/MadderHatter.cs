using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class MadderHatter : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        private float attackTime;
        private bool stuck;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            SetHealthByDifficulty(3);
            scoreValue = 200;

            await RequestMetadataAsync("Enemy/MadderHatter");
            SetAnimation(AnimState.Walk);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1f : 1f) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(30, 30);
        }

        protected override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            if (currentTransitionState == AnimState.Idle) {
                if (attackTime <= 0f) {
                    Vector3 pos = Transform.Pos;

                    List<Player> players = api.Players;
                    for (int i = 0; i < players.Count; i++) {
                        Vector3 newPos = players[i].Transform.Pos;
                        if ((newPos - pos).Length <= 200f) {

                            IsFacingLeft = (newPos.X < pos.X);
                            speedX = 0f;

                            SetAnimation((AnimState)1073741824);
                            SetTransition((AnimState)1073741824, false, delegate {
                                PlaySound("Spit");

                                BulletSpit bullet = new BulletSpit();
                                bullet.OnActivated(new ActorActivationDetails {
                                    Api = api,
                                    Pos = new Vector3(pos.X + (IsFacingLeft ? -42f : 42f), pos.Y - 6f, pos.Z - 2f),
                                    Params = new[] { (ushort)(IsFacingLeft ? 1 : 0) }
                                });
                                api.AddActor(bullet);

                                SetAnimation(AnimState.Walk);
                                SetTransition((AnimState)1073741825, false, delegate {
                                    attackTime = MathF.Rnd.NextFloat(120, 160);

                                    speedX = (IsFacingLeft ? -1f : 1f) * DefaultSpeed;
                                });
                            });
                            break;
                        }
                    }
                } else {
                    attackTime -= timeMult;
                }

                if (canJump) {
                    if (!CanMoveToPosition(speedX * 4, 0)) {
                        if (stuck) {
                            MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                        } else {
                            IsFacingLeft ^= true;
                            speedX = (IsFacingLeft ? -1f : 1f) * DefaultSpeed;
                            stuck = true;
                        }
                    } else {
                        stuck = false;
                    }
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            CreateSpriteDebris("Cup", 1);
            CreateSpriteDebris("Hat", 1);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        private class BulletSpit : EnemyBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                base.canBeFrozen = false;
                base.canCollideWithAmmo = false;
                base.isInvulnerable = true;

                IsFacingLeft = (details.Params[0] != 0);
                speedX = (IsFacingLeft ? -6f : 6f);
                externalForceY = 0.6f;

                health = int.MaxValue;

                await RequestMetadataAsync("Enemy/MadderHatter");
                SetAnimation((AnimState)1073741826);
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(8, 8);
            }

            public override void OnHandleCollision(ActorBase other)
            {
            }

            protected override void OnHitFloor()
            {
                DecreaseHealth(int.MaxValue);
            }

            protected override void OnHitWall()
            {
                DecreaseHealth(int.MaxValue);
            }

            protected override void OnHitCeiling()
            {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}