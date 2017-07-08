using System.Collections.Generic;
using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class AmmoBarrel : GenericContainer
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Movable = true;

            WeaponType weaponType = (WeaponType)details.Params[0];
            if (weaponType != WeaponType.Blaster) {
                GenerateContents(weaponType - WeaponType.Bouncer + EventType.AmmoBouncer, 5);
            }

            RequestMetadata("Object/BarrelContainer");
            SetAnimation(AnimState.Idle);
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
                     collider.WeaponType == WeaponType.Electro)) {

                    DecreaseHealth(int.MaxValue);
                    collider.DecreaseHealth(int.MaxValue);
                }
            }

            base.HandleCollision(other);
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
            }

            PlaySound("Break");

            collisionFlags = CollisionFlags.None;

            CreateParticleDebris();

            CreateSpriteDebris("BarrelShrapnel1", 3);
            CreateSpriteDebris("BarrelShrapnel2", 2);
            CreateSpriteDebris("BarrelShrapnel3", 2);
            CreateSpriteDebris("BarrelShrapnel4", 1);

            SetTransition(AnimState.TransitionDeath, false);
            SpawnContent();
            return base.OnPerish(collider);
        }
    }
}