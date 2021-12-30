﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;
using static Duality.Component;

namespace Jazz2.Actors.Bosses
{
    // ToDo: Implement Crab spawn animation

    public class Uterus : BossBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateOpen = 1;
        private const int StateClosed = 2;

        private int state = StateWaiting;
        private float stateTime;
        private float spawnCrabTime;
        private float anglePhase;
        private Vector3 lastPos;

        private bool hasShield;
        private Shield[] shields;

        private ushort endText;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Boss/Uterus");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Uterus();
            actor.OnActivated(details);
            return actor;
        }

        private Uterus()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            endText = details.Params[1];

            canBeFrozen = false;
            CollisionFlags = (CollisionFlags & ~CollisionFlags.ApplyGravitation) | CollisionFlags.SkipPerPixelCollisions;
            isInvulnerable = true;

            SetHealthByDifficulty(50);
            scoreValue = 3000;

            lastPos = details.Pos;

            await RequestMetadataAsync("Boss/Uterus");
            SetAnimation(AnimState.Idle);

            renderer.Active = false;
        }

        protected override void OnBossActivated()
        {
            renderer.Active = true;

            state = StateOpen;
            stateTime = 240f;

            spawnCrabTime = 500f;

            hasShield = true;
            shields = new Shield[6];
            for (int i = 0; i < shields.Length; i++) {
                shields[i] = new Shield();
                shields[i].Phase = (MathF.TwoPi * i / shields.Length);
                shields[i].OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = lastPos
                });
                levelHandler.AddActor(shields[i]);
            }

            PreloadMetadata("Enemy/Crab");
        }

        public override void OnDestroyed()
        {
            state = StateWaiting;

            if (shields != null) {
                for (int i = 0; i < shields.Length; i++) {
                    levelHandler.RemoveActor(shields[i]);
                }

                shields = null;
            }
        }

        public override void OnFixedUpdate(float timeMult)
        {
            //base.OnFixedUpdate(timeMult);

            OnUpdateHitbox();
            HandleBlinking(timeMult);

            switch (state) {
                case StateOpen: {
                    if (stateTime <= 0f) {
                        PlaySound("Closing");

                        state = StateTransition;
                        SetAnimation((AnimState)1073741825);
                        SetTransition((AnimState)1073741824, false, delegate {
                            state = StateClosed;
                            stateTime = 280f;

                            isInvulnerable = true;
                        });
                    }

                    if (spawnCrabTime <= 0f) {
                        float force = MathF.Rnd.NextFloat(-15f, 15f);

                        Crab crab = new Crab();
                        crab.OnActivated(new ActorActivationDetails {
                            LevelHandler = levelHandler,
                            Pos = Transform.Pos + new Vector3(0f, 0f, 4f)
                        });
                        crab.AddExternalForce(force, 0f);
                        levelHandler.AddActor(crab);

                        spawnCrabTime = (hasShield ? MathF.Rnd.NextFloat(160f, 220f) : MathF.Rnd.NextFloat(120f, 200f));
                    } else {
                        spawnCrabTime -= timeMult;
                    }
                    break;
                }

                case StateClosed: {
                    if (stateTime <= 0f) {
                        PlaySound("Opening");

                        state = StateTransition;
                        SetAnimation(AnimState.Idle);
                        SetTransition((AnimState)1073741826, false, delegate {
                            state = StateOpen;
                            stateTime = 280f;

                            isInvulnerable = hasShield;
                        });
                    }
                    break;
                }
            }

            stateTime -= timeMult;

            if (state != StateWaiting) {
                FollowNearestPlayer();

                anglePhase += timeMult * 0.02f;
                Transform.Angle = MathF.PiOver2 + MathF.Sin(anglePhase) * 0.2f;

                Vector3 pos = lastPos + new Vector3(MathF.Cos(anglePhase) * 60f, MathF.Sin(anglePhase) * 60f, 0f);
                Transform.Pos = pos;

                if (hasShield) {
                    int shieldCount = 0;
                    for (int i = 0; i < shields.Length; i++) {
                        if (shields[i].Scene != null) {
                            if (shields[i].FallTime <= 0f) {
                                shields[i].Transform.Pos = pos + new Vector3(MathF.Cos(anglePhase + shields[i].Phase) * 50f, MathF.Sin(anglePhase + shields[i].Phase) * 50f, -2f);
                                shields[i].Recover(anglePhase + shields[i].Phase);
                            }
                            shieldCount++;
                        }
                    }

                    if (shieldCount == 0) {
                        hasShield = false;
                    }
                }
            }

            OnUpdateHitbox();
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(38, 60);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            levelHandler.BroadcastLevelText(levelHandler.GetLevelText(endText));

            Explosion.Create(levelHandler, Transform.Pos, Explosion.SmokePoof);
            Explosion.Create(levelHandler, Transform.Pos, Explosion.RF);

            return base.OnPerish(collider);
        }

        private void FollowNearestPlayer()
        {
            bool found = false;
            Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, lastPos.Z);

            List<Player> players = levelHandler.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((lastPos - newPos).Length < (lastPos - targetPos).Length) {
                    targetPos = newPos;
                    found = true;
                }
            }

            if (found) {
                targetPos.Y -= 100f;

                Vector3 diff = (targetPos - lastPos).Normalized;
                // ToDo: There is something strange (speedX == speedY == 0)...
                Vector3 speed = (new Vector3(speedX, speedY, 0f) + diff * 0.4f).Normalized;

                float mult = (hasShield ? 0.8f : 2f);
                lastPos.X += speed.X * mult;
                lastPos.Y += speed.Y * mult;
            }
        }

        private class Shield : EnemyBase
        {
            public float Phase;
            public float FallTime;

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                base.canBeFrozen = false;
                base.CollisionFlags = CollisionFlags.CollideWithOtherActors;

                elasticity = 0.3f;

                health = 3;

                await RequestMetadataAsync("Boss/Uterus");
                SetAnimation((AnimState)1073741827);
            }

            public override void OnFixedUpdate(float timeMult)
            {
                if (FallTime > 0f) {
                    base.OnFixedUpdate(timeMult);

                    FallTime -= timeMult;
                } else {
                    HandleBlinking(timeMult);
                }
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(6, 6);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                CreateDeathDebris(collider);
                levelHandler.PlayCommonSound("Splat", Transform.Pos);

                Explosion.Create(levelHandler, Transform.Pos, Explosion.Tiny);

                return base.OnPerish(collider);
            }

            public override void OnHandleCollision(ActorBase other)
            {
                AmmoBase ammo = other as AmmoBase;
                if (ammo != null) {
                    DecreaseHealth(ammo.Strength, other);

                    FallTime = 400f;

                    CollisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithSolidObjects | CollisionFlags.ApplyGravitation;
                }
            }

            public void Recover(float phase)
            {
                CollisionFlags = CollisionFlags.CollideWithOtherActors;

                Transform.Angle = phase;
            }
        }
    }
}