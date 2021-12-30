using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Environment;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    // ToDo: Implement controllable copters

    public class LizardFloat : EnemyBase
    {
        private const float DefaultSpeed = 1.4f;

        private float attackTime = 200f;
        private float moveTime = 100f;

        private ushort theme;

        public static void Preload(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    PreloadMetadata("Enemy/LizardFloat");
                    break;

                case 1: // Xmas
                    PreloadMetadata("Enemy/LizardFloatXmas");
                    break;
            }
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new LizardFloat();
            actor.OnActivated(details);
            return actor;
        }

        private LizardFloat()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    await RequestMetadataAsync("Enemy/LizardFloat");
                    break;

                case 1: // Xmas
                    await RequestMetadataAsync("Enemy/LizardFloatXmas");
                    break;
            }

            CollisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(1);
            scoreValue = 200;

            SetAnimation(AnimState.Idle);

            // Spawn copter
            CopterDecor copter = new CopterDecor();
            copter.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Params = details.Params
            });
            copter.Parent = this;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            canJump = false;

            bool found = false;
            Vector3 pos = Transform.Pos;
            Vector3 targetPos = new Vector3(float.MaxValue, float.MaxValue, pos.Z);

            List<Player> players = levelHandler.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((pos - newPos).Length < (pos - targetPos).Length) {
                    targetPos = newPos;
                    found = true;
                }
            }

            if (found) {
                float distance = (targetPos - pos).Length;

                if (distance < 280f && attackTime <= 0f) {
                    SetTransition(AnimState.TransitionAttack, false, delegate {
                        Bomb bomb = new Bomb();
                        bomb.OnActivated(new ActorActivationDetails {
                            LevelHandler = levelHandler,
                            Pos = Transform.Pos + new Vector3(IsFacingLeft ? -30f : 30f, -10f, -4f),
                            Params = new[] { (ushort)(theme + 1), (ushort)(IsFacingLeft ? 1 : 0) }
                        });
                        levelHandler.AddActor(bomb);

                        SetTransition(AnimState.TransitionAttackEnd, false);
                    });

                    attackTime = MathF.Rnd.NextFloat(120, 240);
                }

                if (distance < 360f && moveTime <= 0f) {
                    Vector3 diff = (targetPos - pos).Normalized;

                    Vector3 speed = (new Vector3(speedX, speedY, 0f) + diff * 0.4f).Normalized;
                    speedX = speed.X * DefaultSpeed;
                    speedY = speed.Y * DefaultSpeed;

                    IsFacingLeft = (speedX < 0f);

                    moveTime = 8f;
                }

                attackTime -= timeMult;
            }

            moveTime -= timeMult;
        }

        protected override bool OnPerish(ActorBase collider)
        {
            if (collider is Player) {
                CreateDeathDebris(collider);
                levelHandler.PlayCommonSound("Splat", Transform.Pos);

                TryGenerateRandomDrop();
            } else {
                Lizard lizard = new Lizard();
                lizard.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = Transform.Pos,
                    Params = new[] { theme, (ushort)1, (ushort)(IsFacingLeft ? 1 : 0) }
                });
                levelHandler.AddActor(lizard);

                Explosion.Create(levelHandler, Transform.Pos, Explosion.SmokeGray);
            }

            return base.OnPerish(collider);
        }

        public class CopterDecor : ActorBase
        {
            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                CollisionFlags = CollisionFlags.ForceDisableCollisions;

                health = int.MaxValue;

                ushort theme = details.Params[0];

                switch (theme) {
                    case 0:
                    default:
                        await RequestMetadataAsync("Enemy/LizardFloat");
                        break;

                    case 1: // Xmas
                        await RequestMetadataAsync("Enemy/LizardFloatXmas");
                        break;
                }

                SetAnimation(AnimState.Activated);
            }

            public override void OnFixedUpdate(float timeMult)
            {
                Transform.RelativePos = new Vector3(0f, 8f, 4f);
            }
        }
    }
}