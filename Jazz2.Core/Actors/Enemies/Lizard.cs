using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Lizard : EnemyBase
    {
        private const float DefaultSpeed = 1f;

        private bool stuck;
        private bool isFalling;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.Y -= 6f;
            Transform.Pos = pos;

            ushort theme = details.Params[0];
            isFalling = details.Params[1] != 0;

            SetHealthByDifficulty(isFalling ? 6 : 1);
            scoreValue = 100;

            switch (theme) {
                case 0:
                default:
                    await RequestMetadataAsync("Enemy/Lizard");
                    break;

                case 1: // Xmas
                    await RequestMetadataAsync("Enemy/LizardXmas");
                    break;
            }

            SetAnimation(AnimState.Walk);

            if (isFalling) {
                IsFacingLeft = details.Params[2] != 0;
            } else {
                IsFacingLeft = MathF.Rnd.NextBool();
            }
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;

            if (isFalling) {
                // Lizard lost its copter, check if spawn position is
                // empty, because walking Lizard has bigger hitbox
                OnUpdateHitbox();

                if (!levelHandler.IsPositionEmpty(this, ref AABBInner, true)) {
                    // Lizard was probably spawned into a wall, try to move it
                    // from the wall by 4px steps (max. 12px) in both directions
                    const float adjust = 4f;

                    for (int i = 1; i < 4; i++) {
                        if (MoveInstantly(new Vector2( adjust * i, 0f), MoveType.Relative, false) ||
                            MoveInstantly(new Vector2(-adjust * i, 0f), MoveType.Relative, false)) {
                            // Empty spot found
                            break;
                        }
                    }
                }
            }
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(30, 30);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0 || isFalling) {
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

            if (MathF.Rnd.NextFloat() < 0.004f * timeMult) {
                PlaySound("Noise", 0.4f);
            }
        }

        protected override void OnHitFloor()
        {
            base.OnHitFloor();

            if (isFalling) {
                isFalling = false;
                SetHealthByDifficulty(1);
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}