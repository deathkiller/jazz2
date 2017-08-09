using System;
using Duality;
using Duality.Components.Renderers;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.UI;

namespace Jazz2.Game
{
    public class ActorRenderer : SpriteRenderer, ICmpUpdatable
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
        private bool animPaused;

        private int curAnimFrame, nextAnimFrame;
        private float curAnimFrameFade;
        private VertexC1P3T4A1[] verticesSmooth;

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

                if (lastDuration != 0.0f && animDuration != 0.0f)
                    animTime *= animDuration / lastDuration;
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

        public Action AnimationFinished;

        public ActorRenderer() { }
        public ActorRenderer(Rect rect, ContentRef<Material> mainMat) : base(rect, mainMat) { }

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
                if (animLoopMode == LoopMode.Once || animLoopMode == LoopMode.FixedSingle)
                    curAnimFrame = MathF.Clamp(curAnimFrame, 0, animFrameCount - 1);
                else
                    curAnimFrame = MathF.NormalizeVar(curAnimFrame, 0, animFrameCount);

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

        protected void PrepareVerticesSmooth(ref VertexC1P3T4A1[] vertices, IDrawDevice device, float curAnimFrameFade, ColorRgba mainClr, Rect uvRect, Rect uvRectNext)
        {
            Vector3 posTemp = gameobj.Transform.Pos;
            float scaleTemp = 1.0f;
            device.PreprocessCoords(ref posTemp, ref scaleTemp);

            Vector2 xDot, yDot;
            MathF.GetTransformDotVec(GameObj.Transform.Angle, scaleTemp, out xDot, out yDot);

            Rect rectTemp = rect.Transformed(gameobj.Transform.Scale, gameobj.Transform.Scale);
            Vector2 edge1 = rectTemp.TopLeft;
            Vector2 edge2 = rectTemp.BottomLeft;
            Vector2 edge3 = rectTemp.BottomRight;
            Vector2 edge4 = rectTemp.TopRight;

            MathF.TransformDotVec(ref edge1, ref xDot, ref yDot);
            MathF.TransformDotVec(ref edge2, ref xDot, ref yDot);
            MathF.TransformDotVec(ref edge3, ref xDot, ref yDot);
            MathF.TransformDotVec(ref edge4, ref xDot, ref yDot);

            float left = uvRect.X;
            float right = uvRect.RightX;
            float top = uvRect.Y;
            float bottom = uvRect.BottomY;
            float nextLeft = uvRectNext.X;
            float nextRight = uvRectNext.RightX;
            float nextTop = uvRectNext.Y;
            float nextBottom = uvRectNext.BottomY;

            if ((flipMode & FlipMode.Horizontal) != FlipMode.None) {
                edge1.X = -edge1.X;
                edge2.X = -edge2.X;
                edge3.X = -edge3.X;
                edge4.X = -edge4.X;
            }
            if ((flipMode & FlipMode.Vertical) != FlipMode.None) {
                edge1.Y = -edge1.Y;
                edge2.Y = -edge2.Y;
                edge3.Y = -edge3.Y;
                edge4.Y = -edge4.Y;
            }

            if (vertices == null || vertices.Length != 4) vertices = new VertexC1P3T4A1[4];

            vertices[0].Pos.X = posTemp.X + edge1.X;
            vertices[0].Pos.Y = posTemp.Y + edge1.Y;
            vertices[0].Pos.Z = posTemp.Z + VertexZOffset;
            vertices[0].TexCoord.X = left;
            vertices[0].TexCoord.Y = top;
            vertices[0].TexCoord.Z = nextLeft;
            vertices[0].TexCoord.W = nextTop;
            vertices[0].Color = mainClr;
            vertices[0].Attrib = curAnimFrameFade;

            vertices[1].Pos.X = posTemp.X + edge2.X;
            vertices[1].Pos.Y = posTemp.Y + edge2.Y;
            vertices[1].Pos.Z = posTemp.Z + VertexZOffset;
            vertices[1].TexCoord.X = left;
            vertices[1].TexCoord.Y = bottom;
            vertices[1].TexCoord.Z = nextLeft;
            vertices[1].TexCoord.W = nextBottom;
            vertices[1].Color = mainClr;
            vertices[1].Attrib = curAnimFrameFade;

            vertices[2].Pos.X = posTemp.X + edge3.X;
            vertices[2].Pos.Y = posTemp.Y + edge3.Y;
            vertices[2].Pos.Z = posTemp.Z + VertexZOffset;
            vertices[2].TexCoord.X = right;
            vertices[2].TexCoord.Y = bottom;
            vertices[2].TexCoord.Z = nextRight;
            vertices[2].TexCoord.W = nextBottom;
            vertices[2].Color = mainClr;
            vertices[2].Attrib = curAnimFrameFade;

            vertices[3].Pos.X = posTemp.X + edge4.X;
            vertices[3].Pos.Y = posTemp.Y + edge4.Y;
            vertices[3].Pos.Z = posTemp.Z + VertexZOffset;
            vertices[3].TexCoord.X = right;
            vertices[3].TexCoord.Y = top;
            vertices[3].TexCoord.Z = nextRight;
            vertices[3].TexCoord.W = nextTop;
            vertices[3].Color = mainClr;
            vertices[3].Attrib = curAnimFrameFade;

            if (pixelGrid) {
                vertices[0].Pos.X = MathF.Round(vertices[0].Pos.X);
                vertices[1].Pos.X = MathF.Round(vertices[1].Pos.X);
                vertices[2].Pos.X = MathF.Round(vertices[2].Pos.X);
                vertices[3].Pos.X = MathF.Round(vertices[3].Pos.X);

                if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2) {
                    vertices[0].Pos.X += 0.5f;
                    vertices[1].Pos.X += 0.5f;
                    vertices[2].Pos.X += 0.5f;
                    vertices[3].Pos.X += 0.5f;
                }

                vertices[0].Pos.Y = MathF.Round(vertices[0].Pos.Y);
                vertices[1].Pos.Y = MathF.Round(vertices[1].Pos.Y);
                vertices[2].Pos.Y = MathF.Round(vertices[2].Pos.Y);
                vertices[3].Pos.Y = MathF.Round(vertices[3].Pos.Y);

                if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2) {
                    vertices[0].Pos.Y += 0.5f;
                    vertices[1].Pos.Y += 0.5f;
                    vertices[2].Pos.Y += 0.5f;
                    vertices[3].Pos.Y += 0.5f;
                }
            }
        }
        protected void GetAnimData(Texture mainTex, bool smoothShaderInput, out Rect uvRect, out Rect uvRectNext)
        {
            UpdateVisibleFrames();

            if (mainTex != null) {
                mainTex.LookupAtlas(curAnimFrame, out uvRect);
                mainTex.LookupAtlas(nextAnimFrame, out uvRectNext);

            } else {
                uvRect = uvRectNext = new Rect(1.0f, 1.0f);
            }
        }

        public override void Draw(IDrawDevice device)
        {
            Texture mainTex = RetrieveMainTex();
            ColorRgba mainClr = RetrieveMainColor();
            DrawTechnique tech = RetrieveDrawTechnique();

            Rect uvRect, uvRectNext;
            bool smoothShaderInput = tech != null && tech.PreferredVertexFormat == VertexC1P3T4A1.Declaration;
            GetAnimData(mainTex/*, tech*/, smoothShaderInput, out uvRect, out uvRectNext);

            if (!smoothShaderInput) {
                PrepareVertices(ref vertices, device, mainClr, uvRect);
                if (customMat != null) {
                    device.AddVertices(customMat, VertexMode.Quads, vertices);
                } else {
                    if (flipMode == 0) {
                        device.AddVertices(sharedMat, VertexMode.Quads, vertices);
                    } else {
                        BatchInfo material = sharedMat.Res.Info;
                        material.SetUniform("normalMultiplier", (flipMode & FlipMode.Horizontal) == 0 ? 1 : -1f, (flipMode & FlipMode.Vertical) == 0 ? 1 : -1f);
                        device.AddVertices(material, VertexMode.Quads, vertices);
                    }
                }
            } else {
                PrepareVerticesSmooth(ref verticesSmooth, device, curAnimFrameFade, mainClr, uvRect, uvRectNext);
                if (customMat != null) {
                    device.AddVertices(customMat, VertexMode.Quads, verticesSmooth);
                } else {
                    if (flipMode == 0) {
                        device.AddVertices(sharedMat, VertexMode.Quads, verticesSmooth);
                    } else {
                        BatchInfo material = sharedMat.Res.Info;
                        material.SetUniform("normalMultiplier", (flipMode & FlipMode.Horizontal) == 0 ? 1 : -1f, (flipMode & FlipMode.Vertical) == 0 ? 1 : -1f);
                        device.AddVertices(material, VertexMode.Quads, verticesSmooth);
                    }
                }
            }

#if DEBUG && !SHOW_HITBOXES
            Structs.Hitbox h = ((Actors.ActorBase)gameobj).Hitbox;
            Hud.ShowDebugRect(new Rect(h.Left, h.Top, h.Right - h.Left, h.Bottom - h.Top));
            Vector3 pos = ((Actors.ActorBase)gameobj).Transform.Pos;
            Hud.ShowDebugRect(new Rect(pos.X - 1, pos.Y - 1 , 3, 3));
#endif
        }
    }
}