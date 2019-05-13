using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Collisions;

namespace Jazz2.Actors.Environment
{
    // ToDo: Implement collision with player
    public class Pole : ActorBase
    {
        private enum FallDirection
        {
            None,
            Right,
            Left,
            Fallen
        }

        private const int BouncesMax = 3;

        private FallDirection fall;
        private float angleVel, angleVelLast;
        private int bouncesLeft = BouncesMax;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            ushort theme = details.Params[0];
            short x = unchecked((short)(details.Params[1]));
            short y = unchecked((short)(details.Params[2]));

            Vector3 pos = Transform.Pos;
            pos.X += x;
            pos.Y += y;
            pos.Z += 20;
            Transform.Pos = pos;

            canBeFrozen = false;
            collisionFlags &= ~CollisionFlags.ApplyGravitation;
            collisionFlags |= CollisionFlags.IsSolidObject;

            switch (theme) {
                default:
                case 0: RequestMetadata("Pole/Carrotus"); break;
                case 1: RequestMetadata("Pole/Diamondus"); break;
                case 2: RequestMetadata("Pole/DiamondusTree"); break;
                case 3: RequestMetadata("Pole/Jungle"); break;
                case 4: RequestMetadata("Pole/Psych"); break;
            }

            SetAnimation("Pole");
        }

        protected override void OnUpdate()
        {
            const float FallMultiplier = 0.0036f;
            const float Bounce = -0.2f;

            base.OnUpdate();

            if (fall == FallDirection.Right) {
                if (angleVel > 0 && IsPositionBlocked()) {
                    if (bouncesLeft > 0) {
                        if (bouncesLeft == BouncesMax) {
                            angleVelLast = angleVel;
                        }

                        bouncesLeft--;
                        angleVel = Bounce * bouncesLeft * angleVelLast;
                    } else {
                        fall = FallDirection.Fallen;
                    }
                } else {
                    angleVel += FallMultiplier * Time.TimeMult;
                    Transform.Angle += angleVel * Time.TimeMult;
                }
            } else if (fall == FallDirection.Left) {
                if (angleVel < 0 && IsPositionBlocked()) {
                    if (bouncesLeft > 0) {
                        if (bouncesLeft == BouncesMax) {
                            angleVelLast = angleVel;
                        }

                        bouncesLeft--;
                        angleVel = Bounce * bouncesLeft * angleVelLast;
                    } else {
                        fall = FallDirection.Fallen;
                    }
                } else {
                    angleVel -= FallMultiplier * Time.TimeMult;
                    Transform.Angle += angleVel * Time.TimeMult;
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            //base.HandleCollision(other);

            switch (other) {
                case AmmoBase ammo: {
                    if (fall == FallDirection.None) {
                        fall = (ammo.Speed.X < 0 ? FallDirection.Left : FallDirection.Right);
                        isInvulnerable = true;
                    }

                    ammo.DecreaseHealth(1, this);
                    break;
                }

                case AmmoTNT ammo: {
                    if (fall == FallDirection.None) {
                        fall = (ammo.Speed.X < 0 ? FallDirection.Left : FallDirection.Right);
                        isInvulnerable = true;
                    }
                    break;
                }
            }
        }

        private bool IsPositionBlocked()
        {
            const float Ratio1 = 0.96f;
            const float Ratio2 = 0.8f;
            const float Ratio3 = 0.6f;
            const float Ratio4 = 0.3f;

            Vector3 pos = Transform.Pos;
            float angle = Transform.Angle - MathF.PiOver2;
            float rx = MathF.Cos(angle);
            float ry = MathF.Sin(angle);
            float radius = currentAnimation.Base.FrameDimensions.Y;

            // Check radius 1
            {
                float x = pos.X + (rx * Ratio1 * radius);
                float y = pos.Y + (ry * Ratio1 * radius);
                AABB aabb = new AABB(x - 3, y - 3, x + 7, y + 7);
                if (!api.IsPositionEmpty(this, ref aabb, true)) {
                    return true;
                }
            }
            // Check radius 2
            {
                float x = pos.X + (rx * Ratio2 * radius);
                float y = pos.Y + (ry * Ratio2 * radius);
                AABB aabb = new AABB(x - 3, y - 3, x + 7, y + 7);
                if (!api.IsPositionEmpty(this, ref aabb, true)) {
                    return true;
                }
            }
            // Check radius 3
            {
                float x = pos.X + (rx * Ratio3 * radius);
                float y = pos.Y + (ry * Ratio3 * radius);
                AABB aabb = new AABB(x - 3, y - 3, x + 7, y + 7);
                if (!api.IsPositionEmpty(this, ref aabb, true)) {
                    return true;
                }
            }
            // Check radius 4
            {
                float x = pos.X + (rx * Ratio4 * radius);
                float y = pos.Y + (ry * Ratio4 * radius);
                AABB aabb = new AABB(x - 3, y - 3, x + 7, y + 7);
                if (!api.IsPositionEmpty(this, ref aabb, true)) {
                    return true;
                }
            }

            return false;
        }
    }
}