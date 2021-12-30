﻿using System.Threading.Tasks;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class PowerUpShieldMonitor : SolidObjectBase
    {
        private Player.ShieldType shieldType;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/PowerUpMonitorShield");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new PowerUpShieldMonitor();
            actor.OnActivated(details);
            return actor;
        }

        private PowerUpShieldMonitor()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            shieldType = (Player.ShieldType)details.Params[0];

            Movable = true;

            CollisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            await RequestMetadataAsync("Object/PowerUpMonitorShield");

            switch (shieldType) {
                case Player.ShieldType.Fire: SetAnimation("ShieldFire"); break;
                case Player.ShieldType.Water: SetAnimation("ShieldWater"); break;
                case Player.ShieldType.Laser: SetAnimation("ShieldLaser"); break;
                case Player.ShieldType.Lightning: SetAnimation("ShieldLightning"); break;

                default: DecreaseHealth(int.MaxValue, null); break;
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
            player.SetShield(shieldType, 30f);

            DecreaseHealth(int.MaxValue, player);
            PlaySound(Transform.Pos, "Break");
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateParticleDebris();

            return base.OnPerish(collider);
        }
    }
}