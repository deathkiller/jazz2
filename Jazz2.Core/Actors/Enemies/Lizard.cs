using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Lizard : EnemyBase
    {
        private const float DefaultSpeed = 1f;

        private bool stuck;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Vector3 pos = Transform.Pos;
            pos.Y -= 6f;
            Transform.Pos = pos;

            SetHealthByDifficulty(1);
            scoreValue = 100;

            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    RequestMetadata("Enemy/Lizard");
                    break;

                case 1: // Xmas
                    RequestMetadata("Enemy/LizardXmas");
                    break;
            }

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

            if (!CanMoveToPosition(speedX * 4, 0)) {
                if (stuck && canJump) {
                    MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                } else {
                    isFacingLeft = !(isFacingLeft);
                    speedX = (isFacingLeft ? -1 : 1) * DefaultSpeed;
                    stuck = true;
                }
            } else {
                stuck = false;
            }

            if ((MathF.Rnd.Next() & 0x3FF) == 1) {
                PlaySound("Noise", 0.4f);
            }
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