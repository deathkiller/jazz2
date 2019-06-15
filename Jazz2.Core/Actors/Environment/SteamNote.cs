using System.Threading.Tasks;
using Duality;

namespace Jazz2.Actors.Environment
{
    public class SteamNote : ActorBase
    {
        private float cooldown;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.X -= 10f;
            pos.Y -= 2f;
            Transform.Pos = pos;

            collisionFlags = CollisionFlags.ForceDisableCollisions;

            await RequestMetadataAsync("Object/SteamNote");
            SetAnimation("SteamNote");

            PlaySound("Appear", 0.4f);
        }

        protected override void OnUpdate()
        {
            //base.OnUpdate();

            if (cooldown > 0f) {
                cooldown -= Time.TimeMult;

                if (cooldown <= 0f) {
                    renderer.AnimTime = 0f;
                    renderer.Active = true;

                    PlaySound("Appear", 0.4f);
                }
            }
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(6, 6);
        }

        protected override void OnAnimationFinished()
        {
            renderer.Active = false;
            cooldown = 80f;
        }
    }
}