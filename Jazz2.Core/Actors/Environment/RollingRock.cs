using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class RollingRock : EnemyBase
    {
        private enum SpeedUpDirection
        {
            None,
            Left,
            Right
        }

        private uint id;
        private float triggerSpeedX, triggerSpeedY;

        private float delayLeft = 40f;
        private bool triggered;
        private SpeedUpDirection speedUpDirection;
        private float speedUpDirectionCooldown;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            id = details.Params[0];
            triggerSpeedX = details.Params[1];
            triggerSpeedY = details.Params[2];

            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors;
            elasticity = 0.4f;
            canHurtPlayer = false;
            isInvulnerable = true;

            await RequestMetadataAsync("Object/RollingRock");

            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            float timeMult = Time.TimeMult;

            // Movement
            if ((collisionFlags & CollisionFlags.ApplyGravitation) != 0) {
                float currentGravity = api.Gravity;

                speedX = MathF.Clamp(speedX + externalForceX * timeMult, -16f, 16f);
                speedY = MathF.Clamp(speedY - (internalForceY + externalForceY) * timeMult, -16f, 16f);

                float effectiveSpeedX = speedX * timeMult;
                float effectiveSpeedY = speedY * timeMult;

                bool success = false;

                float maxYDiff = MathF.Max(3.0f, MathF.Abs(effectiveSpeedX) + 2.5f);
                float yDiff;
                for (yDiff = maxYDiff + effectiveSpeedY; yDiff >= -maxYDiff + effectiveSpeedY; yDiff -= CollisionCheckStep) {
                    if (MoveInstantly(new Vector2(effectiveSpeedX, yDiff), MoveType.Relative)) {
                        success = true;
                        break;
                    }
                }

                // Also try to move horizontally as far as possible
                float xDiff = MathF.Abs(effectiveSpeedX);
                float maxXDiff = -xDiff;
                if (!success) {
                    int sign = (effectiveSpeedX > 0f ? 1 : -1);
                    for (; xDiff >= maxXDiff; xDiff -= CollisionCheckStep) {
                        if (MoveInstantly(new Vector2(xDiff * sign, 0f), MoveType.Relative)) {
                            break;
                        }
                    }

                    // If no angle worked in the previous step, the actor is facing a wall
                    if (xDiff > CollisionCheckStep || (xDiff > 0f && elasticity > 0f)) {
                        speedX = -(elasticity * speedX);
                        speedUpDirection = SpeedUpDirection.None;
                        speedUpDirectionCooldown = 60f;
                    }
                }

                if (yDiff > 0f) {
                    yDiff = MathF.Min(8f, yDiff * yDiff);
                }

                speedX += MathF.Sign(speedX) * (yDiff * 0.02f - 0.02f) * timeMult;

                if (speedY > 0f && yDiff <= 0f) {
                    speedY = 0f;
                }

                if (MathF.Abs(speedX) < 0.4f) {
                    if (speedUpDirectionCooldown > 0f) {
                        speedUpDirection = SpeedUpDirection.None;
                        speedX = 0f;
                        speedY = 0f;
                    } else if (speedUpDirection == SpeedUpDirection.Left) {
                        speedX = -0.6f;
                        speedUpDirection = SpeedUpDirection.None;
                        speedUpDirectionCooldown = 60f;
                    } else if (speedUpDirection == SpeedUpDirection.Right) {
                        speedX = 0.6f;
                        speedUpDirection = SpeedUpDirection.None;
                        speedUpDirectionCooldown = 60f;
                    }
                } else if (yDiff < -1f) {
                    if (speedX > 0f && speedUpDirection != SpeedUpDirection.Left) {
                        speedUpDirection = SpeedUpDirection.Left;
                    } else if (speedX < 0f && speedUpDirection != SpeedUpDirection.Right) {
                        speedUpDirection = SpeedUpDirection.Right;
                    }
                } else {
                    speedUpDirectionCooldown -= timeMult;
                }

                // Reduce all forces if they are present
                if (MathF.Abs(externalForceX) > float.Epsilon) {
                    if (externalForceX > 0f) {
                        externalForceX = MathF.Max(externalForceX - friction * timeMult, 0f);
                    } else {
                        externalForceX = MathF.Min(externalForceX + friction * timeMult, 0f);
                    }
                }
                externalForceY = MathF.Max(externalForceY - currentGravity * 0.33f * timeMult, 0f);
                internalForceY = MathF.Max(internalForceY - currentGravity * 0.33f * timeMult, 0f);

                Transform.Angle += speedX * 0.02f * timeMult;

                if (MathF.Abs(speedX) <= 0.4f && MathF.Abs(speedY) <= 0.4f) {
                    collisionFlags = CollisionFlags.None;
                    canHurtPlayer = false;
                }
            }

            // Trigger
            if (triggered && delayLeft > 0f) {
                delayLeft -= timeMult;

                if (delayLeft <= 0f) {
                    collisionFlags |= CollisionFlags.ApplyGravitation;
                    canHurtPlayer = true;

                    externalForceX = triggerSpeedX * 0.5f;
                    externalForceY = triggerSpeedY * -0.5f;
                }
                return;
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