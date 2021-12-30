using System.Collections.Generic;
using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class AmmoCrate : GenericContainer
    {
        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/CrateContainer");

            // ToDo: Preload also its content
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new AmmoCrate();
            actor.OnActivated(details);
            return actor;
        }

        private AmmoCrate()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Movable = true;

            CollisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            WeaponType weaponType = (WeaponType)details.Params[0];
            if (weaponType != WeaponType.Blaster) {
                GenerateContents(EventType.Ammo, 5, (ushort)weaponType);
            }

            await RequestMetadataAsync("Object/CrateContainer");

            switch (weaponType) {
                case WeaponType.Blaster: SetAnimation(AnimState.Idle); break;
                default: SetAnimation("CrateAmmo" + weaponType.ToString("G")); break;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CollisionFlags = CollisionFlags.None;

            CreateParticleDebris();

            PlaySound(Transform.Pos, "Break");

            if (Content.Count == 0) {
                // Random Ammo create
                HashSet<WeaponType> availableWeapons = new HashSet<WeaponType>();
                foreach (Player player in levelHandler.Players) {
                    for (int i = 1; i < player.WeaponAmmo.Length; i++) {
                        if (player.WeaponAmmo[i] > 0) {
                            availableWeapons.Add((WeaponType)i);
                        }
                    }
                }

                if (availableWeapons.Count == 0) {
                    availableWeapons.Add(WeaponType.Bouncer);
                }

                int n = MathF.Rnd.Next(4, 7);
                for (int i = 0; i < n; i++) {
                    WeaponType weaponType = MathF.Rnd.OneOf(availableWeapons);
                    GenerateContents(EventType.Ammo, 1, (ushort)weaponType);
                }

                CreateSpriteDebris("CrateShrapnel1", 3);
                CreateSpriteDebris("CrateShrapnel2", 2);

                SetTransition(AnimState.TransitionDeath, false, delegate {
                    base.OnPerish(collider);
                });
                SpawnContent();
                return true;
            } else {
                CreateSpriteDebris("CrateAmmoShrapnel1", 3);
                CreateSpriteDebris("CrateAmmoShrapnel2", 2);

                return base.OnPerish(collider);
            }
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