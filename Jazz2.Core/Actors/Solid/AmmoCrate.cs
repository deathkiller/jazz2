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
                case WeaponType.Bouncer: SetAnimation("OBJECT_CRATE_AMMO_BOUNCER"); break;
                case WeaponType.Freezer: SetAnimation("OBJECT_CRATE_AMMO_FREEZER"); break;
                case WeaponType.Seeker: SetAnimation("OBJECT_CRATE_AMMO_SEEKER"); break;
                case WeaponType.RF: SetAnimation("OBJECT_CRATE_AMMO_RF"); break;
                case WeaponType.Toaster: SetAnimation("OBJECT_CRATE_AMMO_TOASTER"); break;
                case WeaponType.TNT: SetAnimation("OBJECT_CRATE_AMMO_TNT"); break;
                case WeaponType.Pepper: SetAnimation("OBJECT_CRATE_AMMO_PEPPER"); break;
                case WeaponType.Electro: SetAnimation("OBJECT_CRATE_AMMO_ELECTRO"); break;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            if (content.Count == 0) {
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

                CreateSpriteDebris("OBJECT_CRATE_SHRAPNEL_1", 3);
                CreateSpriteDebris("OBJECT_CRATE_SHRAPNEL_2", 2);
            } else {
                CreateSpriteDebris("OBJECT_CRATE_AMMO_SHRAPNEL_1", 3);
                CreateSpriteDebris("OBJECT_CRATE_AMMO_SHRAPNEL_2", 2);
            }

            collisionFlags = CollisionFlags.None;

            CreateParticleDebris();

            PlaySound("Break");

            SetTransition(AnimState.TransitionDeath, false);
            SpawnContent();
            return base.OnPerish(collider);
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