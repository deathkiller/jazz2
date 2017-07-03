using System.Collections.Generic;
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
        private float lifetime = 80f;
        private bool exploded;

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

                List<ActorBase> colliders = api.FindCollisionActorsRadius(pos.X, pos.Y, /*40*/50);
                for (int i = 0; i < colliders.Count; i++) {
                    if (colliders[i] is EnemyBase || colliders[i] is SolidObjectBase || colliders[i] is TurtleShell) {
                        colliders[i].DecreaseHealth(10);
                    } else if (colliders[i] is Collectible) {
                        colliders[i].HandleCollision(this);
                    }
                }

                TileMap tiles = api.TileMap;
                if (tiles != null) {
                    Hitbox hitbox = new Hitbox(pos.X - 34, pos.Y - 34, pos.X + 34, pos.Y + 34);
                    if (tiles.CheckWeaponDestructible(ref hitbox, WeaponType.TNT) > 0) {
                        // ToDo: Add score
                    }
                }
            } else {
                Transform.Scale += Time.TimeMult * 0.04f;
            }
        }
    }
}