using System.Threading.Tasks;
using Jazz2.Actors.Solid;

namespace Jazz2.Actors
{
    public class FrozenBlock : SolidObjectBase
    {
        private float timeLeft = 250f;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            CollisionFlags = CollisionFlags.CollideWithOtherActors;
            canBeFrozen = false;

            await RequestMetadataAsync("Object/FrozenBlock");
            SetAnimation("FrozenBlock");
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            timeLeft -= timeMult;
            if (timeLeft <= 0) {
                DecreaseHealth(int.MaxValue);
            }
        }
    }
}