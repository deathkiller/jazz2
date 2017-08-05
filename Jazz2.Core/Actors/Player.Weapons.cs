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

        public int[] WeaponAmmo => weaponAmmo;

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
                attachedHud?.ChangeCurrentWeapon(currentWeapon, weaponUpgrades[(int)currentWeapon]);
            }

            PlaySound("PickupAmmo");
            return true;
        }

        public void AddWeaponUpgrade(WeaponType type, byte upgrade)
        {
            weaponUpgrades[(int)type] |= upgrade;

            attachedHud.ChangeCurrentWeapon(currentWeapon, weaponUpgrades[(int)currentWeapon]);
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

        private void FireWeapon()
        {
            if (weaponCooldown > 0f) {
                return;
            }

            switch (currentWeapon) {
                case WeaponType.Blaster:
                    FireWeaponBlaster();
                    break;

                case WeaponType.Bouncer:
                    FireWeaponBouncer();
                    break;

                case WeaponType.Freezer:
                    FireWeaponFreezer();
                    break;

                case WeaponType.Seeker:
                    FireWeaponSeeker();
                    break;

                case WeaponType.RF:
                    FireWeaponRF();
                    break;

                case WeaponType.Toaster:
                    FireWeaponToaster();
                    break;

                case WeaponType.TNT:
                    FireWeaponTNT();
                    break;

                case WeaponType.Pepper:
                    FireWeaponPepper();
                    break;

                case WeaponType.Electro:
                    FireWeaponElectro();
                    break;

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
                    for (int i = 0; i < (int)WeaponType.Count && weaponAmmo[(int)currentWeapon] == 0; i++) {
                        currentWeapon = (WeaponType)((int)(currentWeapon + 1) % (int)WeaponType.Count);
                    }

                    attachedHud?.ChangeCurrentWeapon(currentWeapon, weaponUpgrades[(int)currentWeapon]);

                    PlaySound("SwitchAmmo");
                }
            }
        }

        private void GetFirePointAndAngle(out Vector3 pos, out float angle)
        {
            pos = Transform.Pos;

            if (inWater) {
                angle = Transform.Angle;

                int size = (currentAnimation.FrameDimensions.X / 2);
                pos.X += (MathF.Cos(angle) * size) * (isFacingLeft ? -1f : 1f);
                pos.Y += (MathF.Sin(angle) * size) * (isFacingLeft ? -1f : 1f);
                
            } else {
                angle = ((currentAnimationState & AnimState.Lookup) > 0 ? MathF.PiOver2 * (isFacingLeft ? 1 : -1) : 0f);

                pos.X += (currentAnimation.Hotspot.X - currentAnimation.Gunspot.X) * (isFacingLeft ? 1 : -1);
                pos.Y -= (currentAnimation.Hotspot.Y - currentAnimation.Gunspot.Y);
            }

            pos.Z += 2f;
        }

        private void FireWeaponBlaster()
        {
            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            AmmoBlaster newAmmo = new AmmoBlaster();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            newAmmo.OnFire(this, Speed, angle, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
            api.AddActor(newAmmo);

            PlaySound("WeaponBlaster");
            weaponCooldown = 40f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 2f;
        }

        private void FireWeaponBouncer()
        {
            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            AmmoBouncer newAmmo = new AmmoBouncer();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            newAmmo.OnFire(this, Speed, angle, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
            api.AddActor(newAmmo);

            weaponCooldown = 32f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.7f;
        }

        private void FireWeaponFreezer()
        {
            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            if ((weaponUpgrades[(int)currentWeapon] & 0x1) != 0) {
                AmmoFreezer newAmmo = new AmmoFreezer();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = pos
                });
                newAmmo.OnFire(this, Speed, angle - 0.018f, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
                api.AddActor(newAmmo);

                newAmmo = new AmmoFreezer();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = pos
                });
                newAmmo.OnFire(this, Speed, angle + 0.018f, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
                api.AddActor(newAmmo);
            } else {
                AmmoFreezer newAmmo = new AmmoFreezer();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = pos
                });
                newAmmo.OnFire(this, Speed, angle, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
                api.AddActor(newAmmo);
            }

            weaponCooldown = 46f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.6f;
        }

        private void FireWeaponSeeker()
        {
            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            AmmoSeeker newAmmo = new AmmoSeeker();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            newAmmo.OnFire(this, Speed, angle, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
            api.AddActor(newAmmo);

            weaponCooldown = 46f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.4f;
        }

        private void FireWeaponRF()
        {
            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            if ((weaponUpgrades[(int)currentWeapon] & 0x1) != 0) {
                AmmoRF newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = pos
                });
                newAmmo.OnFire(this, Speed, angle - 0.26f, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
                api.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = pos
                });
                newAmmo.OnFire(this, Speed, angle, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
                api.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = pos
                });
                newAmmo.OnFire(this, Speed, angle + 0.26f, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
                api.AddActor(newAmmo);
            } else {
                AmmoRF newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = pos
                });
                newAmmo.OnFire(this, Speed, angle - 0.2f, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
                api.AddActor(newAmmo);

                newAmmo = new AmmoRF();
                newAmmo.OnAttach(new ActorInstantiationDetails {
                    Api = api,
                    Pos = pos
                });
                newAmmo.OnFire(this, Speed, angle + 0.2f, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
                api.AddActor(newAmmo);
            }

            weaponCooldown = 34f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.4f;
        }

        private void FireWeaponToaster()
        {
            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            AmmoToaster newAmmo = new AmmoToaster();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            newAmmo.OnFire(this, Speed, angle, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
            api.AddActor(newAmmo);

            //PlaySound("WeaponToaster", 0.6f);

            weaponCooldown = 6f;
        }

        private void FireWeaponTNT()
        {
            Vector3 pos = Transform.Pos;

            AmmoTNT tnt = new AmmoTNT(this);
            tnt.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            api.AddActor(tnt);

            weaponCooldown = 30f;
        }

        private void FireWeaponPepper()
        {
            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            AmmoPepper newAmmo = new AmmoPepper();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            newAmmo.OnFire(this, Speed, angle + MathF.Rnd.NextFloat(-0.2f, 0.2f), isFacingLeft, weaponUpgrades[(int)currentWeapon]);
            api.AddActor(newAmmo);

            newAmmo = new AmmoPepper();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            newAmmo.OnFire(this, Speed, angle + MathF.Rnd.NextFloat(-0.2f, 0.2f), isFacingLeft, weaponUpgrades[(int)currentWeapon]);
            api.AddActor(newAmmo);

            weaponCooldown = 36f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.6f;
        }

        private void FireWeaponElectro()
        {
            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            AmmoElectro newAmmo = new AmmoElectro();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            newAmmo.OnFire(this, Speed, angle, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
            api.AddActor(newAmmo);

            weaponCooldown = 32f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1.2f;
        }

        private bool FireWeaponThunderbolt()
        {
            if (isActivelyPushing || inWater || isAttachedToPole || !(canJump || isAirboard) || MathF.Abs(speedX) > 0.1f || MathF.Abs(speedY) > 0.1f || MathF.Abs(externalForceX) > 0.1f || MathF.Abs(externalForceY) > 0.1f) {
                return false;
            }

            Vector3 pos; float angle;
            GetFirePointAndAngle(out pos, out angle);

            AmmoThunderbolt newAmmo = new AmmoThunderbolt();
            newAmmo.OnAttach(new ActorInstantiationDetails {
                Api = api,
                Pos = pos
            });
            newAmmo.OnFire(this, Speed, angle, isFacingLeft, weaponUpgrades[(int)currentWeapon]);
            api.AddActor(newAmmo);

            controllable = false;
            controllableTimeout = weaponCooldown = 42f - (weaponUpgrades[(int)WeaponType.Blaster] >> 1) * 1f;

            fireFramesLeft = 50f;
            return true;
        }
    }
}