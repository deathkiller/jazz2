using System.Collections.Generic;
using Duality;
using Duality.Audio;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Monkey : EnemyBase
    {
        private float DefaultSpeed = 1.6f;

        private bool isWalking;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            isWalking = (details.Params[0] != 0);

            SetHealthByDifficulty(3);
            scoreValue = 200;

            RequestMetadata("Enemy/Monkey");

            if (isWalking) {
                SetAnimation(AnimState.Walk);

                isFacingLeft = MathF.Rnd.NextBool();
                speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
            } else {
                SetAnimation(AnimState.Jump);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!isWalking || frozenTimeLeft > 0) {
                return;
            }

            if (MathF.Abs(speedX) > float.Epsilon && !CanMoveToPosition(speedX, 0)) {
                isFacingLeft ^= true;
                speedX = (isFacingLeft ? -1f : 1f) * DefaultSpeed;
            }
        }

        protected override void OnAnimationFinished()
        {
            base.OnAnimationFinished();

            if (currentTransitionState == AnimState.Idle) {
                bool found = false;
                Vector3 pos = Transform.Pos;
                Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, 0f);

                List<Player> players = api.Players;
                for (int i = 0; i < players.Count; i++) {
                    Vector3 newPos = players[i].Transform.Pos;
                    if ((pos - newPos).Length < (pos - targetPos).Length) {
                        targetPos = newPos;
                        found = true;
                    }
                }

                if (found) {
                    Vector3 diff = (targetPos - pos);
                    if (diff.Length < 280f) {
                        if (isWalking) {
                            speedX = 0f;

                            SetTransition((AnimState)1073741825, false, delegate {
                                isFacingLeft = (targetPos.X < pos.X);

                                SetTransition((AnimState)1073741826, false, delegate {
                                    Banana banana = new Banana();
                                    banana.OnAttach(new ActorInstantiationDetails {
                                        Api = api,
                                        Pos = Transform.Pos + new Vector3(isFacingLeft ? -8f : 8f, -8f, 0f),
                                        Params = new[] { (ushort)(isFacingLeft ? 1 : 0) }
                                    });
                                    api.AddActor(banana);

                                    SetTransition((AnimState)1073741827, false, delegate {

                                        SetTransition((AnimState)1073741824, false, delegate {
                                            isFacingLeft = MathF.Rnd.NextBool();
                                            speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;

                                        });
                                    });
                                });
                            });
                        } else {
                            isFacingLeft = (targetPos.X < pos.X);

                            SetTransition((AnimState)1073741826, false, delegate {
                                Banana banana = new Banana();
                                banana.OnAttach(new ActorInstantiationDetails {
                                    Api = api,
                                    Pos = Transform.Pos + new Vector3(isFacingLeft ? -42f : 42f, -8f, 0f),
                                    Params = new[] { (ushort)(isFacingLeft ? 1 : 0) }
                                });
                                api.AddActor(banana);

                                SetTransition((AnimState)1073741827, false);
                            });
                        }
                    }
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "COMMON_SPLAT");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        private class Banana : EnemyBase
        {
            private SoundInstance soundThrow;

            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                isFacingLeft = (details.Params[0] != 0);

                health = int.MaxValue;

                speedX = (isFacingLeft ? -8f : 8f);
                speedY = -3f;

                RequestMetadata("Enemy/Monkey");
                SetAnimation((AnimState)1073741828);

                soundThrow = PlaySound("BANANA_THROW");

                OnUpdateHitbox();
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(4, 4);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                if (soundThrow != null) {
                    soundThrow.Stop();
                    soundThrow = null;
                }

                speedX = speedY = 0f;
                collisionFlags = CollisionFlags.None;

                SetTransition((AnimState)1073741829, false, delegate {
                    base.OnPerish(collider);
                });

                PlaySound("BANANA_SPLAT", 0.6f);

                return false;
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