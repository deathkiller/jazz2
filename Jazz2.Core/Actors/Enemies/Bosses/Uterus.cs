using System.Collections.Generic;
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

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            endText = details.Params[1];

            canBeFrozen = false;
            collisionFlags = (collisionFlags & ~CollisionFlags.ApplyGravitation) | CollisionFlags.SkipPerPixelCollisions;
            isInvulnerable = true;

            SetHealthByDifficulty(50);
            scoreValue = 3000;

            lastPos = details.Pos;

            RequestMetadata("Boss/Uterus");
            SetAnimation(AnimState.Idle);

            renderer.Active = false;
        }

        public override void OnBossActivated()
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
                    Api = api,
                    Pos = lastPos
                });
                api.AddActor(shields[i]);
            }

            PreloadMetadata("Enemy/Crab");
        }

        protected override void OnDeactivated(ShutdownContext context)
        {
            state = StateWaiting;

            if (shields != null) {
                for (int i = 0; i < shields.Length; i++) {
                    api.RemoveActor(shields[i]);
                }

                shields = null;
            }
        }

        protected override void OnUpdate()
        {
            //base.OnUpdate();

            OnUpdateHitbox();
            HandleBlinking();

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
                            Api = api,
                            Pos = Transform.Pos + new Vector3(0f, 0f, 4f)
                        });
                        crab.AddExternalForce(force, 0f);
                        api.AddActor(crab);

                        spawnCrabTime = (hasShield ? MathF.Rnd.NextFloat(160f, 220f) : MathF.Rnd.NextFloat(120f, 200f));
                    } else {
                        spawnCrabTime -= Time.TimeMult;
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

            stateTime -= Time.TimeMult;

            if (state != StateWaiting) {
                FollowNearestPlayer();

                anglePhase += Time.TimeMult * 0.02f;
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
            api.PlayCommonSound(this, "Splat");

            api.BroadcastLevelText(endText);

            Explosion.Create(api, Transform.Pos, Explosion.SmokePoof);
            Explosion.Create(api, Transform.Pos, Explosion.RF);

            return base.OnPerish(collider);
        }

        private void FollowNearestPlayer()
        {
            bool found = false;
            Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, lastPos.Z);

            List<Player> players = api.Players;
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

            public override void OnActivated(ActorActivationDetails details)
            {
                base.OnActivated(details);

                canBeFrozen = false;
                collisionFlags = CollisionFlags.CollideWithOtherActors;

                elasticity = 0.3f;

                health = 3;

                RequestMetadata("Boss/Uterus");
                SetAnimation((AnimState)1073741827);

                OnUpdateHitbox();
            }

            protected override void OnUpdate()
            {
                if (FallTime > 0f) {
                    base.OnUpdate();

                    FallTime -= Time.TimeMult;
                } else {
                    HandleBlinking();
                }
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(6, 6);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                CreateDeathDebris(collider);
                api.PlayCommonSound(this, "Splat");

                Explosion.Create(api, Transform.Pos, Explosion.Tiny);

                return base.OnPerish(collider);
            }

            public override void OnHandleCollision(ActorBase other)
            {
                AmmoBase ammo = other as AmmoBase;
                if (ammo != null) {
                    DecreaseHealth(ammo.Strength, other);

                    FallTime = 400f;

                    collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithSolidObjects | CollisionFlags.ApplyGravitation;
                }
            }

            public void Recover(float phase)
            {
                collisionFlags = CollisionFlags.CollideWithOtherActors;

                Transform.Angle = phase;
            }
        }
    }
}