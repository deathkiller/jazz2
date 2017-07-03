using Duality;

namespace Jazz2.Actors.Environment
{
    public class Moth : ActorBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            Vector3 pos = Transform.Pos;
            pos.Z += 20f;
            Transform.Pos = pos;

            ushort theme = details.Params[0];

            RequestMetadata("Object/Moth");

            switch (theme) {
                default:
                case 0: SetAnimation("Pink"); break;
                case 1: SetAnimation("Gray"); break;
                case 2: SetAnimation("Green"); break;
                case 3: SetAnimation("Purple"); break;
            }
        }

        // ToDo: Implement movement
    }
}