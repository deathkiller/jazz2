using Duality;
using Jazz2.Actors.Weapons;

namespace Jazz2.Actors.Environment
{
    public class Pole : ActorBase
    {
        private bool falling;
        private float angleVel;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            ushort theme = details.Params[0];
            // ToDo: x or y ??? something's wrong with this... leads to misalignment
            short y = unchecked((short)(details.Params[1] + 10));
            short x = unchecked((short)(details.Params[2] + 10));

            Vector3 pos = Transform.Pos;
            pos.X += 24 - x;
            pos.Y += 10 + y;
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

        // ToDo: Implement Poles, collisions, hit by bullet, falling

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (falling) {
                angleVel += Time.TimeMult * 0.006f;
                if (Transform.Angle > MathF.PiOver2) {
                    falling = false;
                } else {
                    Transform.Angle += angleVel;
                }
            }
        }

        public override void HandleCollision(ActorBase other)
        {
            //base.HandleCollision(other);

            if (falling) {
                return;
            }

            AmmoBase ammo = other as AmmoBase;
            if (ammo != null) {
                falling = true;
            }
        }
    }
}