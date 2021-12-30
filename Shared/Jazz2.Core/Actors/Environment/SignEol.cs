using System.Threading.Tasks;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class SignEol : ActorBase
    {
        private ExitType exitType;

        public ExitType ExitType => exitType;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Object/SignEol");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new SignEol();
            actor.OnActivated(details);
            return actor;
        }

        private SignEol()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            exitType = (ExitType)details.Params[0];

            canBeFrozen = false;

            await RequestMetadataAsync("Object/SignEol");
            SetAnimation("SignEol");
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(24, 24);
        }
    }
}