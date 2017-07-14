using System;
using System.Collections.Generic;
using System.Text;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class BirdyCage : ActorBase
    {
        private bool activated;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            ushort type = details.Params[0];
            activated = (details.Params[1] != 0);

            switch (type) {
                case 0: // Normal
                    RequestMetadata("Object/BirdyCage");
                    break;
                case 1: // Yellow
                    RequestMetadata("Object/BirdyYellowCage");
                    break;
            }
            
            SetAnimation(activated ? AnimState.Activated : AnimState.Idle);

            //collisionFlags &= ~CollisionFlags.ApplyGravitation;

            collisionFlags |= CollisionFlags.SkipPerPixelCollisions | CollisionFlags.IsSolidObject | CollisionFlags.CollideWithSolidObjects;
        }

        // ToDo: Implement this
    }
}