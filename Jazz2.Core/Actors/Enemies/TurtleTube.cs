using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class TurtleTube : EnemyBase
    {
        private const float WaterDifference = -16f;

        private bool onWater;
        private float phase;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            SetHealthByDifficulty(2);
            scoreValue = 200;

            RequestMetadata("Enemy/TurtleTube");
            SetAnimation(AnimState.Idle);

            Vector3 pos = Transform.Pos;
            if (api.WaterLevel + WaterDifference <= pos.Y) {
                // Water is above the enemy, it's floating on the water
                pos.Y = api.WaterLevel + WaterDifference;
                Transform.Pos = pos;

                collisionFlags &= ~CollisionFlags.ApplyGravitation;
                onWater = true;
            } else {
                // Water is below the enemy, apply gravitation and pause the animation
                renderer.AnimPaused = true;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            Vector3 pos = Transform.Pos;

            if (onWater) {
                // Floating on the water
                speedX = MathF.Sin(phase);

                phase += Time.TimeMult * 0.02f;

                if (api.WaterLevel + WaterDifference < pos.Y) {
                    // Water is above the enemy, return the enemy on the surface
                    pos.Y = api.WaterLevel + WaterDifference;
                    Transform.Pos = pos;
                } else if (api.WaterLevel + WaterDifference > pos.Y) {
                    // Water is below the enemy, apply gravitation and pause the animation 
                    speedX = 0f;

                    collisionFlags |= CollisionFlags.ApplyGravitation;
                    onWater = false;
                }
            } else {
                if (api.WaterLevel + WaterDifference <= pos.Y) {
                    // Water is above the enemy, return the enemy on the surface
                    pos.Y = api.WaterLevel + WaterDifference;
                    Transform.Pos = pos;

                    collisionFlags &= ~CollisionFlags.ApplyGravitation;
                    onWater = true;

                    renderer.AnimPaused = false;
                } else {
                    renderer.AnimPaused = true;
                }
            }
        }

        protected override bool OnPerish(ActorBase collider)
        {
            CreateDeathDebris(collider);
            api.PlayCommonSound(this, "Splat");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}