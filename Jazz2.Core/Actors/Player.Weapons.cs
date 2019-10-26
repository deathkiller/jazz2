using System;
using Duality;
using Duality.Audio;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;
using MathF = Duality.MathF;

namespace Jazz2.Actors
{
    // ToDo: Show flash from weapon

    partial class Player
    {
        private bool weaponAllowed = true;
        private WeaponType currentWeapon;
        private float weaponCooldown;
        private short[] weaponAmmo;
        private byte[] weaponUpgrades;
        private SoundInstance weaponToasterSound;

        public WeaponType CurrentWeapon => currentWeapon;

        public short[] WeaponAmmo => weaponAmmo;

        public byte[] WeaponUpgrades => weaponUpgrades;

        public bool AddAmmo(WeaponType type, short count)
        {
            const short Multiplier = 100;
            const short AmmoLimit = 99 * Multiplier;

            if (weaponAmmo[(int)type] < 0 || weaponAmmo[(int)type] >= AmmoLimit) {
                return false;
            }

            bool switchTo = (weaponAmmo[(int)type] == 0);

            weaponAmmo[(int)type] = (short)Math.Min(weaponAmmo[(int)type] + count * Multiplier, AmmoLimit);

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

        private bool FireWeapon()
        {
            if (weaponCooldown > 0f) {
                return true;
            }

            // Rewind the animation, if it should be played only once
            if (currentAnimation.OnlyOnce) {
                renderer.AnimTime = 0f;
            }

            short ammoDecrease = 100;

            switch (currentWeapon) {
                case WeaponType.Blaster: FireWeaponBlaster(); break;
                case WeaponType.Bouncer: FireWeaponBouncer(); break;
                case WeaponType.Freezer: FireWeaponFreezer(); break;
                case WeaponType.Seeker: FireWeaponSeeker(); break;
                case WeaponType.RF: FireWeaponRF(); break;

                case WeaponType.Toaster: {
                    if (!FireWeaponToaster()) {
                        return false;
                    }
                    ammoDecrease = 20;
                    break;
                }

                case WeaponType.TNT: FireWeaponTNT(); break;
                case WeaponType.Pepper: FireWeaponPepper(); break;
                case WeaponType.Electro: FireWeaponElectro(); break;

                case WeaponType.Thunderbolt: {
                    if (!FireWeaponThunderbolt()) {
                        return false;
                    }
                    if ((weaponUpgrades[(int)currentWeapon] & 0x1) != 0) {
                        ammoDecrease = 25; // Lower ammo consumption with upgrade
                    } else {
                        ammoDecrease = 50;
                    }
                    break;
                }

                default:
                    return false;
            }

            ref short currentAmmo = ref weaponAmmo[(int)currentWeapon];

            if (currentAmmo > 0) {
                currentAmmo -= ammoDecrease;
                if (currentAmmo < 0) {
                    currentAmmo = 0;
                }

                // No ammo, switch weapons
                if (currentAmmo == 0) {
                    SwitchToNextWeapon();
                    PlaySound("SwitchAmmo");
                }
            }

            return true;
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
                gunspotPos.Y += (MathF.Sin(angle) * size) * (IsFacingLeft ? -1f : 1f) - (currentAnimation.Base.Hotspot.Y - currentAnimation.Base.Gunspot.Y);
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
            GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            AmmoBlaster newAmmo = new AmmoBlaster();
            newAmmo.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            levelHandler.AddActor(newAmmo);

            PlaySound("WeaponBlaster");
            weaponCooldown = 40f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 2f;
        }

        private void FireWeaponBouncer()
        {
            GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            AmmoBouncer newAmmo = new AmmoBouncer();
            newAmmo.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            levelHandler.AddActor(newAmmo);

            weaponCooldown = 32f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.7f;
        }

        private void FireWeaponFreezer()
        {
            GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            if ((weaponUpgrades[(int)currentWeapon] & 0x1) != 0) {
                AmmoFreezer newAmmo = new AmmoFreezer();
                newAmmo.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle - 0.018f, IsFacingLeft);
                levelHandler.AddActor(newAmmo);

                newAmmo = new AmmoFreezer();
                newAmmo.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle + 0.018f, IsFacingLeft);
                levelHandler.AddActor(newAmmo);
            } else {
                AmmoFreezer newAmmo = new AmmoFreezer();
                newAmmo.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
                levelHandler.AddActor(newAmmo);
            }

            weaponCooldown = 46f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.6f;
        }

        private void FireWeaponSeeker()
        {
            GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            AmmoSeeker newAmmo = new AmmoSeeker();
            newAmmo.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            levelHandler.AddActor(newAmmo);

            weaponCooldown = 100f;
        }

        private void FireWeaponRF()
        {
            GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            if ((weaponUpgrades[(int)currentWeapon] & 0x1) != 0) {
                AmmoRF newAmmo = new AmmoRF();
                newAmmo.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle - 0.26f, IsFacingLeft);
                levelHandler.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
                levelHandler.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle + 0.26f, IsFacingLeft);
                levelHandler.AddActor(newAmmo);
            } else {
                AmmoRF newAmmo = new AmmoRF();
                newAmmo.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle - 0.2f, IsFacingLeft);
                levelHandler.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnActivated(new ActorActivationDetails {
                    LevelHandler = levelHandler,
                    Pos = initialPos,
                    Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
                });
                newAmmo.OnFire(this, gunspotPos, Speed, angle + 0.2f, IsFacingLeft);
                levelHandler.AddActor(newAmmo);
            }

            weaponCooldown = 100f;
        }

        private bool FireWeaponToaster()
        {
            if (inWater) {
                return false;
            }

            GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            AmmoToaster newAmmo = new AmmoToaster();
            newAmmo.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            levelHandler.AddActor(newAmmo);

            if (weaponToasterSound == null) {
                weaponToasterSound = PlaySound("WeaponToaster", 0.6f);
                weaponToasterSound.Flags |= SoundInstanceFlags.Looped;
            }

            weaponCooldown = 6f;
            return true;
        }

        private void FireWeaponTNT()
        {
            Vector3 pos = Transform.Pos;

            AmmoTNT tnt = new AmmoTNT();
            tnt.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = pos
            });
            tnt.OnFire(this);
            levelHandler.AddActor(tnt);

            weaponCooldown = 30f;
        }

        private void FireWeaponPepper()
        {
            GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            AmmoPepper newAmmo = new AmmoPepper();
            newAmmo.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle + MathF.Rnd.NextFloat(-0.2f, 0.2f), IsFacingLeft);
            levelHandler.AddActor(newAmmo);

            newAmmo = new AmmoPepper();
            newAmmo.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle + MathF.Rnd.NextFloat(-0.2f, 0.2f), IsFacingLeft);
            levelHandler.AddActor(newAmmo);

            weaponCooldown = 36f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.6f;
        }

        private void FireWeaponElectro()
        {
            GetFirePointAndAngle(out Vector3 initialPos, out Vector3 gunspotPos, out float angle);

            AmmoElectro newAmmo = new AmmoElectro();
            newAmmo.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = initialPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, gunspotPos, Speed, angle, IsFacingLeft);
            levelHandler.AddActor(newAmmo);

            weaponCooldown = 32f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.2f;
        }

        private bool FireWeaponThunderbolt()
        {
            if (isActivelyPushing || inWater || isAttachedToPole || !(canJump || activeModifier != Modifier.None) || MathF.Abs(speedX) > 0.1f || MathF.Abs(speedY) > 0.1f || MathF.Abs(externalForceX) > 0.1f || MathF.Abs(externalForceY) > 0.1f) {
                return false;
            }

            GetFirePointAndAngle(out _, out Vector3 gunspotPos, out float angle);

            AmmoThunderbolt newAmmo = new AmmoThunderbolt();
            newAmmo.OnActivated(new ActorActivationDetails {
                LevelHandler = levelHandler,
                Pos = gunspotPos,
                Params = new ushort[] { weaponUpgrades[(int)currentWeapon] }
            });
            newAmmo.OnFire(this, Speed, angle, IsFacingLeft);
            levelHandler.AddActor(newAmmo);

            controllable = false;
            controllableTimeout = weaponCooldown = 42f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1f;

            fireFramesLeft = 50f;
            return true;
        }
    }
}