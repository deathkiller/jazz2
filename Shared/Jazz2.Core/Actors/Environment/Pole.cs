using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Collisions;

namespace Jazz2.Actors.Environment
{
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
        private float fallTime;
        private int bouncesLeft = BouncesMax;

        public static void Preload(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];

            switch (theme) {
                default:
                case 0: PreloadMetadata("Pole/Carrotus"); break;
                case 1: PreloadMetadata("Pole/Diamondus"); break;
                case 2: PreloadMetadata("Pole/DiamondusTree"); break;
                case 3: PreloadMetadata("Pole/Jungle"); break;
                case 4: PreloadMetadata("Pole/Psych"); break;
            }
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Pole();
            actor.OnActivated(details);
            return actor;
        }

        private Pole()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];
            short x = unchecked((short)(details.Params[1]));
            short y = unchecked((short)(details.Params[2]));

            Vector3 pos = Transform.Pos;
            pos.X += x;
            pos.Y += y;
            pos.Z += 20;
            Transform.Pos = pos;

            canBeFrozen = false;
            CollisionFlags &= ~CollisionFlags.ApplyGravitation;

            bool isSolid = true;
            switch (theme) {
                default:
                case 0: await RequestMetadataAsync("Pole/Carrotus"); break;
                case 1: await RequestMetadataAsync("Pole/Diamondus"); break;
                case 2: await RequestMetadataAsync("Pole/DiamondusTree"); isSolid = false; break;
                case 3: await RequestMetadataAsync("Pole/Jungle"); break;
                case 4: await RequestMetadataAsync("Pole/Psych"); break;
            }

            if (isSolid) {
                CollisionFlags |= CollisionFlags.IsSolidObject;
            }

            SetAnimation("Pole");
        }

        public override void OnFixedUpdate(float timeMult)
        {
            const float FallMultiplier = 0.0036f;
            const float Bounce = -0.2f;

            base.OnFixedUpdate(timeMult);

            if (fall != FallDirection.Left && fall != FallDirection.Right) {
                return;
            }

            fallTime += timeMult;

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
                        if (fallTime < 10) {
                            CollisionFlags &= ~CollisionFlags.IsSolidObject;
                        }
                    }
                } else {
                    angleVel += FallMultiplier * timeMult;
                    Transform.Angle += angleVel * timeMult;
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
                        if (fallTime < 10) {
                            CollisionFlags &= ~CollisionFlags.IsSolidObject;
                        }
                    }
                } else {
                    angleVel -= FallMultiplier * timeMult;
                    Transform.Angle += angleVel * timeMult;
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            //base.HandleCollision(other);

            switch (other) {
                case AmmoBase ammo: {
                    Fall(ammo.Speed.X < 0 ? FallDirection.Left : FallDirection.Right);
                    ammo.DecreaseHealth(1, this);
                    break;
                }

                case AmmoTNT ammo: {
                    Fall(ammo.Transform.Pos.X > Transform.Pos.X ? FallDirection.Left : FallDirection.Right);
                    break;
                }
            }
        }

        private void Fall(FallDirection dir)
        {
            if (fall != FallDirection.None) {
                return;
            }

            fall = dir;
            isInvulnerable = true;
            CollisionFlags |= CollisionFlags.IsSolidObject;
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

            if (fallTime > 20) {
                // Check radius 1
                {
                    float x = pos.X + (rx * Ratio1 * radius);
                    float y = pos.Y + (ry * Ratio1 * radius);
                    AABB aabb = new AABB(x - 3, y - 3, x + 7, y + 7);
                    if (!levelHandler.IsPositionEmpty(this, ref aabb, true)) {
                        return true;
                    }
                }
                // Check radius 2
                {
                    float x = pos.X + (rx * Ratio2 * radius);
                    float y = pos.Y + (ry * Ratio2 * radius);
                    AABB aabb = new AABB(x - 3, y - 3, x + 7, y + 7);
                    if (!levelHandler.IsPositionEmpty(this, ref aabb, true)) {
                        return true;
                    }
                }
            }
            // Check radius 3
            {
                float x = pos.X + (rx * Ratio3 * radius);
                float y = pos.Y + (ry * Ratio3 * radius);
                AABB aabb = new AABB(x - 3, y - 3, x + 7, y + 7);
                if (!levelHandler.IsPositionEmpty(this, ref aabb, true)) {
                    return true;
                }
            }
            // Check radius 4
            {
                float x = pos.X + (rx * Ratio4 * radius);
                float y = pos.Y + (ry * Ratio4 * radius);
                AABB aabb = new AABB(x - 3, y - 3, x + 7, y + 7);
                if (!levelHandler.IsPositionEmpty(this, ref aabb, true)) {
                    return true;
                }
            }

            return false;
        }
    }
}