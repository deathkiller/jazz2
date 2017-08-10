using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Solid
{
    public class TriggerCrate : SolidObjectBase
    {
        private ushort triggerID;
        private bool? newState;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            triggerID = details.Params[0];
            if (details.Params[2] != 0) {
                newState = null;
            } else {
                newState = (details.Params[1] != 0);
            }

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Object/TriggerCrate");

            SetAnimation(AnimState.Idle);
        }

        public override void HandleCollision(ActorBase other)
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
            }

            base.HandleCollision(other);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            TileMap tiles = api.TileMap;
            if (tiles != null) {
                if (newState.HasValue) {
                    // Turn off/on
                    tiles.SetTrigger(triggerID, newState.Value);
                } else {
                    // Switch
                    tiles.SetTrigger(triggerID, !tiles.GetTrigger(triggerID));
                }
            }

            PlaySound("Break");

            CreateParticleDebris();

            Explosion.Create(api, Transform.Pos, Explosion.SmokeBrown);

            return base.OnPerish(collider);
        }
    }
}