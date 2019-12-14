using System.Threading.Tasks;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class CrateContainer : GenericContainer
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Movable = true;

            CollisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            if (details.Params[0] != 0 && details.Params[1] != 0) {
                GenerateContents((EventType)details.Params[0], details.Params[1], details.Params[2], details.Params[3],
                    details.Params[4], details.Params[5], details.Params[6], details.Params[7]);
            }

            await RequestMetadataAsync("Object/CrateContainer");
            SetAnimation(AnimState.Idle);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CollisionFlags = CollisionFlags.None;

            CreateParticleDebris();

            PlaySound(Transform.Pos, "Break");

            CreateSpriteDebris("CrateShrapnel1", 3);
            CreateSpriteDebris("CrateShrapnel2", 2);

            SetTransition(AnimState.TransitionDeath, false, delegate {
                base.OnPerish(collider);
            });
            SpawnContent();
            return true;
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case AmmoBase collision: {
                    DecreaseHealth(collision.Strength, collision);
                    break;
                }

                case AmmoTNT collision: {
                    DecreaseHealth(int.MaxValue, collision);
                    break;
                }

                case Player collision: {
                    if (collision.CanBreakSolidObjects) {
                        DecreaseHealth(int.MaxValue, collision);
                    }
                    break;
                }
            }
        }
    }
}