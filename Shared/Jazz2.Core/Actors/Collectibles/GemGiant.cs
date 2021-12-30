using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Collectibles
{
    public class GemGiant : ActorBase
    {
        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/GemGiant");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new GemGiant();
            actor.OnActivated(details);
            return actor;
        }

        private GemGiant()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await base.OnActivatedAsync(details);

            CollisionFlags &= ~CollisionFlags.ApplyGravitation;

            await RequestMetadataAsync("Object/GemGiant");
            SetAnimation("GemGiant");
        }

        protected override bool OnPerish(ActorBase collider)
        {
            PlaySound(Transform.Pos, "Break");

            Vector3 pos = Transform.Pos;

            for (int i = 0; i < 10; i++) {
                float fx = MathF.Rnd.NextFloat(-18f, 18f);
                float fy = MathF.Rnd.NextFloat(-8f, 0.2f);

                ActorBase actor = levelHandler.EventSpawner.SpawnEvent(EventType.Gem, new ushort[] { 0 }, ActorInstantiationFlags.None, pos + new Vector3(fx * 2f, fy * 4f, 10f));
                actor.AddExternalForce(fx, fy);
                levelHandler.AddActor(actor);
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