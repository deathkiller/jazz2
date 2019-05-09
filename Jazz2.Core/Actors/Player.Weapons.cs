using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    // ToDo: Show flash from weapon

    partial class Player
    {
        private WeaponType currentWeapon;
        private float weaponCooldown;
        private int[] weaponAmmo;
        private byte[] weaponUpgrades;

        public WeaponType CurrentWeapon => currentWeapon;

        public int[] WeaponAmmo => weaponAmmo;

        public byte[] WeaponUpgrades => weaponUpgrades;

        public bool AddAmmo(WeaponType type, int count)
        {
            const int ammoLimit = 99;

            if (weaponAmmo[(int)type] < 0 || weaponAmmo[(int)type] >= ammoLimit) {
                return false;
            }

            bool switchTo = (weaponAmmo[(int)type] == 0);

            weaponAmmo[(int)type] = MathF.Min(weaponAmmo[(int)type] + count, ammoLimit);

            if (switchTo) {
                currentWeapon = type;

                PreloadMetadata("Weapon/" + currentWeapon);
            }

            PlaySound("PickupAmmo");
            return true;
        }

        public void AddWeaponUpgrade(WeaponType type, byte upgrade)
        {
            weaponUpgrades[(int)type] |= upgrade;
        }

        public bool AddFastFire(int count)
        {
            const int fastFireLimit = 9;

            int current = (weaponUpgrades[(int)WeaponType.Blaster] >> 1);
            if (current >= fastFireLimit) {
                return false;
            }

            current = MathF.Min(current + count, fastFireLimit);

            weaponUpgrades[(int)WeaponType.Blaster] = (byte)((weaponUpgrades[(int)WeaponType.Blaster] & 0x1) | (current << 1));

            PlaySound("PickupAmmo");
            return true;
        }

        private void SwitchToNextWeapon()
        {
            // Find next available weapon
            currentWeapon = (WeaponType)((int)(currentWeapon + 1) % (int)WeaponType.Count);

            for (int i = 0; i < (int)WeaponType.Count && weaponAmmo[(int)currentWeapon] == 0; i++) {
                currentWeapon = (WeaponType)((int)(currentWeapon + 1) % (int)WeaponType.Count);
            }

            PreloadMetadata("Weapon/" + currentWeapon);
        }

        private void FireWeapon()
        {
            if (weaponCooldown > 0f) {
                return;
            }

            // Rewind the animation, if it should be played only once
            if (currentAnimation.OnlyOnce) {
                renderer.AnimTime = 0f;
            }

            switch (currentWeapon) {
                case WeaponType.Blaster: FireWeaponBlaster(); break;
                case WeaponType.Bouncer: FireWeaponBouncer(); break;
                case WeaponType.Freezer: FireWeaponFreezer(); break;
                case WeaponType.Seeker: FireWeaponSeeker(); break;
                case WeaponType.RF: FireWeaponRF(); break;
                case WeaponType.Toaster: FireWeaponToaster(); break;
                case WeaponType.TNT: FireWeaponTNT(); break;
                case WeaponType.Pepper: FireWeaponPepper(); break;
                case WeaponType.Electro: FireWeaponElectro(); break;

                case WeaponType.Thunderbolt:
                    if (!FireWeaponThunderbolt()) {
                        return;
                    }
                    break;

                default:
                    return;
            }

            if (weaponAmmo[(int)currentWeapon] > 0) {
                weaponAmmo[(int)currentWeapon]--;

                // No ammo, switch weapons
                if (weaponAmmo[(int)currentWeapon] == 0) {
                    SwitchToNextWeapon();
                    PlaySound("SwitchAmmo");
                }
            }
        }

        private void GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle)
        {
            initialPos = Transform.Pos;

            // Spawn bullet behind the player
            initialPos.Z += 2f;

            gunspotPos = initialPos;

            if (inWater) {
                angle = Transform.Angle;

                int size = (currentAnimation.Base.FrameDimensions.X / 2);
                gunspotPos.X += (MathF.Cos(angle) * size) * (IsFacingLeft ? -1f : 1f);
                gunspotPos.Y += (MathF.Sin(angle) * size) * (IsFacingLeft ? -1f : 1f);
            } else {
                gunspotPos.X += (currentAnimation.Base.Hotspot.X - currentAnimation.Base.Gunspot.X) * (IsFacingLeft ? 1 : -1);
                gunspotPos.Y -= (currentAnimation.Base.Hotspot.Y - currentAnimation.Base.Gunspot.Y);

                if ((currentAnimationState & AnimState.Lookup) > 0) {
                    initialPos.X = gunspotPos.X;
                    angle = MathF.PiOver2 * (IsFacingLeft ? 1 : -1);
                } else {
                    initialPos.Y = gunspotPos.Y;
                    angle = 0f;
                }
            }
        }

        private void FireWeaponBlaster()
        {
            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            AmmoBlaster newAmmo = new AmmoBlaster();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            api.AddActor(newAmmo);

            PlaySound("WeaponBlaster");
            weaponCooldown = 40f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 2f;
        }

        private void FireWeaponBouncer()
        {
            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            AmmoBouncer newAmmo = new AmmoBouncer();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            api.AddActor(newAmmo);

            weaponCooldown = 32f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.7f;
        }

        private void FireWeaponFreezer()
        {
            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            if ((weaponUpgrades[(int)currentWeapon] & 0x1) != 0) {
                AmmoFreezer newAmmo = new AmmoFreezer();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle - 0.018f, IsFacingLeft);
                api.AddActor(newAmmo);

                newAmmo = new AmmoFreezer();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle + 0.018f, IsFacingLeft);
                api.AddActor(newAmmo);
            } else {
                AmmoFreezer newAmmo = new AmmoFreezer();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
                api.AddActor(newAmmo);
            }

            weaponCooldown = 46f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.6f;
        }

        private void FireWeaponSeeker()
        {
            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            AmmoSeeker newAmmo = new AmmoSeeker();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            api.AddActor(newAmmo);

            weaponCooldown = 46f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.4f;
        }

        private void FireWeaponRF()
        {
            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            if ((weaponUpgrades[(int)currentWeapon] & 0x1) != 0) {
                AmmoRF newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle - 0.26f, IsFacingLeft);
                api.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
                api.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle + 0.26f, IsFacingLeft);
                api.AddActor(newAmmo);
            } else {
                AmmoRF newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle - 0.2f, IsFacingLeft);
                api.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle + 0.2f, IsFacingLeft);
                api.AddActor(newAmmo);
            }

            weaponCooldown = 34f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.4f;
        }

        private void FireWeaponToaster()
        {
            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            AmmoToaster newAmmo = new AmmoToaster();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            api.AddActor(newAmmo);

            //PlaySound("WeaponToaster", 0.6f);

            weaponCooldown = 6f;
        }

        private void FireWeaponTNT()
        {
            Vector3 pos = Transform.Pos;

            AmmoTNT tnt = new AmmoTNT();
            tnt.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            tnt.OnFire(this);
            api.AddActor(tnt);

            weaponCooldown = 30f;
        }

        private void FireWeaponPepper()
        {
            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            AmmoPepper newAmmo = new AmmoPepper();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle + MathF.Rnd.NextFloat(-0.2f, 0.2f), IsFacingLeft);
            api.AddActor(newAmmo);

            newAmmo = new AmmoPepper();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle + MathF.Rnd.NextFloat(-0.2f, 0.2f), IsFacingLeft);
            api.AddActor(newAmmo);

            weaponCooldown = 36f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.6f;
        }

        private void FireWeaponElectro()
        {
            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            AmmoElectro newAmmo = new AmmoElectro();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            api.AddActor(newAmmo);

            weaponCooldown = 32f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.2f;
        }

        private bool FireWeaponThunderbolt()
        {
            if (isActivelyPushing || inWater || isAttachedToPole || !(canJump || activeModifier != Modifier.None) || MathF.Abs(speedX) > 0.1f || MathF.Abs(speedY) > 0.1f || MathF.Abs(externalForceX) > 0.1f || MathF.Abs(externalForceY) > 0.1f) {
                return false;
            }

            Vector3 initialPos, gunspotPos; float angle;
            GetFirePointAndAngle(out initialPos, out gunspotPos, out angle);

            AmmoThunderbolt newAmmo = new AmmoThunderbolt();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = gunspotPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, Speed, angle, IsFacingLeft);
            api.AddActor(newAmmo);

            controllable = false;
            controllableTimeout = weaponCooldown = 42f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1f;

            fireFramesLeft = 50f;
            return true;
        }
    }
}