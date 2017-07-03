using System.Collections.Generic;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Dragonfly : EnemyBase
    {
        private const float DefaultSpeed = -3.2f;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            RequestMetadata("Enemy/Dragonfly");
            SetAnimation(AnimState.Idle);

            isFacingLeft = MathF.Rnd.NextBool();
        }

        // TODO: Do this better...

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            Vector3 pos = Transform.Pos;
            Vector3 targetPos;

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                targetPos = players[i].Transform.Pos;
                Vector3 direction = (pos - targetPos);
                float length = direction.Length;
                if (length < 180f) {
                    if (length > 80f) {
                        direction.Normalize();
                        speedX = direction.X * DefaultSpeed;
                        speedY = direction.Y * DefaultSpeed;
                    }
                    return;
                }
            }

            speedX = speedY = 0;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();
            api.PlayCommonSound(this, "COMMON_SPLAT");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}