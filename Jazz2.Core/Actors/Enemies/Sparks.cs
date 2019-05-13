using System.Collections.Generic;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Sparks : EnemyBase
    {
        private const float DefaultSpeed = -2f;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            collisionFlags = CollisionFlags.CollideWithOtherActors;
            //friction = 40f;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            RequestMetadata("Enemy/Sparks");
            SetAnimation(AnimState.Idle);

            IsFacingLeft = true;
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();
            HandleBlinking();

            if (frozenTimeLeft > 0) {
                frozenTimeLeft -= Time.TimeMult;
                return;
            }

            MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                Vector3 direction = (pos - targetPos);
                float length = direction.Length;
                if (length < 180f) {
                    if (length > 100f) {
                        direction.Normalize();
                        speedX = (direction.X * DefaultSpeed + speedX) * 0.5f;
                        speedY = (direction.Y * DefaultSpeed + speedY) * 0.5f;
                    }
                    return;
                }
            }

            speedX = 0f;
            speedY = 0f;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}