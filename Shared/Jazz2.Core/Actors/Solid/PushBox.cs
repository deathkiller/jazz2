using System.Threading.Tasks;

namespace Jazz2.Actors.Solid
{
    public class PushBox : SolidObjectBase
    {
        public static void Preload(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];

            switch (theme) {
                default:
                case 0: PreloadMetadata("Object/PushBoxRock"); break;
                case 1: PreloadMetadata("Object/PushBoxCrate"); break;
            }
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new PushBox();
            actor.OnActivated(details);
            return actor;
        }

        private PushBox()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];

            Movable = true;

            switch (theme) {
                default:
                case 0: await RequestMetadataAsync("Object/PushBoxRock"); break;
                case 1: await RequestMetadataAsync("Object/PushBoxCrate"); break;
            }

            SetAnimation("PushBox");
        }
    }
}