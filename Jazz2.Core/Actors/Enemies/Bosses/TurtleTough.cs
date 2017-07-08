using System.Collections.Generic;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class TurtleTough : BossBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateWalking1 = 1;
        private const int StateWalking2 = 2;
        private const int StateAttacking = 3;

        private int state = StateWaiting;
        private float stateTime;

        private ushort endText;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            endText = details.Params[1];

            SetHealthByDifficulty(100);
            scoreValue = 5000;

            isFacingLeft = true;

            RequestMetadata("Boss/TurtleTough");
            SetAnimation(AnimState.Idle);
        }

        public override void OnBossActivated()
        {
            FollowNearestPlayer(StateWalking1, MathF.Rnd.NextFloat(120, 160));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            switch (state) {
                case StateWalking1: {
                    if (stateTime <= 0f) {
                        FollowNearestPlayer(StateWalking2, 16);
                    } else if (!CanMoveToPosition(speedX, 0)) {
                        isFacingLeft ^= true;
                        speedX = -speedX;
                    }
                    break;
                }

                case StateWalking2: {
                    if (stateTime <= 0f) {
                        speedX = 0f;

                        PlaySound("AttackStart");

                        state = StateTransition;
                        SetAnimation(AnimState.Idle);
                        SetTransition((AnimState)1073741824, false, delegate {
                            Mace mace = new Mace();
                            mace.OnAttach(new ActorInstantiationDetails {
                                Api = api,
                                Pos = Transform.Pos
                            });
                            api.AddActor(mace);

                            SetTransition((AnimState)1073741825, false, delegate {
                                state = StateAttacking;
                                stateTime = 10f;
                            });
                        });
                    } else if (!CanMoveToPosition(speedX, 0)) {
                        speedX = 0;

                        SetAnimation(AnimState.Idle);
                    }
                    break;
                }

                case StateAttacking: {
                    if (stateTime <= 0f) {
                        List<ActorBase> collisions = api.FindCollisionActors(this);
                        for (int i = 0; i < collisions.Count; i++) {
                            Mace mace = collisions[i] as Mace;
                            if (mace != null) {
                                mace.DecreaseHealth(int.MaxValue);

                                PlaySound("AttackEnd");

                                SetTransition((AnimState)1073741826, false, delegate {
                                    FollowNearestPlayer(StateWalking1, MathF.Rnd.NextFloat(80, 160));
                                });
                            }
                        }
                    }

                    break;
                }

            }

            stateTime -= Time.TimeMult;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();
            CreateSpriteDebris("Shell", 1);

            api.PlayCommonSound(this, "Splat");

            api.BroadcastLevelText(endText);

            return base.OnPerish(collider);
        }

        private void FollowNearestPlayer(int newState, float time)
        {
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
                state = newState;
                stateTime = time;

                isFacingLeft = (targetPos.X < pos.X);

                speedX = (isFacingLeft ? -1.6f : 1.6f);

                SetAnimation(AnimState.Walk);
            }
        }

        public class Mace : EnemyBase
        {
            private const float TotalTime = 60f;

            private Vector3 originPos;
            private Vector3 targetPos;
            private bool returning;
            private float returnTime;

            private Vector3 targetSpeed;

            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                canBeFrozen = false;
                collisionFlags = CollisionFlags.CollideWithOtherActors;

                health = int.MaxValue;

                RequestMetadata("Boss/TurtleTough");
                SetAnimation((AnimState)1073741827);

                originPos = details.Pos;

                FollowNearestPlayer();

                OnUpdateHitbox();
            }

            protected override void OnUpdate()
            {
                //base.OnUpdate();
                MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);

                Vector3 pos = Transform.Pos;

                if (returning) {
                    Vector3 diff = (targetSpeed - Speed);
                    if (diff.LengthSquared > 1f) {
                        speedX += diff.X * 0.04f;
                        speedY += diff.Y * 0.04f;
                    }

                } else {
                    if (returnTime > 0f) {
                        returnTime -= Time.TimeMult;
                    } else {
                        returning = true;

                        targetSpeed = (originPos - pos) / (TotalTime / 2);
                    }
                }
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(18, 18);
            }

            private void FollowNearestPlayer()
            {
                bool found = false;
                targetPos = new Vector3(float.MaxValue, float.MaxValue, originPos.Z);

                List<Player> players = api.Players;
                for (int i = 0; i < players.Count; i++) {
                    Vector3 newPos = players[i].Transform.Pos;
                    if ((originPos - newPos).Length < (originPos - targetPos).Length) {
                        targetPos = newPos;
                        found = true;
                    }
                }

                if (found) {
                    isFacingLeft = (targetPos.X < originPos.X);
                    RefreshFlipMode();

                    returnTime = (TotalTime / 2);

                    Vector3 diff = (targetPos - originPos);
                    speedX = (diff.X / returnTime);
                    speedY = (diff.Y / returnTime);
                }
            }

            public override void HandleCollision(ActorBase other)
            {
            }
        }
    }
}