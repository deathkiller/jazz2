using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Witch : EnemyBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags &= ~CollisionFlags.ApplyGravitation;

            SetHealthByDifficulty(30);
            scoreValue = 1000;

            RequestMetadata("Enemy/Witch");
            SetAnimation(AnimState.Idle);
        }

        // ToDo: Implement this

        protected override bool OnPerish(ActorBase collider)
        {
            api.PlayCommonSound(this, "COMMON_SPLAT");

            SetTransition(AnimState.TransitionDeath, false, delegate {
                base.OnPerish(collider);
            });

            CreateParticleDebris();

            return false;
        }
    }
}