using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class PowerUpSwapMonitor : SolidObjectBase
    {
        private enum SwapType
        {
            Swap2,
            Swap3,
            ToBird
        }

        private SwapType swapType;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            swapType = (SwapType)details.Params[0];

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Object/PowerUpMonitor");

            switch (swapType) {
                case SwapType.Swap2: SetAnimation("Swap2"); break;
                case SwapType.Swap3: SetAnimation("Swap3"); break;
                case SwapType.ToBird: SetAnimation("Bird"); break;
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
            switch (swapType) {
                case SwapType.Swap2:
                    if (player.PlayerType != PlayerType.Jazz) {
                        targetType = PlayerType.Jazz;
                    } else  {
                        targetType = PlayerType.Spaz;
                    }
                    break;

                case SwapType.Swap3:
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

            player.TransformTo(targetType);

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