using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Skeleton : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        private bool stuck;

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

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
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

            if (canJump) {
                if (!CanMoveToPosition(speedX * 4, 0)) {
                    if (stuck) {
                        MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                    } else {
                        IsFacingLeft = !IsFacingLeft;
                        speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
                        stuck = true;
                    }
                } else {
                    stuck = false;
                }
            }
        }

        protected override void OnHealthChanged(ActorBase collider)
        {
            CreateSpriteDebris("Bone", MathF.Rnd.Next(1, 3));

            base.OnHealthChanged(collider);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            // TODO: Sound of bones
            // TODO: Use CreateDeathDebris(collider); instead?
            CreateParticleDebris();
            api.PlayCommonSound(this, "Splat");

            CreateSpriteDebris("Skull", 1);
            CreateSpriteDebris("Bone", MathF.Rnd.Next(9, 12));

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}