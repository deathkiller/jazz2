using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class RollingRock : EnemyBase
    {
        private uint id;
        private float triggerSpeedX, triggerSpeedY;
        private float lastY;

        private float delayLeft = 40f;
        private bool triggered;
        private float deceleration;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            id = details.Params[0];
            triggerSpeedX = details.Params[1];
            triggerSpeedY = details.Params[2];

            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors;
            elasticity = 0.4f;
            canHurtPlayer = false;

            RequestMetadata("Object/RollingRock");

            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            float timeMult = Time.TimeMult;
            if (triggered && delayLeft > 0f) {
                delayLeft -= timeMult;

                if (delayLeft <= 0f) {
                    collisionFlags |= CollisionFlags.ApplyGravitation;
                    canHurtPlayer = true;

                    speedX = triggerSpeedX;
                    speedY = triggerSpeedY;
                }
                return;
            }

            if (MathF.Abs(speedX) > float.Epsilon) {
                if (lastY - 4f > Transform.Pos.Y && (speedY >= 0 || lastY + speedY * timeMult - 4f > Transform.Pos.Y)) {
                    speedX = 0f;
                } else {
                    speedX = MathF.Max((MathF.Abs(speedX) - deceleration * timeMult), 0) * (speedX < 0 ? -1 : 1);
                    deceleration += timeMult * 0.0001f;
                    lastY = Transform.Pos.Y;

                    Transform.Angle += speedX * timeMult * 0.02f;
                }
            } else {
                canHurtPlayer = false;
            }
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(30, 30);
        }

        public override void OnTriggeredEvent(EventType eventType, ushort[] eventParams)
        {
            if (!triggered && eventType == EventType.RollingRockTrigger && eventParams[0] == id) {
                triggered = true;
            }
        }
    }
}