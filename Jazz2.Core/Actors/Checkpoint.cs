using Jazz2.Game.Structs;

namespace Jazz2.Actors
{
    public class Checkpoint : ActorBase
    {
        private ushort theme;
        private bool activated;

        public override void OnActivated(ActorActivationDetails details)
        {
            base.OnActivated(details);

            canBeFrozen = false;

            theme = details.Params[0];
            activated = (details.Params[1] != 0);

            switch (theme) {
                case 0:
                default:
                    RequestMetadata("Object/Checkpoint");
                    break;

                case 1: // Xmas
                    RequestMetadata("Object/CheckpointXmas");
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
                        foreach (Player p in api.Players) {
                            p.SetCheckpoint(Transform.Pos.Xy);
                        }

                        SetAnimation("Opened");
                        SetTransition(AnimState.TransitionActivate, false);

                        PlaySound("TransitionActivate");

                        // Deactivate event in map
                        api.EventMap.StoreTileEvent(originTile.X, originTile.Y, EventType.Checkpoint, ActorInstantiationFlags.None, new ushort[] { theme, 1 });
                    }
                    break;
                }
            }
        }
    }
}