using System;
using Duality;
using Duality.Components.Renderers;
using Duality.Drawing;
using Duality.Resources;
using MathF = Duality.MathF;

namespace Jazz2.Game.Components
{
    public class ActorRenderer : Component, ICmpUpdatable
    {
        /// <summary>
        /// Describes the sprite animations loop behaviour.
        /// </summary>
        public enum LoopMode
        {
            /// <summary>
            /// The animation is played once an then remains in its last frame.
            /// </summary>
            Once,
            /// <summary>
            /// The animation is looped: When reaching the last frame, it begins again at the first one.
            /// </summary>
            Loop,
            /// <summary>
            /// A fixed, single frame is displayed. Which one depends on the one you set in the editor or
            /// in source code.
            /// </summary>
            FixedSingle
        }

        private int animFirstFrame;
        private int animFrameCount = 1;
        private float animDuration = 5.0f;
        private LoopMode animLoopMode = LoopMode.Loop;
        private float animTime;
        private bool animPaused, animHidden;

        private int curAnimFrame, nextAnimFrame;
        private float curAnimFrameFade;

        private Point2 frameConfiguration = new Point2(1, 1);

        public Point2 FrameConfiguration
        {
            get { return frameConfiguration; }
            set { frameConfiguration = value; }
        }

        /// <summary>
        /// [GET / SET] The index of the first frame to display. Ignored if <see cref="CustomFrameSequence"/> is set.
        /// </summary>
        /// <remarks>
        /// Animation indices are looked up in the <see cref="Duality.Resources.Pixmap.Atlas"/> map
        /// of the <see cref="Duality.Resources.Texture"/> that is used.
        /// </remarks>
        public int AnimFirstFrame
        {
            get { return animFirstFrame; }
            set { animFirstFrame = MathF.Max(0, value); }
        }

        /// <summary>
        /// [GET / SET] The number of continous frames to use for the animation. Ignored if <see cref="CustomFrameSequence"/> is set.
        /// </summary>
        /// <remarks>
        /// Animation indices are looked up in the <see cref="Duality.Resources.Pixmap.Atlas"/> map
        /// of the <see cref="Duality.Resources.Texture"/> that is used.
        /// </remarks>
        public int AnimFrameCount
        {
            get { return animFrameCount; }
            set { animFrameCount = MathF.Max(1, value); }
        }

        /// <summary>
        /// [GET / SET] The time a single animation cycle needs to complete, in seconds.
        /// </summary>
        public float AnimDuration
        {
            get { return animDuration; }
            set
            {
                float lastDuration = animDuration;

                animDuration = MathF.Max(0.0f, value);

                if (lastDuration != 0.0f && animDuration != 0.0f) {
                    animTime *= animDuration / lastDuration;
                }
            }
        }
        /// <summary>
        /// [GET / SET] The animations current play time, i.e. the current state of the animation.
        /// </summary>
        public float AnimTime
        {
            get { return animTime; }
            set { animTime = MathF.Max(0.0f, value); }
        }
        /// <summary>
        /// [GET / SET] If true, the animation is paused and won't advance over time. <see cref="AnimTime"/> will stay constant until resumed.
        /// </summary>
        public bool AnimPaused
        {
            get { return animPaused; }
            set { animPaused = value; }
        }
        /// <summary>
        /// [GET / SET] The animations loop behaviour.
        /// </summary>
        public LoopMode AnimLoopMode
        {
            get { return animLoopMode; }
            set { animLoopMode = value; }
        }

        public bool AnimHidden
        {
            get { return animHidden; }
            set { animHidden = value; }
        }

        /// <summary>
        /// [GET] Whether the animation is currently running, i.e. if there is anything animated right now.
        /// </summary>
        public bool IsAnimationRunning
        {
            get
            {
                switch (animLoopMode) {
                    case LoopMode.FixedSingle:
                        return false;
                    case LoopMode.Loop:
                        return !animPaused;
                    case LoopMode.Once:
                        return !animPaused && animTime < animDuration;
                    default:
                        return false;
                }
            }
        }
        /// <summary>
        /// [GET] The currently visible animation frames index.
        /// </summary>
        public int CurrentFrame
        {
            get { return curAnimFrame; }
        }
        /// <summary>
        /// [GET] The next visible animation frames index.
        /// </summary>
        public int NextFrame
        {
            get { return nextAnimFrame; }
        }
        /// <summary>
        /// [GET] The current animation frames progress where zero means "just entered the current frame"
        /// and one means "about to leave the current frame". This value is also used for smooth animation blending.
        /// </summary>
        public float CurrentFrameProgress
        {
            get { return curAnimFrameFade; }
        }

        public ColorRgba ColorTint { get; set; }
        public SpriteRenderer.FlipMode Flip { get; set; }

        public Action AnimationFinished;

        public ActorRenderer() { }
        public ActorRenderer(Rect rect, ContentRef<Material> mainMat) { }

        /// <summary>
        /// Updates the <see cref="CurrentFrame"/>, <see cref="NextFrame"/> and <see cref="CurrentFrameProgress"/> properties immediately.
        /// This is called implicitly once each frame before drawing, so you don't normally call this. However, when changing animation
        /// parameters and requiring updated animation frame data immediately, this could be helpful.
        /// </summary>
        public void UpdateVisibleFrames()
        {
            // Calculate visible frames
            curAnimFrame = 0;
            nextAnimFrame = 0;
            curAnimFrameFade = 0.0f;
            if (animFrameCount > 0 && animDuration > 0) {
                // Calculate currently visible frame
                float frameTemp = animFrameCount * animTime / animDuration;
                curAnimFrame = (int)frameTemp;

                // Normalize current frame when exceeding anim duration
                if (animLoopMode == LoopMode.Once || animLoopMode == LoopMode.FixedSingle) {
                    curAnimFrame = MathF.Clamp(curAnimFrame, 0, animFrameCount - 1);
                } else {
                    curAnimFrame = MathF.NormalizeVar(curAnimFrame, 0, animFrameCount);
                }

                // Calculate second frame and fade value
                curAnimFrameFade = frameTemp - (int)frameTemp;
                if (animLoopMode == LoopMode.Loop) {
                    nextAnimFrame = MathF.NormalizeVar(curAnimFrame + 1, 0, animFrameCount);
                } else {
                    nextAnimFrame = curAnimFrame + 1;
                }
            }
            curAnimFrame = animFirstFrame + MathF.Clamp(curAnimFrame, 0, animFrameCount - 1);
            nextAnimFrame = animFirstFrame + MathF.Clamp(nextAnimFrame, 0, animFrameCount - 1);
        }

        void ICmpUpdatable.OnUpdate()
        {
            if (!IsAnimationRunning) {
                return;
            }

            // Advance animation timer
            if (animLoopMode == LoopMode.Loop) {
                animTime += Time.TimeMult * Time.SecondsPerFrame;
                if (animTime > animDuration) {
                    int n = (int)(animTime / animDuration);
                    animTime -= animDuration * n;

                    if (AnimationFinished != null) {
                        AnimationFinished();
                    }
                }
            } else if (animLoopMode == LoopMode.Once) {
                float newAnimTime = animTime + Time.TimeMult * Time.SecondsPerFrame;
                if (animTime > animDuration) {
                    if (AnimationFinished != null) {
                        AnimationFinished();
                    }
                } else {
                    animTime = newAnimTime;
                }
            }

            UpdateVisibleFrames();
        }
    }
}