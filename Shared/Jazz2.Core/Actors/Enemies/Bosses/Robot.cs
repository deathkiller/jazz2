﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game.Components;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class Robot : EnemyBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateCopter = 1;
        private const int StateRunning1 = 2;
        private const int StateRunning2 = 3;
        private const int StatePreparingToRun = 4;

        private int state = StateWaiting;
        private float stateTime;
        private int shots;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Boss/Robot");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Robot();
            actor.OnActivated(details);
            return actor;
        }

        private Robot()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            scoreValue = 2000;

            await RequestMetadataAsync("Boss/Robot");
            SetAnimation(AnimState.Idle);

            renderer.Active = false;

            IsFacingLeft = true;
        }

        public void Activate()
        {
            SetHealthByDifficulty(100);

            renderer.Active = true;
            state = StateCopter;

            MoveInstantly(new Vector2(0f, -300f), MoveType.Relative);

            SetAnimation((AnimState)1073741828);
        }

        public void Deactivate()
        {
            base.OnTileDeactivate(int.MinValue, int.MinValue, int.MinValue, int.MinValue);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            switch (state) {
                case StateCopter: {
                    if (canJump) {
                        speedY = 0f;

                        state = StateTransition;
                        SetTransition((AnimState)1073741829, false, delegate {
                            FollowNearestPlayer(StateRunning1, MathF.Rnd.NextFloat(20, 40));
                        });
                    } else {
                        speedY -= 0.27f * timeMult;
                    }
                    break;
                }
                case StateRunning1: {
                    if (stateTime <= 0f) {
                        FollowNearestPlayer(
                            MathF.Rnd.NextFloat() < 0.65f ? StateRunning1 : StateRunning2,
                            MathF.Rnd.NextFloat(10, 30));
                    }
                    break;
                }

                case StateRunning2: {
                    if (stateTime <= 0f) {
                        speedX = 0f;

                        state = StateTransition;
                        PlaySound("AttackStart");
                        SetAnimation(AnimState.Idle);
                        SetTransition((AnimState)1073741824, false, delegate {
                            shots = MathF.Rnd.Next(1, 4);
                            Shoot();
                        });
                    }
                    break;
                }

                case StatePreparingToRun: {
                    if (stateTime <= 0f) {
                        FollowNearestPlayer(
                            StateRunning1,
                            MathF.Rnd.NextFloat(10, 30));
                    }
                    break;
                }
            }

            stateTime -= timeMult;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();

            string[] shrapnels = new[] {
                "Shrapnel1", "Shrapnel2", "Shrapnel3",
                "Shrapnel4", "Shrapnel5", "Shrapnel6",
                "Shrapnel7", "Shrapnel8", "Shrapnel9"
            };
            for (int i = 0; i < 6; i++) {
                CreateSpriteDebris(MathF.Rnd.OneOf(shrapnels), 1);
            }

            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            return base.OnPerish(collider);
        }

        //public override bool OnTileDeactivate(int tx, int ty, int tileDistance)
        //{
        //    return false;
        //}

        protected override void OnHealthChanged(ActorBase collider)
        {
            base.OnHealthChanged(collider);

            string[] shrapnels = new[] {
                "Shrapnel1", "Shrapnel2", "Shrapnel3",
                "Shrapnel4", "Shrapnel5", "Shrapnel6",
                "Shrapnel7", "Shrapnel8", "Shrapnel9"
            };
            int n = MathF.Rnd.Next(1, 3);
            for (int i = 0; i < n; i++) {
                CreateSpriteDebris(MathF.Rnd.OneOf(shrapnels), 1);
            }

            PlaySound("Shrapnel");
        }

        private void FollowNearestPlayer(int newState, float time)
        {
            bool found = false;
            Vector3 pos = Transform.Pos;
            Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, 0f);

            List<Player> players = levelHandler.Players;
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

                IsFacingLeft = (targetPos.X < pos.X);
                
                float mult = MathF.Rnd.NextFloat(0.6f, 0.9f);
                speedX = (IsFacingLeft ? -3f : 3f) * mult;
                renderer.AnimDuration = currentAnimation.FrameDuration / mult;

                PlaySound("Run");
                SetAnimation(AnimState.Run);
            }
        }

        private void Shoot()
        {
            SpikeBall spikeBall = new SpikeBall();
            spikeBall.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = Transform.Pos + new Vector3(0f, -32f, 0f),
                Params = new[] { (ushort)(IsFacingLeft ? 1 : 0) }
            });
            levelHandler.AddActor(spikeBall);

            shots--;

            PlaySound("Attack");
            SetTransition((AnimState)1073741825, false, delegate {
                if (shots > 0) {
                    PlaySound("AttackShutter");
                    Shoot();
                } else {
                    Run();
                }
            });
        }

        private void Run()
        {
            PlaySound("AttackEnd");
            SetTransition((AnimState)1073741826, false, delegate {
                state = StatePreparingToRun;
                stateTime = 10f;
            });
        }

        private class SpikeBall : EnemyBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                IsFacingLeft = (details.Params[0] != 0);

                canBeFrozen = false;
                health = int.MaxValue;

                speedX = (IsFacingLeft ? -8f : 8f);

                await RequestMetadataAsync("Boss/Robot");
                SetAnimation((AnimState)1073741827);

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.1f;
                light.Brightness = 0.8f;
                light.RadiusNear = 0f;
                light.RadiusFar = 22f;
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(8, 8);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                Explosion.Create(levelHandler, Transform.Pos, Explosion.SmallDark);

                return base.OnPerish(collider);
            }

            public override void OnHandleCollision(ActorBase other)
            {
                // Nothing to do...
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