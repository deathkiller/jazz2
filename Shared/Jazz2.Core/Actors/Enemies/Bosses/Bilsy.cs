﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game.Collisions;
using Jazz2.Game.Components;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class Bilsy : BossBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateWaiting2 = 1;

        private int state = StateWaiting;
        private float stateTime;
        private Vector3 originPos;

        private ushort theme, endText;

        public static void Preload(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    PreloadMetadata("Boss/Bilsy");
                    break;

                case 1: // Xmas
                    PreloadMetadata("Boss/BilsyXmas");
                    break;
            }
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Bilsy();
            actor.OnActivated(details);
            return actor;
        }

        private Bilsy()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            theme = details.Params[0];
            endText = details.Params[1];

            SetHealthByDifficulty(120);
            scoreValue = 3000;

            CollisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors;

            originPos = details.Pos;

            switch (theme)
            {
                case 0:
                default:
                    await RequestMetadataAsync("Boss/Bilsy");
                    break;

                case 1: // Xmas
                    await RequestMetadataAsync("Boss/BilsyXmas");
                    break;
            }

            SetAnimation(AnimState.Idle);

            renderer.Active = false;
        }

        protected override void OnBossActivated()
        {
            Teleport();
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            switch (state) {
                case StateWaiting: {
                    if (stateTime <= 0f) {
                        state = StateTransition;
                        SetTransition((AnimState)1073741826, false, delegate {
                            PlaySound("ThrowFireball");

                            Vector3 pos = Transform.Pos;

                            Fireball fireball = new Fireball();
                            fireball.OnActivated(new ActorActivationDetails {
                                LevelHandler = levelHandler,
                                Pos = new Vector3(pos.X + 26f * (IsFacingLeft ? -1f : 1f), pos.Y - 20f, pos.Z - 2f),
                                Params = new[] { theme, (ushort)(IsFacingLeft ? 1 : 0) }
                            });
                            levelHandler.AddActor(fireball);

                            SetTransition((AnimState)1073741827, false, delegate {
                                state = StateWaiting2;
                                stateTime = 30f;
                            });
                        });
                    }
                    break;
                }

                case StateWaiting2: {
                    if (stateTime <= 0f) {
                        canBeFrozen = false;

                        PlaySound("Disappear", 0.8f);

                        state = StateTransition;
                        SetTransition((AnimState)1073741825, false, delegate {
                            Teleport();
                        });
                    }
                    break;
                }
            }

            stateTime -= timeMult;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(20, 60);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);

            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            levelHandler.BroadcastLevelText(levelHandler.GetLevelText(endText));

            return base.OnPerish(collider);
        }

        private void Teleport()
        {
            Vector3 pos = Transform.Pos;
            for (int i = 0; i < 20; i++) {
                pos = new Vector3(originPos.X + MathF.Rnd.NextFloat(-320f, 320f), originPos.Y + MathF.Rnd.NextFloat(-240f, 240f), originPos.Z);
                AABB aabb = new AABB(pos.X - 30, pos.Y - 40, pos.X + 30, pos.Y + 40);
                if (levelHandler.IsPositionEmpty(this, ref aabb, true)) {
                    Transform.Pos = pos;
                    break;
                }
            }

            OnUpdateHitbox();

            int j = 60;
            while (j-- > 0 && MoveInstantly(new Vector2(0f, 4f), MoveType.Relative)) {
                // Nothing to do...
            }
            while (j-- > 0 && MoveInstantly(new Vector2(0f, 1f), MoveType.Relative)) {
                // Nothing to do...
            }

            bool found = false;
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
                IsFacingLeft = (targetPos.X < pos.X);
            }

            renderer.Active = true;

            state = StateTransition;
            SetTransition((AnimState)1073741824, false, delegate {
                canBeFrozen = true;

                state = StateWaiting;
                stateTime = 30f;
            });

            PlaySound("Appear", 0.8f);
        }

        private class Fireball : EnemyBase
        {
            private float time = 90f;

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                ushort theme = details.Params[0];
                bool isFacingLeft = (details.Params[1] != 0);

                base.canBeFrozen = false;
                base.isInvulnerable = true;
                base.CollisionFlags = CollisionFlags.CollideWithOtherActors;

                health = int.MaxValue;

                speedX = (isFacingLeft ? -4f : 4f);
                speedY = 2f;

                switch (theme) {
                    case 0:
                    default:
                        await RequestMetadataAsync("Boss/Bilsy");
                        break;

                    case 1: // Xmas
                        await RequestMetadataAsync("Boss/BilsyXmas");
                        break;
                }

                SetAnimation((AnimState)1073741828);

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.85f;
                light.Brightness = 0.4f;
                light.RadiusNear = 0f;
                light.RadiusFar = 30f;

                PlaySound("FireStart");
            }

            public override void OnFixedUpdate(float timeMult)
            {
                //base.OnFixedUpdate(timeMult);
                MoveInstantly(new Vector2(speedX * timeMult, speedY * timeMult), MoveType.Relative, true);

                if (time <= 0f) {
                    DecreaseHealth(int.MaxValue);
                } else {
                    time -= timeMult;

                    FollowNearestPlayer();
                }

                // ToDo: Spawn fire particles
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(18, 18);
            }

            protected override bool OnPerish(ActorBase collider)
            {
                Explosion.Create(levelHandler, Transform.Pos + Speed, Explosion.RF);

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

            private void FollowNearestPlayer()
            {
                bool found = false;
                Vector3 pos = Transform.Pos;
                Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, pos.Z);

                List<Player> players = levelHandler.Players;
                for (int i = 0; i < players.Count; i++) {
                    Vector3 newPos = players[i].Transform.Pos;
                    if ((pos - newPos).Length < (pos - targetPos).Length) {
                        targetPos = newPos;
                        found = true;
                    }
                }

                if (found) {
                    Vector3 diff = (targetPos - pos).Normalized;
                    Vector3 speed = (new Vector3(speedX, speedY, 0f) + diff * 0.4f).Normalized;
                    speedX = speed.X * 4f;
                    speedY = speed.Y * 4f;

                    Transform.Angle = MathF.Atan2(speedY, speedX);
                }
            }
        }
    }
}