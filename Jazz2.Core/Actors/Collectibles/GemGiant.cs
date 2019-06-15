using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class GemGiant : ActorBase
    {
        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            await RequestMetadataAsync("Object/GemGiant");
            SetAnimation("GemGiant");
        }

        protected override bool OnPerish(ActorBase collider)
        {
            PlaySound("Break");

            Vector3 pos = Transform.Pos;

            for (int i = 0; i < 10; i++) {
                float fx = MathF.Rnd.NextFloat(-18f, 18f);
                float fy = MathF.Rnd.NextFloat(-8f, 0.2f);

                ActorBase actor = api.EventSpawner.SpawnEvent(ActorInstantiationFlags.None, EventType.Gem, pos + new Vector3(fx * 2f, fy * 4f, 10f), new ushort[] { 0 });
                actor.AddExternalForce(fx, fy);
                api.AddActor(actor);
            }

            return base.OnPerish(collider);
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case AmmoBase collision: {
                    DecreaseHealth(collision.Strength, collision);
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
        }
    }
}