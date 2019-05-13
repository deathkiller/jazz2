using System.Collections.Generic;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    // ToDo: Implement mace
    // ToDo: Implement rockets

    public class Bolly : BossBase
    {
        private const int StateTransition = -1;
        private const int StateWaiting = 0;
        private const int StateWalking1 = 1;
        private const int StateWalking2 = 2;
        //private const int StateAttacking = 3;

        private int state = StateWaiting;
        private float stateTime;

        private Bottom bottom;
        private Turret turret;

        private ushort endText;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            endText = details.Params[1];

            canBeFrozen = false;
            SetHealthByDifficulty(100);
            scoreValue = 3000;

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            RequestMetadata("Boss/Bolly");
            SetAnimation(AnimState.Idle);

            // Spawn bottom
            bottom = new Bottom();
            bottom.OnActivated(new ActorActivationDetails {
                Api = api
            });
            bottom.Parent = this;

            // Spawn turret
            turret = new Turret();
            turret.OnActivated(new ActorActivationDetails {
                Api = api
            });
            turret.Parent = this;
        }

        public override void OnBossActivated()
        {
            FollowNearestPlayer(StateWalking1, 100);
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            HandleBlinking();

            MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);

            switch (state) {
                case StateWalking1: {
                    if (stateTime <= 0f) {
                        state = StateWalking2;
                        stateTime = 20;
                    }
                    break;
                }

                case StateWalking2: {
                    if (stateTime <= 0f) {
                        FollowNearestPlayer(StateWalking1, 20);
                    }
                    break;
                }
            }

            stateTime -= Time.TimeMult;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            api.TileMap.CreateParticleDebris(availableAnimations["Top"], Transform.Pos, Vector2.Zero, 0, IsFacingLeft);
            api.TileMap.CreateParticleDebris(availableAnimations["Bottom"], Transform.Pos, Vector2.Zero, 0, IsFacingLeft);

            api.PlayCommonSound(this, "Splat");

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

                IsFacingLeft = (targetPos.X < pos.X);

                Vector3 speed = (targetPos - pos).Normalized;
                speedX = speed.X * 0.8f;
                speedY = speed.Y * 0.8f;
            }
        }

        private class Bottom : EnemyBase
        {
            public override void OnActivated(ActorActivationDetails details)
            {
                base.OnActivated(details);

                collisionFlags = CollisionFlags.CollideWithOtherActors;

                health = int.MaxValue;

                RequestMetadata("Boss/Bolly");
                SetAnimation((AnimState)1);
            }

            protected override void OnUpdate()
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
            public override void OnActivated(ActorActivationDetails details)
            {
                base.OnActivated(details);

                collisionFlags = CollisionFlags.None;

                health = int.MaxValue;

                RequestMetadata("Boss/Bolly");
                SetAnimation((AnimState)2);
            }

            protected override void OnUpdate()
            {
                IsFacingLeft = (Parent as Bolly).IsFacingLeft;

                Transform.RelativePos = new Vector3(IsFacingLeft ? 10f : -10f, 10f, -4f);
            }

            public override void OnHandleCollision(ActorBase other)
            {
            }
        }
    }
}