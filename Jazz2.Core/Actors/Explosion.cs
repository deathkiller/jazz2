using Duality;
using Jazz2.Game;

namespace Jazz2.Actors
{
    public class Explosion : ActorBase
    {
        // Available explosion types
        public const ushort Tiny = 0;
        public const ushort TinyDark = 1;
        public const ushort Small = 2;
        public const ushort SmallDark = 3;
        public const ushort Large = 4;

        public const ushort SmokeBrown = 5;
        public const ushort SmokeGray = 6;
        public const ushort SmokeWhite = 7;
        public const ushort SmokePoof = 8;

        public const ushort WaterSplash = 9;

        public const ushort Pepper = 10;
        public const ushort RF = 11;

        public const ushort Generator = 20;

        public static void Create(ActorApi api, Vector3 pos, ushort type)
        {
            Explosion explosion = new Explosion();
            explosion.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos,
                Params = new[] { type }
            });
            api.AddActor(explosion);
        }

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.None;

            ushort type = details.Params[0];

            RequestMetadata("Common/Explosions");

            switch (type)
            {
                default:
                case Tiny: SetAnimation("Tiny"); break;
                case TinyDark: SetAnimation("TinyDark"); break;
                case Small: SetAnimation("Small"); break;
                case SmallDark: SetAnimation("SmallDark"); break;
                case Large: SetAnimation("Large"); break;

                case SmokeBrown: SetAnimation("SmokeBrown"); break;
                case SmokeGray: SetAnimation("SmokeGray"); break;
                case SmokeWhite: SetAnimation("SmokeWhite"); break;
                case SmokePoof: SetAnimation("SmokePoof"); break;

                case WaterSplash: SetAnimation("WaterSplash"); break;

                case Pepper: SetAnimation("Pepper"); break;
                case RF: SetAnimation("RF"); break;

                case Generator:
                    SetAnimation("Generator");

                    // Apply random orientation
                    Transform.Angle = MathF.Rnd.Next(4) * MathF.PiOver2;
                    isFacingLeft = (MathF.Rnd.NextFloat() < 0.5f);
                    RefreshFlipMode();
                    break;
            }
        }

        protected override void OnAnimationFinished()
        {
            //base.OnAnimationFinished();

            DecreaseHealth(int.MaxValue);
        }

        protected override void OnUpdate()
        {
            //base.OnUpdate();
        }
    }
}