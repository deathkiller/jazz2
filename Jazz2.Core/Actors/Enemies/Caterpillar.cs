using System.Threading.Tasks;
using Duality;
using Duality.Audio;
using Jazz2.Actors.Weapons;
using Jazz2.Game.Structs;

namespace Jazz2.Actors.Enemies
{
    public class Caterpillar : EnemyBase
    {
        private const int StateIdle = 0;
        private const int StateAttacking = 1;
        private const int StateDisoriented = 2;

        private int state;
        private int smokesLeft;
        private float attackTime;

        protected override async Task OnActivatedAsync(ActorActivationDetails details)
        {
            canBeFrozen = false;
            IsFacingLeft = true;
            collisionFlags = CollisionFlags.CollideWithTileset | CollisionFlags.CollideWithOtherActors | CollisionFlags.ApplyGravitation;
            canHurtPlayer = false;
            isInvulnerable = true;

            health = int.MaxValue;

            await RequestMetadataAsync("Enemy/Caterpillar");
            SetAnimation(AnimState.Idle);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (frozenTimeLeft > 0) {
                return;
            }

            switch (state) {
                case StateIdle: {
                    if (currentTransitionState == AnimState.Idle) {
                        // InhaleStart
                        SetTransition((AnimState)1, true, delegate {
                            // Inhale
                            SetTransition((AnimState)2, true, delegate {
                                // ExhaleStart
                                SetTransition((AnimState)3, true, delegate {
                                    state = StateAttacking;
                                    smokesLeft = MathF.Rnd.Next(6, 12);
                                });
                            });
                        });
                    }

                    break;
                }

                case StateAttacking: {
                    if (attackTime <= 0f) {
                        attackTime = 60f;

                        SetAnimation((AnimState)5);
                        SetTransition((AnimState)4, true, delegate {
                            Vector3 pos = Transform.Pos;

                            Smoke smoke = new Smoke();
                            smoke.OnActivated(new ActorActivationDetails {
                                Api = api,
                                Pos = new Vector3(pos.X - 26f, pos.Y - 18f, pos.Z - 20f)
                            });
                            api.AddActor(smoke);

                            smokesLeft--;
                            if (smokesLeft <= 0) {
                                state = StateIdle;
                                SetAnimation(AnimState.Idle);
                            }
                        });
                    } else {
                        attackTime -= Time.TimeMult;
                    }
                    break;
                }
            }
        }

        public override void OnHandleCollision(ActorBase other)
        {
            base.OnHandleCollision(other);

            switch (other) {
                case AmmoBase ammo:
                    if (state != StateDisoriented) {
                        Disoriented(MathF.Rnd.Next(8, 13));
                    }
                    break;
            }
        }

        private void Disoriented(int count)
        {
            attackTime = 0f;
            smokesLeft = 0;
            state = StateDisoriented;

            SetTransition((AnimState)6, false, delegate {
                count--;
                if (count > 0) {
                    Disoriented(count);
                } else {
                    state = StateIdle;
                    SetAnimation(AnimState.Idle);
                }
            });
        }

        private class Smoke : EnemyBase
        {
            private float time = 500f;
            private Vector2 baseSpeed;

            protected override async Task OnActivatedAsync(ActorActivationDetails details)
            {
                base.canBeFrozen = false;
                base.canHurtPlayer = false;
                base.canCollideWithAmmo = false;
                base.isInvulnerable = true;
                base.collisionFlags = CollisionFlags.CollideWithOtherActors | CollisionFlags.SkipPerPixelCollisions;

                health = int.MaxValue;

                await RequestMetadataAsync("Enemy/Caterpillar");
                SetAnimation("Smoke");

                baseSpeed.X = MathF.Rnd.NextFloat(-1.4f, -0.8f);
                baseSpeed.Y = MathF.Rnd.NextFloat(-1.6f, -0.8f);

                //OnUpdateHitbox();
            }

            protected override void OnUpdate()
            {
                speedX = baseSpeed.X + MathF.Cos((500 - time) * 0.09f) * 0.5f;
                speedY = baseSpeed.Y + MathF.Sin((500 - time) * 0.05f) * 0.5f;

                MoveInstantly(new Vector2(speedX, speedY), MoveType.RelativeTime, true);

                Transform.Scale -= 0.0011f * Time.TimeMult;

                if (time <= 0f) {
                    DecreaseHealth(int.MaxValue);
                } else {
                    time -= Time.TimeMult;
                }
            }

            public override void OnHandleCollision(ActorBase other)
            {
                switch (other) {
                    case Player player: {
                        if (player.SetDizzyTime(180f)) {
                            SoundInstance sound = PlaySound("Dizzy");
                            sound.Looped = true;
                            sound.FadeOut(2.4f);
                        }
                        break;
                    }
                }
            }
        }
    }
}