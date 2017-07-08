using System.Collections.Generic;
using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class AmmoCrate : GenericContainer
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            WeaponType weaponType = (WeaponType)details.Params[0];
            if (weaponType != WeaponType.Blaster) {
                GenerateContents(weaponType - WeaponType.Bouncer + EventType.AmmoBouncer, 5);
            }

            RequestMetadata("Object/CrateContainer");

            switch (weaponType) {
                case WeaponType.Blaster: SetAnimation(AnimState.Idle); break;
                default: SetAnimation("CrateAmmo" + weaponType.ToString("G")); break;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            collisionFlags = CollisionFlags.None;

            CreateParticleDebris();

            PlaySound("Break");

            if (content.Count == 0) {
                // Random Ammo create
                HashSet<WeaponType> availableWeapons = new HashSet<WeaponType>();
                foreach (Player player in api.Players) {
                    for (int i = 1; i < player.WeaponAmmo.Length; i++) {
                        if (player.WeaponAmmo[i] > 0) {
                            availableWeapons.Add((WeaponType)i);
                        }
                    }
                }

                if (availableWeapons.Count > 0) {
                    int n = MathF.Rnd.Next(4, 7);
                    for (int i = 0; i < n; i++) {
                        WeaponType weapon = MathF.Rnd.OneOf(availableWeapons);
                        GenerateContents(weapon - WeaponType.Bouncer + EventType.AmmoBouncer, 1);
                    }
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

                SpawnContent();
                return base.OnPerish(collider);
            }
        }

        public override void HandleCollision(ActorBase other)
        {
            AmmoBase collider = other as AmmoBase;
            if (collider != null) {
                DecreaseHealth(collider.Strength, collider);
            }
        }
    }
}