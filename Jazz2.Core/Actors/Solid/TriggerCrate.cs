using System.Threading.Tasks;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Solid
{
    public class TriggerCrate : SolidObjectBase
    {
        private ushort triggerID;
        private bool? newState;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            triggerID = details.Params[0];
            if (details.Params[2] != 0) {
                newState = null;
            } else {
                newState = (details.Params[1] != 0);
            }

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            await RequestMetadataAsync("Object/TriggerCrate");
            SetAnimation("Crate");
        }

        public override void OnHandleCollision(ActorBase other)
        {
            if (health == 0) {
                return;
            }

            switch (other) {
                case AmmoBase collision: {
                    if ((collision.WeaponType == WeaponType.RF ||
                         collision.WeaponType == WeaponType.Seeker ||
                         collision.WeaponType == WeaponType.Pepper ||
                         collision.WeaponType == WeaponType.Electro)) {

                        DecreaseHealth(collision.Strength, collision);
                        collision.DecreaseHealth(int.MaxValue);
                    }
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

            base.OnHandleCollision(other);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            TileMap tiles = levelHandler.TileMap;
            if (tiles != null) {
                if (newState.HasValue) {
                    // Turn off/on
                    tiles.SetTrigger(triggerID, newState.Value);
                } else {
                    // Switch
                    tiles.SetTrigger(triggerID, !tiles.GetTrigger(triggerID));
                }
            }

            PlaySound(Transform.Pos, "Break");

            CreateParticleDebris();

            Explosion.Create(levelHandler, Transform.Pos, Explosion.SmokeBrown);

            return base.OnPerish(collider);
        }
    }
}