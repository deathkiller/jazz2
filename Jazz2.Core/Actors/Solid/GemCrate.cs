using System.Threading.Tasks;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class GemCrate : GenericContainer
    {
        public override EventType EventType => EventType.CrateGem;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            GenerateContents(EventType.Gem, details.Params[0], 0);
            GenerateContents(EventType.Gem, details.Params[1], 1);
            GenerateContents(EventType.Gem, details.Params[2], 2);
            GenerateContents(EventType.Gem, details.Params[3], 3);

            await RequestMetadataAsync("Object/CrateContainer");
            SetAnimation(AnimState.Idle);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            collisionFlags = CollisionFlags.None;

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