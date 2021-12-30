using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class TurtleTube : EnemyBase
    {
        private const float WaterDifference = -16f;

        private bool onWater;
        private float phase;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Enemy/TurtleTube");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new TurtleTube();
            actor.OnActivated(details);
            return actor;
        }

        private TurtleTube()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            SetHealthByDifficulty(2);
            scoreValue = 200;

            await RequestMetadataAsync("Enemy/TurtleTube");
            SetAnimation(AnimState.Idle);

            Vector3 pos = Transform.Pos;
            if (levelHandler.WaterLevel + WaterDifference <= pos.Y) {
                // Water is above the enemy, it's floating on the water
                pos.Y = levelHandler.WaterLevel + WaterDifference;
                Transform.Pos = pos;

                CollisionFlags &= ~CollisionFlags.ApplyGravitation;
                onWater = true;
            } else {
                // Water is below the enemy, apply gravitation and pause the animation
                renderer.AnimPaused = true;
            }
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            Vector3 pos = Transform.Pos;

            if (onWater) {
                // Floating on the water
                speedX = MathF.Sin(phase);

                phase += timeMult * 0.02f;

                if (levelHandler.WaterLevel + WaterDifference < pos.Y) {
                    // Water is above the enemy, return the enemy on the surface
                    pos.Y = levelHandler.WaterLevel + WaterDifference;
                    Transform.Pos = pos;
                } else if (levelHandler.WaterLevel + WaterDifference > pos.Y) {
                    // Water is below the enemy, apply gravitation and pause the animation 
                    speedX = 0f;

                    CollisionFlags |= CollisionFlags.ApplyGravitation;
                    onWater = false;
                }
            } else {
                if (levelHandler.WaterLevel + WaterDifference <= pos.Y) {
                    // Water is above the enemy, return the enemy on the surface
                    pos.Y = levelHandler.WaterLevel + WaterDifference;
                    Transform.Pos = pos;

                    CollisionFlags &= ~CollisionFlags.ApplyGravitation;
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
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}