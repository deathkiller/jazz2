using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class GemGiant : ActorBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            RequestMetadata("Object/GemGiant");
            SetAnimation(AnimState.Idle);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            PlaySound("Break");

            Vector3 pos = Transform.Pos;

            for (int i = 0; i < 10; i++) {
                float fx = MathF.Rnd.NextFloat(-18f, 18f);
                float fy = MathF.Rnd.NextFloat(-8f, 0.2f);

                ActorBase actor = api.CreateActor(EventType.Gem, pos.X + fx * 2f, pos.Y + fy * 4f, pos.Z + 10, new ushort[] { 0 }, true);
                actor.AddExternalForce(fx, fy);
                api.AddActor(actor);
            }

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