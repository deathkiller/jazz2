using System.Threading.Tasks;
using Duality;

namespace Jazz2.Actors.Environment
{
    public class Bomb : ActorBase
    {
        private float timeLeft = MathF.Rnd.NextFloat(40f, 90f);

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];
            IsFacingLeft = (details.Params[1] != 0);

            health = int.MaxValue;
            elasticity = 0.3f;

            switch (theme) {
                case 0: await RequestMetadataAsync("Object/Bomb"); break;
                case 1: await RequestMetadataAsync("Enemy/LizardFloat"); break;
                case 2: await RequestMetadataAsync("Enemy/LizardFloatXmas"); break;
            }

            SetAnimation("Bomb");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (timeLeft > 0f) {
                timeLeft -= Time.TimeMult;
            } else {
                DecreaseHealth(int.MaxValue);
            }
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(6, 6);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            Vector3 pos = Transform.Pos;

            api.FindCollisionActorsByRadius(pos.X, pos.Y, 40, actor => {
                Player player = actor as Player;
                if (player != null) {
                    bool pushLeft = (pos.X > player.Transform.Pos.X);
                    player.TakeDamage(1, pushLeft ? -8f : 8f);
                }
                return true;
            });

            // Explosion.Large is the same as Explosion.Bomb
            Explosion.Create(api, pos, Explosion.Large);

            api.PlayCommonSound(this, "Bomb");

            return base.OnPerish(collider);
        }
    }
}