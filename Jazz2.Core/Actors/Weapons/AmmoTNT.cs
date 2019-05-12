using Duality;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Environment;
using Jazz2.Actors.Solid;
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

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.None;

            RequestMetadata("Weapon/TNT");
            SetAnimation(AnimState.Idle);
        }

        public void OnFire(Player owner)
        {
            this.owner = owner;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (lifetime > 0f) {
                lifetime -= Time.TimeMult;

                if (lifetime > 40f) {
                    Vector3 pos = Transform.Pos;
                    foreach (ActorBase collision in api.FindCollisionActorsRadius(pos.X, pos.Y, 50)) {
                        if (!collision.IsInvulnerable && (collision is EnemyBase ||
                            collision is AmmoBarrel || collision is AmmoCrate ||
                            collision is BarrelContainer || collision is CrateContainer ||
                            collision is GemBarrel || collision is GemCrate ||
                            collision is PowerUpMorphMonitor || collision is PowerUpShieldMonitor ||
                            collision is PowerUpWeaponMonitor || collision is TriggerCrate ||
                            collision is BirdCage || collision is Pole)) {
                            lifetime = 40f;
                            break;
                        }
                    }
                }
            } else if (!isExploded) {
                isExploded = true;

                // ToDo: Sound + Animation
                SetTransition(AnimState.TransitionActivate, false, delegate {
                    DecreaseHealth(int.MaxValue);
                });

                PlaySound("Explosion");

                Vector3 pos = Transform.Pos;
                foreach (ActorBase collision in api.FindCollisionActorsRadius(pos.X, pos.Y, 50)) {
                    collision.OnHandleCollision(this);
                }

                TileMap tiles = api.TileMap;
                if (tiles != null) {
                    Hitbox hitbox = new Hitbox(pos.X - 34, pos.Y - 34, pos.X + 34, pos.Y + 34);
                    int destroyedCount = tiles.CheckWeaponDestructible(ref hitbox, WeaponType.TNT, 8);
                    if (destroyedCount > 0 && owner != null) {
                        owner.AddScore(destroyedCount * 50);
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