using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class TurtleTube : EnemyBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(2);
            scoreValue = 200;

            RequestMetadata("Enemy/TurtleTube");
            SetAnimation(AnimState.Idle);

            // ToDo: Implement better water handling
            Vector3 pos = Transform.Pos;
            if (api.WaterLevel < pos.Y) {
                collisionFlags &= ~CollisionFlags.ApplyGravitation;

                pos.Y = api.WaterLevel - 16;
                Transform.Pos = pos;
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "COMMON_SPLAT");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}