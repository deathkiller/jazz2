using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class CrateContainer : GenericContainer
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Movable = true;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions;

            if (details.Params[0] != 0 && details.Params[1] != 0) {
                GenerateContents((EventType)details.Params[0], details.Params[1], details.Params[2], details.Params[3],
                    details.Params[4], details.Params[5], details.Params[6], details.Params[7]);
            }

            RequestMetadata("Object/CrateContainer");
            SetAnimation(AnimState.Idle);
        }


        protected override bool OnPerish(ActorBase collider)
        {
            collisionFlags = CollisionFlags.None;

            CreateParticleDebris();

            PlaySound("Break");

            CreateSpriteDebris("CrateShrapnel1", 3);
            CreateSpriteDebris("CrateShrapnel2", 2);

            SetTransition(AnimState.TransitionDeath, false, delegate {
                base.OnPerish(collider);
            });
            SpawnContent();
            return true;
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