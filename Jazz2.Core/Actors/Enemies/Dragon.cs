using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Game;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Dragon : EnemyBase
    {
        private bool attacking;
        private float stateTime;
        private float attackTime;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            SetHealthByDifficulty(1);
            scoreValue = 200;

            await RequestMetadataAsync("Enemy/Dragon");
            SetAnimation(AnimState.Idle);

            IsFacingLeft = MathF.Rnd.NextBool();
        }

        protected override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                if ((pos - targetPos).Length < 220f) {
                    goto PLAYER_IS_CLOSE;
                }
            }

            if (attacking) {
                SetAnimation(AnimState.Idle);
                SetTransition((AnimState)1073741826, false, delegate {
                    attacking = false;
                    stateTime = 80f;
                });
            }

            return;

        PLAYER_IS_CLOSE:
            if (currentTransitionState == AnimState.Idle) {
                if (!attacking) {
                    if (stateTime <= 0f) {
                        bool willFaceLeft = (pos.X > targetPos.X);
                        if (IsFacingLeft != willFaceLeft) {
                            SetTransition(AnimState.TransitionTurn, false, delegate {
                                IsFacingLeft = willFaceLeft;

                                SetAnimation((AnimState)1073741825);
                                SetTransition((AnimState)1073741824, false, delegate {
                                    attacking = true;
                                    stateTime = 30f;
                                });
                            });
                        } else {
                            SetAnimation((AnimState)1073741825);
                            SetTransition((AnimState)1073741824, false, delegate {
                                attacking = true;
                                stateTime = 30f;
                            });
                        }
                    }
                } else {
                    if (stateTime <= 0f) {
                        SetAnimation(AnimState.Idle);
                        SetTransition((AnimState)1073741826, false, delegate {
                            attacking = false;
                            stateTime = 60f;
                        });
                    } else {
                        if (attackTime <= 0f) {
                            Fire fire = new Fire();
                            fire.OnActivated(new ActorActivationDetails {
                                Api = api,
                                Pos = new Vector3(pos.X + (IsFacingLeft ? -14f : 14f), pos.Y - 6f, pos.Z - 2f),
                                Params = new[] { (ushort)(IsFacingLeft ? 1 : 0) }
                            });
                            api.AddActor(fire);

                            attackTime = 10f;
                        } else {
                            attackTime -= timeMult;
                        }
                    }
                }
            }

            stateTime -= timeMult;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(Transform.Pos, "Splat");

            Explosion.Create(api, Transform.Pos, Explosion.Tiny);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        public class Fire : EnemyBase
        {
            private float timeLeft = 60;

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                IsFacingLeft = (details.Params[0] != 0);

                base.canBeFrozen = false;
                base.isInvulnerable = true;
                base.collisionFlags &= ~CollisionFlags.ApplyGravitation;

                await RequestMetadataAsync("Weapon/Toaster");
                SetAnimation("Fire");

                const float baseSpeed = 1.2f;
                if (IsFacingLeft) {
                    speedX = -1f * (baseSpeed + MathF.Rnd.NextFloat(0f, 0.2f));
                } else {
                    speedX = +1f * (baseSpeed + MathF.Rnd.NextFloat(0f, 0.2f));
                }
                speedY += MathF.Rnd.NextFloat(-0.5f, 0.5f);

                timeLeft = 60;

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.85f;
                light.Brightness = 0.4f;
                light.RadiusNear = 0f;
                light.RadiusFar = 30f;
            }

            protected override void OnFixedUpdate(float timeMult)
            {
                base.OnFixedUpdate(timeMult);

                if (timeLeft <= 0f) {
                    DecreaseHealth(int.MaxValue, null);
                } else {
                    timeLeft -= timeMult;
                }
            }
        }
    }
}