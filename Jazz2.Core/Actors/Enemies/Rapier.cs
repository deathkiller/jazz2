using System.Collections.Generic;
using Duality;
using Duality.Resources;
using Jazz2.Game.Structs;
using static Jazz2.Game.Tiles.TileMap;

namespace Jazz2.Actors.Enemies
{
    public class Rapier : EnemyBase
    {
        private Vector3 originPos, lastPos, targetPos, lastSpeed;
        private float anglePhase;
        private float attackTime = 80f;
        private bool attacking;
        private float noiseCooldown = MathF.Rnd.NextFloat(180, 300);

        public override void OnAttach(ActorInstantiationDetails details)
        {
            base.OnAttach(details);

            collisionFlags = CollisionFlags.CollideWithOtherActors;

            SetHealthByDifficulty(2);
            scoreValue = 300;

            originPos = lastPos = targetPos = details.Pos;

            RequestMetadata("Enemy/Rapier");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            HandleBlinking();

            if (frozenTimeLeft > 0) {
                frozenTimeLeft -= Time.TimeMult;
                return;
            }

            if (currentTransitionState == AnimState.Idle) {
                if (attackTime > 0f) {
                    attackTime -= Time.TimeMult;
                } else {
                    if (attacking) {
                        SetAnimation(AnimState.Idle);
                        SetTransition((AnimState)1073741826, false, delegate {
                            targetPos = originPos;

                            attackTime = MathF.Rnd.NextFloat(200f, 260f);
                            attacking = false;
                        });
                    } else {
                        AttackNearestPlayer();
                    }
                }

                if (noiseCooldown > 0f) {
                    noiseCooldown -= Time.TimeMult;
                } else {
                    noiseCooldown = MathF.Rnd.NextFloat(300, 500);

                    if (MathF.Rnd.NextFloat() < 0.5f) {
                        PlaySound("Noise", 0.7f);
                    }
                }
            }

            anglePhase += Time.TimeMult * 0.02f;

            Vector3 speed = ((targetPos - lastPos) / 26f + lastSpeed * 1.4f) / 2.4f;
            lastPos.X += speed.X;
            lastPos.Y += speed.Y;
            lastSpeed = speed;

            bool willFaceLeft = (speed.X < 0f);
            if (isFacingLeft != willFaceLeft) {
                isFacingLeft = willFaceLeft;
                SetTransition(AnimState.TransitionTurn, false, delegate {
                    RefreshFlipMode();
                });
            }

            Transform.Pos = lastPos + new Vector3(MathF.Cos(anglePhase) * 10f, MathF.Sin(anglePhase * 2f) * 10f, 0f);
        }

        protected override bool OnPerish(ActorBase collider)
        {
            {
                const int debrisSize = 2;

                Vector3 pos = Transform.Pos;
                Material material = currentAnimation.Material.Res;
                Texture texture = material.MainTexture.Res;

                float x = pos.X - currentAnimation.Hotspot.X;
                float y = pos.Y - currentAnimation.Hotspot.Y;

                for (int fx = 0; fx < currentAnimation.FrameDimensions.X; fx += debrisSize + 1) {
                    for (int fy = 0; fy < currentAnimation.FrameDimensions.Y; fy += debrisSize + 1) {
                        float currentSize = debrisSize * MathF.Rnd.NextFloat(0.2f, 1.1f);
                        api.TileMap.CreateDebris(new DestructibleDebris {
                            Pos = new Vector3(x + (isFacingLeft ? currentAnimation.FrameDimensions.X - fx : fx), y + fy, pos.Z),
                            Size = new Vector2(currentSize /** (isFacingLeft ? -1f : 1f)*/, currentSize),
                            Speed = new Vector2(((fx - currentAnimation.FrameDimensions.X / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (isFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(2f, 5f) / currentAnimation.FrameDimensions.X,
                                 ((fy - currentAnimation.FrameDimensions.Y / 2) + MathF.Rnd.NextFloat(-2f, 2f)) * (isFacingLeft ? -1f : 1f) * MathF.Rnd.NextFloat(2f, 5f) / currentAnimation.FrameDimensions.Y),
                            Acceleration = new Vector2(0f, 0f),

                            Scale = 1.2f,
                            Alpha = 1f,
                            AlphaSpeed = -0.01f,

                            Time = 280f,

                            Material = material,
                            MaterialOffset = new Rect(
                                 (((float)(renderer.CurrentFrame % currentAnimation.FrameConfiguration.X) / currentAnimation.FrameConfiguration.X) + ((float)fx / texture.ContentWidth)) * texture.UVRatio.X,
                                 (((float)(renderer.CurrentFrame / currentAnimation.FrameConfiguration.X) / currentAnimation.FrameConfiguration.Y) + ((float)fy / texture.ContentHeight)) * texture.UVRatio.Y,
                                 (currentSize * texture.UVRatio.X / texture.ContentWidth),
                                 (currentSize * texture.UVRatio.Y / texture.ContentHeight)
                             ),

                            CollisionAction = DebrisCollisionAction.None
                        });
                    }
                }
            }

            PlaySound("Die");

            TryGenerateRandomDrop();

            return base.OnPerish(collider);
        }

        private void AttackNearestPlayer()
        {
            bool found = false;
            Vector3 foundPos = new Vector3(float.MaxValue, float.MaxValue, lastPos.Z);

            List<Player> players = api.Players;
            for (int i = 0; i < players.Count; i++) {
                Vector3 newPos = players[i].Transform.Pos;
                if ((lastPos - newPos).Length < (lastPos - foundPos).Length) {
                    foundPos = newPos;
                    found = true;
                }
            }

            Vector3 diff = (foundPos - lastPos);
            if (found && diff.Length <= 200f) {
                SetAnimation((AnimState)1073741824);
                SetTransition((AnimState)1073741825, false, delegate {
                    targetPos = foundPos;

                    attackTime = 80f;
                    attacking = true;

                    PlaySound("Attack", 0.7f);
                });
            }
        }
    }
}