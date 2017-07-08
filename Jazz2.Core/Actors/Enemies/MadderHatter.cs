using System.Collections.Generic;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class MadderHatter : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        private float attackTime;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(3);
            scoreValue = 200;

            RequestMetadata("Enemy/MadderHatter");
            SetAnimation(AnimState.Walk);

            isFacingLeft = MathF.Rnd.NextBool();
            speedX = (isFacingLeft ? -1f : 1f) * DefaultSpeed;
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

            if (currentTransitionState == AnimState.Idle) {
                if (attackTime <= 0f) {
                    Vector3 pos = Transform.Pos;

                    List<Player> players = api.Players;
                    for (int i = 0; i < players.Count; i++) {
                        Vector3 newPos = players[i].Transform.Pos;
                        if ((newPos - pos).Length <= 200f) {

                            isFacingLeft = (newPos.X < pos.X);
                            speedX = 0f;

                            SetAnimation((AnimState)1073741824);
                            SetTransition((AnimState)1073741824, false, delegate {
                                PlaySound("Spit");

                                BulletSpit bullet = new BulletSpit();
                                bullet.OnAttach(new ActorInstantiationDetails {
                                    Api = api,
                                    Pos = new Vector3(pos.X + (isFacingLeft ? -42f : 42f), pos.Y - 6f, pos.Z - 2f),
                                    Params = new[] { (ushort)(isFacingLeft ? 1 : 0) }
                                });
                                api.AddActor(bullet);

                                SetAnimation(AnimState.Walk);
                                SetTransition((AnimState)1073741825, false, delegate {
                                    attackTime = MathF.Rnd.NextFloat(120, 160);

                                    speedX = (isFacingLeft ? -1f : 1f) * DefaultSpeed;
                                });
                            });
                            break;
                        }
                    }
                } else {
                    attackTime -= Time.TimeMult;
                }

                if (!CanMoveToPosition(speedX, 0)) {
                    isFacingLeft ^= true;
                    speedX = (isFacingLeft ? -1f : 1f) * DefaultSpeed;
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
            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                isFacingLeft = (details.Params[0] != 0);
                speedX = (isFacingLeft ? -6f : 6f);
                externalForceY = 0.6f;

                health = int.MaxValue;

                RequestMetadata("Enemy/MadderHatter");
                SetAnimation((AnimState)1073741826);

                OnUpdateHitbox();
                RefreshFlipMode();
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(8, 8);
            }

            public override void HandleCollision(ActorBase other)
            {
            }

            protected override void OnHitFloorHook()
            {
                DecreaseHealth(int.MaxValue);
            }

            protected override void OnHitWallHook()
            {
                DecreaseHealth(int.MaxValue);
            }

            protected override void OnHitCeilingHook()
            {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}