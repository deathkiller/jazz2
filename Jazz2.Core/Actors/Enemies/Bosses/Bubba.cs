
using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Duality.Drawing;
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
        private const int StateDying = 4;

        private int state = StateWaiting;
        private float stateTime;

        private ushort endText;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            endText = details.Params[1];

            SetHealthByDifficulty(93);
            scoreValue = 4000;

            await RequestMetadataAsync("Boss/Bubba");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnBossActivated()
        {
            FollowNearestPlayer();
        }

        protected override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

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
                                PlaySound("Sneeze");

                                SetTransition(AnimState.Shoot, false, delegate {
                                    Vector3 pos = Transform.Pos;
                                    float x = (IsFacingLeft ? -16f : 16f);
                                    float y = -5f;

                                    Fireball fireball = new Fireball();
                                    fireball.OnActivated(new ActorActivationDetails {
                                        Api = api,
                                        Pos = new Vector3(pos.X + x, pos.Y + y, pos.Z + 2f),
                                        Params = new[] { (ushort)(IsFacingLeft ? 1 : 0) }
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

                case StateDying: {
                    float time = (renderer.AnimTime / renderer.AnimDuration);
                    renderer.ColorTint = new ColorRgba(1f, 1f - (time * time * time * time));
                    break;
                }
            }

            stateTime -= timeMult;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(20, 24);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();

            api.PlayCommonSound(Transform.Pos, "Splat");

            api.BroadcastLevelText(endText);

            speedX = 0f;
            speedY = -2f;
            internalForceY = 0f;
            frozenTimeLeft = 0f;

            collisionFlags = CollisionFlags.None;

            state = StateDying;
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

                IsFacingLeft = (targetPos.X < pos.X);

                speedX = (IsFacingLeft ? -1.3f : 1.3f);

                internalForceY = 1.27f;

                PlaySound("Jump");

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

        private class Fireball : EnemyBase
        {
            private float time = 50f;

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                IsFacingLeft = (details.Params[0] != 0);
                speedX = (IsFacingLeft ? -4.8f : 4.8f);

                base.canBeFrozen = false;
                base.canCollideWithAmmo = false;
                base.isInvulnerable = true;
                base.collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors;

                health = int.MaxValue;

                await RequestMetadataAsync("Boss/Bubba");
                SetAnimation((AnimState)1073741834);

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.85f;
                light.Brightness = 0.4f;
                light.RadiusNear = 0f;
                light.RadiusFar = 30f;
            }

            protected override void OnFixedUpdate(float timeMult)
            {
                //base.OnFixedUpdate(timeMult);
                MoveInstantly(new Vector2(speedX * timeMult, speedY * timeMult), MoveType.Relative, true);

                if (time <= 0f) {
                    DecreaseHealth(int.MaxValue);
                } else {
                    time -= timeMult;
                }
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(18, 18);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                Explosion.Create(api, Transform.Pos + Speed, Explosion.RF);

                return base.OnPerish(collider);
            }

            public override void OnHandleCollision(ActorBase other)
            {
                switch (other) {
                    case Player player: {
                        DecreaseHealth(int.MaxValue);
                        break;
                    }
                }
            }
        }
    }
}