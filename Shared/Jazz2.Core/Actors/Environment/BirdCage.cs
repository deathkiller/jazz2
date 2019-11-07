using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Environment
{
    public class BirdCage : ActorBase
    {
        private ushort type;
        private bool activated;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.Z -= 50f;
            Transform.Pos = pos;

            type = details.Params[0];
            activated = (details.Params[1] != 0);

            switch (type) {
                case 0: // Chuck (red)
                    await RequestMetadataAsync("Object/BirdCageChuck");
                    PreloadMetadata("Object/BirdChuck");
                    break;
                case 1: // Birdy (yellow)
                    await RequestMetadataAsync("Object/BirdCageBirdy");
                    PreloadMetadata("Object/BirdBirdy");
                    break;
            }
            
            SetAnimation(activated ? AnimState.Activated : AnimState.Idle);

            CollisionFlags |= CollisionFlags.CollideWithSolidObjects;
        }

        public override void OnHandleCollision(ActorBase other)
        {
            switch (other) {
                case AmmoBase ammo: {
                    Player player = ammo.Owner;
                    if (!activated && player != null) {
                        ApplyToPlayer(player);

                        ammo.DecreaseHealth(int.MaxValue);
                    }
                    break;
                }

                case AmmoTNT ammo: {
                    Player player = ammo.Owner;
                    if (!activated && player != null) {
                        ApplyToPlayer(player);
                    }
                    break;
                }

                case Player player: {
                    if (!activated && player.CanBreakSolidObjects) {
                        ApplyToPlayer(player);
                    }
                    break;
                }
            }
        }

        private void ApplyToPlayer(Player player)
        {
            if (!player.SpawnBird(type, Transform.Pos)) {
                return;
            }

            activated = true;
            SetAnimation(AnimState.Activated);
            
            Explosion.Create(levelHandler, Transform.Pos + new Vector3(-12f, -6f, -20f), Explosion.SmokeBrown);
            Explosion.Create(levelHandler, Transform.Pos + new Vector3(-8f, 28f, -20f), Explosion.SmokeBrown);
            Explosion.Create(levelHandler, Transform.Pos + new Vector3(12f, 10f, -20f), Explosion.SmokeBrown);

            Explosion.Create(levelHandler, Transform.Pos + new Vector3(0f, 12f, -22f), Explosion.SmokePoof);

            // Deactivate event in map
            levelHandler.EventMap.StoreTileEvent(originTile.X, originTile.Y, EventType.BirdCage, ActorInstantiationFlags.None, new ushort[] { type, 1 });
        }
    }
}