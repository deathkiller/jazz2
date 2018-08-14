using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class Copter : ActorBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            RequestMetadata("Enemy/LizardFloat");
            SetAnimation(AnimState.Activated);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;
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
