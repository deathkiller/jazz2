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

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            morphType = (MorphType)details.Params[0];

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            RequestMetadata("Object/PowerUpMonitor");

            switch (morphType) {
                case MorphType.Swap2: SetAnimation("Swap2"); break;
                case MorphType.Swap3: SetAnimation("Swap3"); break;
                case MorphType.ToBird: SetAnimation("Bird"); break;
            }

            for (int i = 0; i < api.Players.Count; i++) {
                PlayerType? playerType = GetTargetType(api.Players[i].PlayerType);
                switch (playerType) {
                    case PlayerType.Jazz: PreloadMetadata("Interactive/PlayerJazz"); break;
                    case PlayerType.Spaz: PreloadMetadata("Interactive/PlayerSpaz"); break;
                    case PlayerType.Lori: PreloadMetadata("Interactive/PlayerLori"); break;
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            if (health == 0) {
                return;
            }

            switch (other) {
                case AmmoBase collision: {
                    if ((collision.WeaponType == WeaponType.Blaster ||
                         collision.WeaponType == WeaponType.RF ||
                         collision.WeaponType == WeaponType.Seeker ||
                         collision.WeaponType == WeaponType.Pepper ||
                         collision.WeaponType == WeaponType.Electro) &&
                        collision.Owner != null) {

                        DestroyAndApplyToPlayer(collision.Owner);
                        collision.DecreaseHealth(int.MaxValue);
                    }
                    break;
                }

                case AmmoTNT collision: {
                    if (collision.Owner != null) {
                        DestroyAndApplyToPlayer(collision.Owner);
                    }
                    break;
                }

                case Player collision: {
                    if (collision.CanBreakSolidObjects) {
                        DestroyAndApplyToPlayer(collision);
                    }
                    break;
                }
            }

            base.OnHandleCollision(other);
        }

        public void DestroyAndApplyToPlayer(Player player)
        {
            PlayerType? playerType = GetTargetType(player.PlayerType);
            if (playerType.HasValue) {
                player.MorphTo(playerType.Value);

                DecreaseHealth(int.MaxValue, player);
                PlaySound("Break");
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();

            return base.OnPerish(collider);
        }

        private PlayerType? GetTargetType(PlayerType currentType)
        {
            PlayerType targetType;
            switch (morphType) {
                case MorphType.Swap2:
                    if (currentType != PlayerType.Jazz) {
                        targetType = PlayerType.Jazz;
                    } else {
                        targetType = PlayerType.Spaz;
                    }
                    break;

                case MorphType.Swap3:
                    if (currentType == PlayerType.Spaz) {
                        targetType = PlayerType.Lori;
                    } else if (currentType == PlayerType.Lori) {
                        targetType = PlayerType.Jazz;
                    } else {
                        targetType = PlayerType.Spaz;
                    }
                    break;

                //case SwapType.ToBird:
                //    // ToDo: Implement Birds
                //    break;

                default:
                    return null;
            }
            return targetType;
        }
    }
}