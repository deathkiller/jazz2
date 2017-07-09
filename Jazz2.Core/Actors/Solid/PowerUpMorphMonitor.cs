using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class PowerUpMorphMonitor : SolidObjectBase
    {
        private enum MorphType
        {
            Swap2,
            Swap3,
            ToBird
        }

        private MorphType morphType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            morphType = (MorphType)details.Params[0];

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Object/PowerUpMonitor");

            switch (morphType) {
                case MorphType.Swap2: SetAnimation("Swap2"); break;
                case MorphType.Swap3: SetAnimation("Swap3"); break;
                case MorphType.ToBird: SetAnimation("Bird"); break;
            }
        }

        public override void HandleCollision(ActorBase other)
        {
            if (health == 0) {
                return;
            }

            AmmoBase collider = other as AmmoBase;
            if (collider != null) {
                if ((collider.WeaponType == WeaponType.RF ||
                     collider.WeaponType == WeaponType.Seeker ||
                     collider.WeaponType == WeaponType.Pepper ||
                     collider.WeaponType == WeaponType.Electro) &&
                    collider.Owner != null) {

                    DestroyAndApplyToPlayer(collider.Owner);
                    collider.DecreaseHealth(int.MaxValue);
                }
            }

            base.HandleCollision(other);
        }

        public void DestroyAndApplyToPlayer(Player player)
        {
            PlayerType targetType;
            switch (morphType) {
                case MorphType.Swap2:
                    if (player.PlayerType != PlayerType.Jazz) {
                        targetType = PlayerType.Jazz;
                    } else  {
                        targetType = PlayerType.Spaz;
                    }
                    break;

                case MorphType.Swap3:
                    if (player.PlayerType == PlayerType.Spaz) {
                        targetType = PlayerType.Lori;
                    } else if (player.PlayerType == PlayerType.Lori) {
                        targetType = PlayerType.Jazz;
                    } else {
                        targetType = PlayerType.Spaz;
                    }
                    break;

                //case SwapType.ToBird:
                //    // ToDo: Implement Birds
                //    break;

                default:
                    return;
            }

            player.MorphTo(targetType);

            DecreaseHealth(int.MaxValue, player);
            PlaySound("Break");
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();

            return base.OnPerish(collider);
        }
    }
}