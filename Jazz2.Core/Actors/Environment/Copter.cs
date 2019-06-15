using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class Copter : ActorBase
    {
        private Vector3 originPos;
        private float anglePhase;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await RequestMetadataAsync("Enemy/LizardFloat");
            SetAnimation(AnimState.Activated);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            originPos = details.Pos;
        }

        protected override void OnUpdate()
        {
            OnUpdateHitbox();

            anglePhase += Time.TimeMult * 0.05f;

            Transform.Pos = originPos + new Vector3(0f, MathF.Sin(anglePhase) * 4f, 0f);
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player:
                    if (player.SetModifier(Player.Modifier.LizardCopter)) {
                        DecreaseHealth(int.MaxValue);
                    }
                    break;
            }
        }
    }
}