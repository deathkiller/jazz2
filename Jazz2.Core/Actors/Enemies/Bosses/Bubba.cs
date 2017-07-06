
using System.Collections.Generic;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    // ToDo: Implement sounds

    public class Bubba : BossBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateJumping = 1;
        private const int StateFalling = 2;
        private const int StateTornado = 3;

        private int state = StateWaiting;
        private float stateTime;

        private ushort endText;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            endText = details.Params[1];

            SetHealthByDifficulty(93);
            scoreValue = 4000;

            RequestMetadata("Boss/Bubba");
            SetAnimation(AnimState.Idle);
        }

        public override void OnBossActivated()
        {
            FollowNearestPlayer();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            switch (state) {
                case StateJumping: {
                        if (speedY > 0f) {
                            state = StateFalling;
                            SetAnimation(AnimState.Fall);
                        }
                        break;
                    }

                case StateFalling: {
                        if (canJump) {
                            speedY = 0f;
                            speedX = 0f;

                            state = StateTransition;
                            SetTransition(AnimState.TransitionFallToIdle, false, delegate {
                                float rand = MathF.Rnd.NextFloat();
                                bool spewFileball = (rand < 0.35f);
                                bool tornado = (rand < 0.65f);
                                if (spewFileball) {
                                    PlaySound("SNEEZE");

                                    SetTransition(AnimState.Shoot, false, delegate {
                                        Vector3 pos = Transform.Pos;
                                        float x = (isFacingLeft ? -16f : 16f);
                                        float y = -5f;

                                        BubbaFireball fireball = new BubbaFireball();
                                        fireball.OnAttach(new ActorInstantiationDetails {
                                            Api = api,
                                            Pos = new Vector3(pos.X + x, pos.Y + y, pos.Z + 2f),
                                            Params = new[] { (ushort)(isFacingLeft ? 1 : 0) }
                                        });
                                        api.AddActor(fireball);

                                        SetTransition(AnimState.TransitionShootToIdle, false, delegate {
                                            FollowNearestPlayer();
                                        });
                                    });
                                } else if (tornado) {
                                    TornadoToNearestPlayer();
                                } else {
                                    FollowNearestPlayer();
                                }
                            });
                            
                        }
                        break;
                    }

                case StateTornado: {
                        if (stateTime <= 0f) {
                            state = StateTransition;
                            SetTransition((AnimState)1073741832, false, delegate {
                                collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation;

                                state = StateFalling;

                                SetAnimation((AnimState)1073741833);
                            });
                        }

                        break;
                    }
            }

            stateTime -= Time.TimeMult;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(20, 24);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();

            api.PlayCommonSound(this, "COMMON_SPLAT");

            api.BroadcastLevelText(endText);

            speedX = 0f;
            speedY = -2f;

            collisionFlags = CollisionFlags.None;

            state = StateTransition;
            SetTransition(AnimState.TransitionDeath, false, delegate {
                base.OnPerish(collider);
            });

            return false;
        }

        private void FollowNearestPlayer()
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
                state = StateJumping;
                stateTime = 26;

                isFacingLeft = (targetPos.X < pos.X);

                speedX = (isFacingLeft ? -1.3f : 1.3f);

                internalForceY = 1.27f;

                PlaySound("JUMP");

                SetTransition((AnimState)1073741825, false);
                SetAnimation(AnimState.Jump);
            }
        }

        private void TornadoToNearestPlayer()
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
                state = StateTornado;
                stateTime = 60f;

                collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors;

                MoveInstantly(new Vector2(0, -1), MoveType.Relative);

                Vector3 diff = (targetPos - pos);

                SetTransition((AnimState)1073741830, false, delegate {
                    speedX = (diff.X / stateTime);
                    speedY = (diff.Y / stateTime);

                    SetAnimation((AnimState)1073741831);
                });
            }
        }

        private class BubbaFireball : EnemyBase
        {
            private float time = 50f;

            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                isFacingLeft = (details.Params[0] != 0);
                speedX = (isFacingLeft ? -4.8f : 4.8f);

                collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors;

                health = int.MaxValue;

                RequestMetadata("Boss/Bubba");
                SetAnimation((AnimState)1073741834);

                OnUpdateHitbox();
                RefreshFlipMode();

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.85f;
                light.RadiusNear = 0f;
                light.RadiusFar = 30f;
            }

            protected override void OnUpdate()
            {
                //base.OnUpdate();
                MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);

                if (time <= 0f) {
                    DecreaseHealth(int.MaxValue);
                } else {
                    time -= Time.TimeMult;
                }
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(18, 18);
            }

            public override void HandleCollision(ActorBase other)
            {
            }
        }
    }
}