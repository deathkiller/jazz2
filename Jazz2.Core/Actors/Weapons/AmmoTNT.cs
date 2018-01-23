using Duality;
using Jazz2.Actors.Collectibles;
using Jazz2.Actors.Enemies;
using Jazz2.Actors.Solid;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Actors.Weapons
{
    public class AmmoTNT : ActorBase
    {
        private Player owner;

        private float lifetime = 80f;
        private bool exploded;

        public Player Owner => owner;

        public AmmoTNT(Player owner)
        {
            this.owner = owner;
        }

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            RequestMetadata("Weapon/TNT");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (lifetime > 0f) {
                lifetime -= Time.TimeMult;
            } else if (!exploded) {
                exploded = true;

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
                Transform.Scale += Time.TimeMult * 0.04f;
            }
        }
    }
}