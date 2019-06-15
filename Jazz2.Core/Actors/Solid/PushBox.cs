using System.Threading.Tasks;

namespace Jazz2.Actors.Solid
{
    public class PushBox : SolidObjectBase
    {
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