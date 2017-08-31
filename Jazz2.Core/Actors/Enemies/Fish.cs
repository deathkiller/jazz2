using System.Collections.Generic;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Fish : EnemyBase
    {
        private const float DefaultSpeed = -2f;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            RequestMetadata("Enemy/Fish");
            SetAnimation(AnimState.Idle);

            isFacingLeft = MathF.Rnd.NextBool();
        }

        // TODO: Implement this

        protected override void OnUpdate()
        {
            base.OnUpdate();

            canJump = false;

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                Vector3 direction = (pos - targetPos);
                float length = direction.Length;
                if (length < 180f) {
                    if (length > 120f) {
                        direction.Normalize();
                        speedX = direction.X * DefaultSpeed;
                        speedY = direction.Y * DefaultSpeed;

                        isFacingLeft = (speedX < 0f);
                    }
                    return;
                }
            }

            speedX = speedY = 0;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);

            api.PlayCommonSound(this, "Splat");

            Explosion.Create(api, Transform.Pos, Explosion.SmallDark);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}