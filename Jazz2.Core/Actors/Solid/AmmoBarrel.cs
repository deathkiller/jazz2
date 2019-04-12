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
                GenerateContents(EventType.Ammo, 5, (ushort)weaponType);
            }

            RequestMetadata("Object/BarrelContainer");
            SetAnimation(AnimState.Idle);
        }

        public override void OnHandleCollision(ActorBase other)
        {
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
            if (Content.Count == 0) {
                HashSet<WeaponType> availableWeapons = new HashSet<WeaponType>();
                foreach (Player player in api.Players) {
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
            }

            PlaySound("Break");

            collisionFlags = CollisionFlags.None;

            CreateParticleDebris();

            CreateSpriteDebris("BarrelShrapnel1", 3);
            CreateSpriteDebris("BarrelShrapnel2", 2);
            CreateSpriteDebris("BarrelShrapnel3", 2);
            CreateSpriteDebris("BarrelShrapnel4", 1);

            return base.OnPerish(collider);
        }
    }
}