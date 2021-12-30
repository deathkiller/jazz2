﻿using System.Threading.Tasks;
using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Skeleton : EnemyBase
    {
        private const float DefaultSpeed = 0.7f;

        private bool stuck;

        public static void Preload(ActorActivationDetails details)
        {
            PreloadMetadata("Enemy/Skeleton");
        }

        public static ActorBase Create(ActorActivationDetails details)
        {
            var actor = new Skeleton();
            actor.OnActivated(details);
            return actor;
        }

        private Skeleton()
        {
        }

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            Vector3 pos = Transform.Pos;
            pos.Y -= 6f;
            Transform.Pos = pos;

            SetHealthByDifficulty(3);
            scoreValue = 200;

            await RequestMetadataAsync("Enemy/Skeleton");
            SetAnimation(AnimState.Walk);

            IsFacingLeft = MathF.Rnd.NextBool();
            speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
        }

        protected override void OnUpdateHitbox()
        {
            UpdateHitbox(30, 30);
        }

        public override void OnFixedUpdate(float timeMult)
        {
            base.OnFixedUpdate(timeMult);

            if (frozenTimeLeft > 0) {
                return;
            }

            if (canJump) {
                if (!CanMoveToPosition(speedX * 4, 0)) {
                    if (stuck) {
                        MoveInstantly(new Vector2(0f, -2f), MoveType.Relative, true);
                    } else {
                        IsFacingLeft = !IsFacingLeft;
                        speedX = (IsFacingLeft ? -1 : 1) * DefaultSpeed;
                        stuck = true;
                    }
                } else {
                    stuck = false;
                }
            }
        }

        protected override void OnHealthChanged(ActorBase collider)
        {
            CreateSpriteDebris("Bone", MathF.Rnd.Next(1, 3));

            base.OnHealthChanged(collider);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            // TODO: Sound of bones
            // TODO: Use CreateDeathDebris(collider); instead?
            CreateParticleDebris();
            levelHandler.PlayCommonSound("Splat", Transform.Pos);

            CreateSpriteDebris("Skull", 1);
            CreateSpriteDebris("Bone", MathF.Rnd.Next(9, 12));

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }
    }
}