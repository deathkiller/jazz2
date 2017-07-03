namespace Jazz2.Actors.Solid
{
    public class PushBox : SolidObjectBase
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            ushort theme = details.Params[0];

            Movable = true;

            RequestMetadata("Object/PushBox");

            switch (theme) {
                case 0: SetAnimation("Rock"); break;
                case 1: SetAnimation("Crate"); break;
            }
        }
    }
}