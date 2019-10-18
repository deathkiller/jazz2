using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;
using static Duality.Component;

namespace Jazz2.Actors.Bosses
{
    public class DevanRemote : BossBase
    {
        private Robot activeRobot;

        private ushort introText, endText;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            introText = details.Params[1];
            endText = details.Params[2];

            canBeFrozen = false;
            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.ApplyGravitation;
            IsFacingLeft = true;

            await RequestMetadataAsync("Boss/DevanRemote");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnBossActivated()
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

        protected override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (activeRobot != null) {
                if (activeRobot.Scene == null) {
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