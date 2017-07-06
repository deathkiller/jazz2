using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Crab : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        private float noiseCooldown = 70f;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(3);
            scoreValue = 300;

            RequestMetadata("Enemy/Crab");
            SetAnimation(AnimState.Walk);

            isFacingLeft = MathF.Rnd.NextBool();
            speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(26, 20);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            if (!CanMoveToPosition(speedX, 0)) {
                isFacingLeft = !(isFacingLeft);
                speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
            }

            if (noiseCooldown <= 0f) {
                noiseCooldown = MathF.Rnd.NextFloat(60, 100);
                PlaySound("NOISE", 0.4f);
            } else {
                noiseCooldown -= Time.TimeMult;
            }

            // ToDo: Implement fall animation
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "COMMON_SPLAT");

            Explosion.Create(api, Transform.Pos, Explosion.Large);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}