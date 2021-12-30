using System.Threading.Tasks;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class Checkpoint : ActorBase
    {
        private ushort theme;
        private bool activated;

        public static void Preload(ActorActivationDetails details)
        {
            ushort theme = details.Params[0];

            switch (theme) {
                case 0:
                default:
                    PreloadMetadata("Object/Checkpoint");
                    break;

                case 1: // Xmas
                    PreloadMetadata("Object/CheckpointXmas");
                    break;
            }
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Checkpoint();
            actor.OnActivated(details);
            return actor;
        }

        private Checkpoint()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            canBeFrozen = false;

            theme = details.Params[0];
            activated = (details.Params[1] != 0);

            switch (theme) {
                case 0:
                default:
                    await RequestMetadataAsync("Object/Checkpoint");
                    break;

                case 1: // Xmas
                    await RequestMetadataAsync("Object/CheckpointXmas");
                    break;
            }

            SetAnimation(activated ? "Opened" : "Closed");
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(20, 20);
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case Player player: {
                    if (!activated) {
                        activated = true;

                        // Set this checkpoint for all players
                        foreach (Player p in levelHandler.Players) {
                            p.SetCheckpoint(Transform.Pos.Xy);
                        }

                        SetAnimation("Opened");
                        SetTransition(AnimState.TransitionActivate, false);

                        PlaySound("TransitionActivate");

                        // Deactivate event in map
                        levelHandler.EventMap.StoreTileEvent(originTile.X, originTile.Y, EventType.Checkpoint, ActorInstantiationFlags.None, new ushort[] { theme, 1 });

                        if (levelHandler.Difficulty != GameDifficulty.Multiplayer) {
                            levelHandler.EventMap.CreateCheckpointForRollback();
                        }
                    }
                    break;
                }
            }
        }
    }
}