using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class Copter : ActorBase
    {
        private Vector3 originPos;
        private float anglePhase;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Enemy/LizardFloat");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Copter();
            actor.OnActivated(details);
            return actor;
        }

        private Copter()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            await RequestMetadataAsync("Enemy/LizardFloat");
            SetAnimation(AnimState.Activated);

            CollisionFlags &= ~CollisionFlags.ApplyGravitation;

            originPos = details.Pos;
        }

        public override void OnFixedUpdate(float timeMult)
        {
            OnUpdateHitbox();

            anglePhase += timeMult * 0.05f;

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