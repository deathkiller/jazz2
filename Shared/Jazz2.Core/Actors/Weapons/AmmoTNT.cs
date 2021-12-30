using System.Threading.Tasks;
using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Environment;
using Jazz2.Actors.Solid;
using Jazz2.Game.Collisions;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Weapons
{
    public partial class AmmoTNT : ActorBase
    {
        private Player owner;

        private float lifetime = 200f;
        private bool isExploded;

        public Player Owner => owner;

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new AmmoTNT();
            actor.OnActivated(details);
            return actor;
        }

        public AmmoTNT()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            CollisionFlags = CollisionFlags.None;

            await RequestMetadataAsync("Weapon/TNT");
            SetAnimation(AnimState.Idle);
        }

        public void OnFire(Player owner)
        {
            this.owner = owner;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (lifetime > 0f) {
                lifetime -= Time.TimeMult;

                if (lifetime > 40f) {
                    Vector3 pos = Transform.Pos;
                    levelHandler.FindCollisionActorsByRadius(pos.X, pos.Y, 50, actor => {
                        if (!actor.IsInvulnerable && (actor is EnemyBase ||
                            actor is AmmoBarrel || actor is AmmoCrate ||
                            actor is BarrelContainer || actor is CrateContainer ||
                            actor is GemBarrel || actor is GemCrate ||
                            actor is PowerUpMorphMonitor || actor is PowerUpShieldMonitor ||
                            actor is PowerUpWeaponMonitor || actor is TriggerCrate ||
                            actor is BirdCage || actor is Pole)) {
                            lifetime = 40f;
                            return false;
                        }
                        return true;
                    });
                }
            } else if (!isExploded) {
                isExploded = true;

                // ToDo: Sound + Animation
                SetTransition(AnimState.TransitionActivate, false, delegate {
                    DecreaseHealth(int.MaxValue);
                });

                PlaySound(Transform.Pos, "Explosion");

                Vector3 pos = Transform.Pos;
                levelHandler.FindCollisionActorsByRadius(pos.X, pos.Y, 50, actor => {
                    actor.OnHandleCollision(this);
                    return true;
                });

                TileMap tiles = levelHandler.TileMap;
                if (tiles != null) {
                    AABB aabb = new AABB(pos.X - 34, pos.Y - 34, pos.X + 34, pos.Y + 34);
                    int destroyedCount = tiles.CheckWeaponDestructible(ref aabb, WeaponType.TNT, 8);
                    if (destroyedCount > 0 && owner != null) {
                        owner.AddScore((uint)(destroyedCount * 50));
                    }
                }
            } else {
                Transform.Scale += Time.TimeMult * 0.02f;
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            //base.OnHandleCollision(other);

            switch (other) {
                case AmmoTNT ammo: {
                    if (lifetime > 40f) {
                        lifetime = 40f;
                    }
                    break;
                }
            }
        }
    }
}