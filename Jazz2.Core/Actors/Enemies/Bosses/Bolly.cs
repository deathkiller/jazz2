using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game;
using Jazz2.Game.Structs;
using static Duality.Component;

namespace Jazz2.Actors.Bosses
{

    public class Bolly : BossBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateFlying = 1;
        private const int StateNewDirection = 2;
        private const int StatePrepairingToAttack = 3;
        private const int StateAttacking = 4;

        private int state = StateWaiting;
        private float stateTime;
        private float noiseCooldown;
        private int rocketsLeft;

        private Bottom bottom;
        private Turret turret;
        private ChainPiece[] pieces;
        private float chainPhase;

        private ushort endText;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            endText = details.Params[1];

            canBeFrozen = false;
            SetHealthByDifficulty(100);
            scoreValue = 3000;

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            await RequestMetadataAsync("Boss/Bolly");
            SetAnimation(AnimState.Idle);

            // Bottom
            bottom = new Bottom();
            bottom.OnActivated(new ActorActivationDetails {
                Api = api
            });
            bottom.Parent = this;

            // Turret
            turret = new Turret();
            turret.OnActivated(new ActorActivationDetails {
                Api = api
            });
            turret.Parent = this;

            // Chain
            const int ChainLength = 5 * 3;
            pieces = new ChainPiece[ChainLength];
            for (int i = 0; i < ChainLength; i++) {
                pieces[i] = new ChainPiece();
                pieces[i].OnActivated(new ActorActivationDetails {
                    Api = api,
                    Params = new ushort[] { (ushort)((i % 3) == 2 ? 0 : 1) }
                });
                api.AddActor(pieces[i]);
            }
        }

        protected override void OnBossActivated()
        {
            FollowNearestPlayer(StateFlying, 100);
        }

        protected override void OnDeactivated(ShutdownContext context)
        {
            state = StateWaiting;

            if (pieces != null) {
                for (int i = 0; i < pieces.Length; i++) {
                    api.RemoveActor(pieces[i]);
                }

                pieces = null;
            }
        }

        protected override void OnFixedUpdate(float timeMult)
        {
            OnUpdateHitbox();
            HandleBlinking(timeMult);

            MoveInstantly(new Vector2(speedX * timeMult, speedY * timeMult), MoveType.Relative, true);

            switch (state) {
                case StateFlying: {
                    if (stateTime <= 0f) {
                        if (MathF.Rnd.NextFloat() < 0.1f) {
                            state = StatePrepairingToAttack;
                            stateTime = 100;
                            speedX = 0;
                            speedY = 0;
                        } else {
                            state = StateNewDirection;
                            stateTime = 50;
                        }
                    }
                    break;
                }

                case StateNewDirection: {
                    if (stateTime <= 0f) {
                        FollowNearestPlayer(StateFlying, 1);
                    }
                    break;
                }

                case StatePrepairingToAttack: {
                    if (stateTime <= 0f) {
                        state = StateAttacking;
                        stateTime = 20;
                        rocketsLeft = 5;

                        PlaySound("PreAttack");
                    }
                    break;
                }

                case StateAttacking: {
                    if (stateTime <= 0f) {
                        if (rocketsLeft > 0) {
                            stateTime = 20;

                            FireRocket();
                            rocketsLeft--;

                            PlaySound("Attack");
                        } else {
                            state = StateNewDirection;
                            stateTime = 100;

                            PlaySound("PostAttack");
                        }
                    }
                    break;
                }
            }

            Vector3 pos = Transform.Pos;
            float distance = 30;
            for (int i = 0; i < pieces.Length; i++) {
                float angle = MathF.Sin(chainPhase - i * 0.08f) * 1.2f + MathF.PiOver2;

                Vector3 piecePos = pos;
                piecePos.X += MathF.Cos(angle) * distance;
                piecePos.Y += MathF.Sin(angle) * distance;
                piecePos.Z += pieces[i].ZOffset;
                pieces[i].Transform.Pos = piecePos;

                distance += pieces[i].Size;
            }

            if (noiseCooldown > 0f) {
                noiseCooldown -= timeMult;
            } else {
                noiseCooldown = 120f;
                PlaySound("Noise", 0.2f);
            }

            stateTime -= timeMult;
            chainPhase += timeMult * 0.08f;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            api.TileMap.CreateParticleDebris(availableAnimations["Top"], Transform.Pos, Vector2.Zero, 0, IsFacingLeft);
            api.TileMap.CreateParticleDebris(availableAnimations["Bottom"], Transform.Pos, Vector2.Zero, 0, IsFacingLeft);

            for (int i = 0; i < pieces.Length; i++) {
                api.RemoveActor(pieces[i]);
            }

            api.PlayCommonSound(Transform.Pos, "Splat");

            Explosion.Create(api, Transform.Pos, Explosion.Large);

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

                targetPos.Y -= 100;

                IsFacingLeft = (targetPos.X < pos.X);

                Vector3 speed = (targetPos - pos).Normalized;
                speedX = speed.X * 0.8f;
                speedY = speed.Y * 0.8f;
            }
        }

        private void FireRocket()
        {
            bool found = false;
            Vector3 pos = Transform.Pos;
            Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, pos.Z);

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((pos - newPos).Length < (pos - targetPos).Length) {
                    targetPos = newPos;
                    found = true;
                }
            }

            if (found) {
                Vector3 diff = (targetPos - pos).Normalized;

                Rocket rocket = new Rocket();
                rocket.OnActivated(new ActorActivationDetails {
                    Api = api,
                    Pos = new Vector3(pos.X + (IsFacingLeft ? 10f : -10f), pos.Y + 10f, pos.Z - 2f)
                });
                rocket.Transform.Angle = MathF.Atan2(diff.Y, diff.X);
                api.AddActor(rocket);
            }
        }

        private class Bottom : EnemyBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                collisionFlags = CollisionFlags.CollideWithOtherActors;

                health = int.MaxValue;

                await RequestMetadataAsync("Boss/Bolly");
                SetAnimation((AnimState)1);
            }

            protected override void OnFixedUpdate(float timeMult)
            {
                IsFacingLeft = (Parent as Bolly).IsFacingLeft;

                Transform.RelativePos = new Vector3(0f, 0f, -2f);

                OnUpdateHitbox();
            }

            public override void OnHandleCollision(ActorBase other)
            {
            }
        }

        private class Turret : EnemyBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                collisionFlags = CollisionFlags.ForceDisableCollisions;

                health = int.MaxValue;

                await RequestMetadataAsync("Boss/Bolly");
                SetAnimation((AnimState)2);
            }

            protected override void OnFixedUpdate(float timeMult)
            {
                IsFacingLeft = (Parent as Bolly).IsFacingLeft;

                Transform.RelativePos = new Vector3(IsFacingLeft ? 10f : -10f, 10f, -4f);
            }

            public override void OnHandleCollision(ActorBase other)
            {
            }
        }

        private class ChainPiece : EnemyBase
        {
            public float Size;
            public float ZOffset;

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                base.canBeFrozen = false;
                base.canCollideWithAmmo = false;
                base.isInvulnerable = true;
                base.collisionFlags = CollisionFlags.CollideWithOtherActors;

                health = int.MaxValue;

                await RequestMetadataAsync("Boss/Bolly");

                AnimState animState;
                if (details.Params[0] == 0) {
                    animState = (AnimState)3;
                    Size = 14;
                    ZOffset = 15;
                } else {
                    animState = (AnimState)4;
                    Size = 7;
                    ZOffset = 20;
                }
                SetAnimation(animState);
            }

            protected override void OnFixedUpdate(float timeMult)
            {
            }
        }

        public class Rocket : EnemyBase
        {
            private float time = 300f;

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                base.canBeFrozen = false;
                base.canCollideWithAmmo = false;
                base.isInvulnerable = true;
                base.collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors | CollisionFlags.SkipPerPixelCollisions;

                health = int.MaxValue;

                await RequestMetadataAsync("Boss/Bolly");
                SetAnimation((AnimState)5);

                LightEmitter light = AddComponent<LightEmitter>();
                light.Intensity = 0.8f;
                light.Brightness = 0.8f;
                light.RadiusNear = 3f;
                light.RadiusFar = 12f;
            }

            protected override void OnFixedUpdate(float timeMult)
            {
                float angle = Transform.Angle;
                speedX += MathF.Cos(angle) * 0.14f * timeMult;
                speedY += MathF.Sin(angle) * 0.14f * timeMult;

                base.OnFixedUpdate(timeMult);

                if (time <= 0f) {
                    DecreaseHealth(int.MaxValue);
                } else {
                    time -= timeMult;
                }
            }

            protected override void OnUpdateHitbox()
            {
                UpdateHitbox(20, 20);
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