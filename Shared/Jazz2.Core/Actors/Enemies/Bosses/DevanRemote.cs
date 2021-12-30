using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Bosses
{
    public class DevanRemote : BossBase
    {
        private Robot activeRobot;

        private ushort introText, endText;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Boss/DevanRemote");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new DevanRemote();
            actor.OnActivated(details);
            return actor;
        }

        private DevanRemote()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            introText = details.Params[1];
            endText = details.Params[2];

            canBeFrozen = false;
            CollisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.ApplyGravitation;
            IsFacingLeft = true;

            await RequestMetadataAsync("Boss/DevanRemote");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnBossActivated()
        {
            levelHandler.BroadcastLevelText(levelHandler.GetLevelText(introText));

            foreach (GameObject obj in levelHandler.ActiveObjects) {
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

        public override void OnDestroyed()
        {
            if (activeRobot != null) {
                activeRobot.Deactivate();
                activeRobot = null;
            }
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (activeRobot != null) {
                if (activeRobot.Scene == null) {
                    activeRobot = null;

                    health = 0;

                    levelHandler.BroadcastLevelText(levelHandler.GetLevelText(endText));

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