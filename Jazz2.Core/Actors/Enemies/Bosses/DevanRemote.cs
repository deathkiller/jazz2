using Duality;
using Jazz2.Game.Structs;
using static Duality.Component;

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

            canBeFrozen = false;
            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.ApplyGravitation;
            IsFacingLeft = true;

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

                    // Copy health to Devan to enable HealthBar
                    health = activeRobot.Health;
                    maxHealth = activeRobot.MaxHealth;
                    break;
                }
            }
        }

        protected override void OnDeactivated(ShutdownContext context)
        {
            if (activeRobot != null) {
                activeRobot.Deactivate();
                activeRobot = null;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (activeRobot != null) {
                if (activeRobot.ParentScene == null) {
                    activeRobot = null;

                    health = 0;

                    api.BroadcastLevelText(endText);

                    PlaySound("WarpOut");
                    SetTransition(AnimState.TransitionWarpOut, false, delegate {
                        DecreaseHealth(int.MaxValue);
                    });
                } else {
                    health = activeRobot.Health;
                }
            }
        }
    }
}