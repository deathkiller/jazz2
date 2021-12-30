﻿using System.Threading.Tasks;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class BarrelContainer : GenericContainer
    {
        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/BarrelContainer");

            // ToDo: Preload also its content
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new BarrelContainer();
            actor.OnActivated(details);
            return actor;
        }

        private BarrelContainer()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Movable = true;

            CollisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            if (details.Params[0] != 0 && details.Params[1] != 0) {
                GenerateContents((EventType)details.Params[0], details.Params[1], details.Params[2], details.Params[3],
                    details.Params[4], details.Params[5], details.Params[6], details.Params[7]);
            }

            await RequestMetadataAsync("Object/BarrelContainer");
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
            PlaySound(Transform.Pos, "Break");

            CreateParticleDebris();

            CreateSpriteDebris("BarrelShrapnel1", 3);
            CreateSpriteDebris("BarrelShrapnel2", 3);
            CreateSpriteDebris("BarrelShrapnel3", 2);
            CreateSpriteDebris("BarrelShrapnel4", 1);

            return base.OnPerish(collider);
        }
    }
}