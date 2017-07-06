using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class DevanRemote : BossBase
    {
        private Robot activeRobot;

        private ushort introText, endText;

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            introText = details.Params[1];
            endText = details.Params[2];

            maxHealth = int.MaxValue;

            canBeFrozen = false;
            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.ApplyGravitation;
            isFacingLeft = true;

            RequestMetadata("Boss/DevanRemote");
            SetAnimation(AnimState.Idle);
        }

        public override void OnBossActivated()
        {
            api.BroadcastLevelText(introText);

            foreach (GameObject obj in api.ActiveObjects) {
                activeRobot = obj as Robot;
                if (activeRobot != null) {
                    activeRobot.Activate();
                    break;
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (activeRobot != null && activeRobot.ParentScene == null) {
                api.BroadcastLevelText(endText);

                PlaySound("WARP_OUT");
                SetTransition(AnimState.TransitionWarpOut, false, delegate {
                    DecreaseHealth(int.MaxValue);
                });
            }
        }
    }
}