using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

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

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

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
            //collisionFlags |= CollisionFlags.IsSolidObject;

            RequestMetadata("Object/Pole");

            switch (theme) {
                case 0: SetAnimation("Carrotus"); break;
                case 1: SetAnimation("Diamondus"); break;
                case 2: SetAnimation("DiamondusTree"); break;
                case 3: SetAnimation("Jungle"); break;
                case 4: SetAnimation("Psych"); break;
            }
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

        public override void HandleCollision(ActorBase other)
        {
            //base.HandleCollision(other);

            AmmoBase ammo = other as AmmoBase;
            if (ammo != null) {
                if (fall == FallDirection.None) {
                    fall = (ammo.Speed.X < 0 ? FallDirection.Left : FallDirection.Right);
                }

                ammo.DecreaseHealth(1, this);
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
                Hitbox hitbox = new Hitbox(x - 3, y - 3, x + 7, y + 7);
                if (!api.IsPositionEmpty(this, ref hitbox, true)) {
                    return true;
                }
            }
            // Check radius 2
            {
                float x = pos.X + (rx * Ratio2 * radius);
                float y = pos.Y + (ry * Ratio2 * radius);
                Hitbox hitbox = new Hitbox(x - 3, y - 3, x + 7, y + 7);
                if (!api.IsPositionEmpty(this, ref hitbox, true)) {
                    return true;
                }
            }
            // Check radius 3
            {
                float x = pos.X + (rx * Ratio3 * radius);
                float y = pos.Y + (ry * Ratio3 * radius);
                Hitbox hitbox = new Hitbox(x - 3, y - 3, x + 7, y + 7);
                if (!api.IsPositionEmpty(this, ref hitbox, true)) {
                    return true;
                }
            }
            // Check radius 4
            {
                float x = pos.X + (rx * Ratio4 * radius);
                float y = pos.Y + (ry * Ratio4 * radius);
                Hitbox hitbox = new Hitbox(x - 3, y - 3, x + 7, y + 7);
                if (!api.IsPositionEmpty(this, ref hitbox, true)) {
                    return true;
                }
            }

            return false;
        }
    }
}