using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Skeleton : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Vector3 pos = Transform.Pos;
            pos.Y -= 6f;
            Transform.Pos = pos;

            SetHealthByDifficulty(3);
            scoreValue = 200;

            RequestMetadata("Enemy/Skeleton");
            SetAnimation(AnimState.Walk);

            isFacingLeft = MathF.Rnd.NextBool();
            speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(30, 30);
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
        }

        protected override void OnHealthChanged(ActorBase collider)
        {
            CreateSpriteDebris("ENEMY_SKELETON_BONE", MathF.Rnd.Next(1, 3));

            base.OnHealthChanged(collider);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            // TODO: Sound of bones
            // TODO: Use CreateDeathDebris(collider); instead?
            CreateParticleDebris();
            api.PlayCommonSound(this, "COMMON_SPLAT");

            CreateSpriteDebris("ENEMY_SKELETON_SKULL", 1);
            CreateSpriteDebris("ENEMY_SKELETON_BONE", MathF.Rnd.Next(9, 12));

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}