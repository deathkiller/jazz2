using System;
using System.Collections.Generic;
using Duality;
using Duality.Audio;
using Duality.Components;
using Duality.Drawing;
using Duality.Resources;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Menu.I
{
    public partial class InGameMenu : Scene
    {
        private readonly Controller root;
        private readonly GameObject rootObject;
        private readonly LevelHandler levelHandler;

        private Stack<InGameMenuSection> sectionStack;

        private CanvasBuffer canvasBuffer;
        private BitmapFont fontSmall, fontMedium;

        private Metadata metadata;

        public ContentRef<Material> TopLine, BottomLine, Dim;

        private Material finalMaterial;
        private float transition;

        public InGameMenu(Controller root, LevelHandler levelHandler)
        {
            this.root = root;
            this.levelHandler = levelHandler;

            root.Immersive = true;

            rootObject = new GameObject("InGameMenu");
            rootObject.AddComponent(new LocalController(this));
            AddObject(rootObject);

            // Setup camera
            GameObject camera = new GameObject("MainCamera");
            camera.AddComponent<Transform>();

            Camera cameraInner = camera.AddComponent<Camera>();
            cameraInner.Perspective = PerspectiveMode.Flat;
            cameraInner.NearZ = 0;
            cameraInner.FarZ = 1000;

            cameraInner.RenderingSetup = new MainMenuRenderSetup();
            camera.Parent = rootObject;

            canvasBuffer = new CanvasBuffer();

            // Load resources
            fontSmall = new BitmapFont("UI/font_small", 17, 18, 15, 32, 256, -2, canvasBuffer);
            fontMedium = new BitmapFont("UI/font_medium", 29, 31, 15, 32, 256, -1, canvasBuffer);

            metadata = ContentResolver.Current.RequestMetadata("UI/MainMenu", null);

            // Get game screen
            Camera cameraLevel = levelHandler.FindComponent<Camera>();
            LevelRenderSetup renderSetup = cameraLevel.ActiveRenderSetup as LevelRenderSetup;
            if (renderSetup != null) {
                finalMaterial = new Material(DrawTechnique.Solid, ColorRgba.White, renderSetup.FunalTexture);
            }

            InitTouch();

            // Show Begin section
            sectionStack = new Stack<InGameMenuSection>();

            SwitchToSection(new InGameMenuBeginSection());

        }

        public void SwitchToSection(InGameMenuSection section)
        {
            if (sectionStack.Count > 0) {
                sectionStack.Peek().OnHide();
            }

            sectionStack.Push(section);
            section.OnShow(this);
        }

        public void LeaveSection(InGameMenuSection section)
        {
            if (sectionStack.Count > 0) {
                InGameMenuSection activeSection = sectionStack.Pop();
                if (activeSection != section) {
                    throw new InvalidOperationException();
                }

                activeSection.OnHide();

                if (sectionStack.Count > 0) {
                    sectionStack.Peek().OnShow(this);
                }
            }
        }

        public void SwitchToCurrentGame()
        {
            root.Immersive = false;

            Scene.Current.DisposeLater();
            Scene.SwitchTo(levelHandler);
        }

        public void SwitchToMainMenu()
        {
            levelHandler.Dispose();

            root.ShowMainMenu();
        }

        public void DrawString(IDrawDevice device, ref int charOffset, string text, float x,
            float y, Alignment align, ColorRgba? color, float scale = 1f, float angleOffset = 0f, float varianceX = 4f, float varianceY = 4f,
            float speed = 4f, float charSpacing = 1f, float lineSpacing = 1f)
        {
            fontSmall.DrawString(device, ref charOffset, text, x, y, align,
                color, scale, angleOffset, varianceX, varianceY, speed, charSpacing, lineSpacing);
        }

        public void DrawStringShadow(IDrawDevice device, ref int charOffset, string text, float x,
            float y, Alignment align, ColorRgba? color, float scale = 1f, float angleOffset = 0f, float varianceX = 4f, float varianceY = 4f,
            float speed = 4f, float charSpacing = 1f, float lineSpacing = 1f)
        {
            int charOffsetShadow = charOffset;
            fontSmall.DrawString(device, ref charOffsetShadow, text, x, y + 2.8f * scale, align,
                new ColorRgba(0f, 0.29f), scale, angleOffset, varianceX, varianceY, speed, charSpacing, lineSpacing);
            fontSmall.DrawString(device, ref charOffset, text, x, y, align,
                color, scale, angleOffset, varianceX, varianceY, speed, charSpacing, lineSpacing);
        }

        public void DrawMaterial(Canvas c, string name, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            GraphicResource res;
            if (metadata.Graphics.TryGetValue(name, out res)) {
                Texture texture = res.Material.Res.MainTexture.Res;

                Vector2 originPos = new Vector2(x, y);
                alignment.ApplyTo(ref originPos, new Vector2(texture.InternalWidth * scaleX, texture.InternalHeight * scaleY));

                c.State.SetMaterial(res.Material);
                c.State.ColorTint = color;
                c.FillRect((int)originPos.X, (int)originPos.Y, texture.InternalWidth * scaleX, texture.InternalHeight * scaleY);
            }
        }

        public void DrawMaterial(Canvas c, string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            GraphicResource res;
            if (metadata.Graphics.TryGetValue(name, out res)) {
                Texture texture = res.Material.Res.MainTexture.Res;

                if (frame < 0) {
                    frame = (int)(Time.GameTimer.TotalSeconds * 0.86f * res.FrameCount / res.FrameDuration) % res.FrameCount;
                }

                Rect uv = texture.LookupAtlas(frame);
                float w = texture.InternalWidth * scaleX * uv.W;
                float h = texture.InternalHeight * scaleY * uv.H;

                Vector2 originPos = new Vector2(x, y);
                alignment.ApplyTo(ref originPos, new Vector2(w, h));

                c.State.SetMaterial(res.Material);
                c.State.ColorTint = color;
                c.State.TextureCoordinateRect = uv;
                c.FillRect((int)originPos.X, (int)originPos.Y, w, h);
            }
        }

        public void PlaySound(string name, float volume = 1f)
        {
            SoundResource res;
            if (metadata.Sounds.TryGetValue(name, out res)) {
                SoundInstance instance = DualityApp.Sound.PlaySound(res.Sound);
                // TODO: Hardcoded volume
                instance.Volume = volume * Settings.SfxVolume;
            }
        }

        public static float EaseOutElastic(float t)
        {
            float p = 0.3f;
            return MathF.Pow(2, -10 * t) * MathF.Sin((t - p / 4) * (2 * MathF.Pi) / p) + 1;
        }

        private void OnUpdate()
        {
            if (sectionStack.Count > 0) {
                sectionStack.Peek().OnUpdate();
            }

            if (transition < 1f) {
                transition += Time.TimeMult * 0.14f;
            } else {
                transition = 1f;
            }
        }

        private void OnRender(IDrawDevice device)
        {
            Vector2 center = device.TargetSize * 0.5f;

            Canvas c = new Canvas(device, canvasBuffer);

            int charOffset = 0;
            int charOffsetShadow = 0;

            c.State.SetMaterial(finalMaterial);
            c.State.ColorTint = new ColorRgba(1f - transition * 0.5f);
            c.State.TextureCoordinateRect = new Rect(0, 0, 1, 1);
            c.FillRect(0, 0, device.TargetSize.X, device.TargetSize.Y);

            // Title
            DrawMaterial(c, "MenuCarrot", -1, center.X - 76f, 64f + 2f, Alignment.Center, new ColorRgba(0f, 0.3f), 0.8f, 0.8f);
            DrawMaterial(c, "MenuCarrot", -1, center.X - 76f, 64f, Alignment.Center, ColorRgba.White, 0.8f, 0.8f);

            fontMedium.DrawString(device, ref charOffsetShadow, "Jazz", center.X - 63f, 70f + 2f, Alignment.Left,
                new ColorRgba(0f, 0.35f), 0.75f, 1.63f, 3f, 3f, 0f, 0.92f);
            fontMedium.DrawString(device, ref charOffsetShadow, "2", center.X - 19f, 70f - 8f + 2f, Alignment.Left,
                new ColorRgba(0f, 0.35f), 0.5f, 0f, 0f, 0f, 0f);
            fontMedium.DrawString(device, ref charOffsetShadow, "Resurrection", center.X - 10f, 70f + 4f + 2.5f, Alignment.Left,
                new ColorRgba(0f, 0.33f), 0.5f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            fontMedium.DrawString(device, ref charOffset, "Jazz", center.X - 63f, 70f, Alignment.Left,
                new ColorRgba(0.54f, 0.44f, 0.34f, 0.5f), 0.75f, 1.63f, 3f, 3f, 0f, 0.92f);
            fontMedium.DrawString(device, ref charOffset, "2", center.X - 19f, 70f - 8f, Alignment.Left,
                new ColorRgba(0.54f, 0.44f, 0.34f, 0.5f), 0.5f, 0f, 0f, 0f, 0f);
            fontMedium.DrawString(device, ref charOffset, "Resurrection", center.X - 10f, 70f + 4f, Alignment.Left,
                new ColorRgba(0.6f, 0.42f, 0.42f, 0.5f), 0.5f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            // Version
            Vector2 bottomRight = device.TargetSize;
            bottomRight.X -= 24f;
            bottomRight.Y -= 10f;
            DrawStringShadow(device, ref charOffset, "v" + App.AssemblyVersion, bottomRight.X, bottomRight.Y, Alignment.BottomRight,
                new ColorRgba(0.45f, 0.5f), 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            // Copyright
            Vector2 bottomLeft = bottomRight;
            bottomLeft.X = 24f;
            DrawStringShadow(device, ref charOffset, "(c) 2016-2017  Dan R.", bottomLeft.X, bottomLeft.Y, Alignment.BottomLeft,
                new ColorRgba(0.45f, 0.5f), 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            // Current section
            if (sectionStack.Count > 0) {
                sectionStack.Peek().OnPaint(device, c);
            }

            DrawTouch(device, c, device.TargetSize);
        }

        partial void InitTouch();

        partial void DrawTouch(IDrawDevice device, Canvas c, Vector2 size);


        private class LocalController : Component, ICmpUpdatable, ICmpRenderer
        {
            private readonly InGameMenu menu;

            public LocalController(InGameMenu menu)
            {
                this.menu = menu;
            }

            void ICmpUpdatable.OnUpdate()
            {
                menu.OnUpdate();
            }

            float ICmpRenderer.BoundRadius => float.MaxValue;

            void ICmpRenderer.Draw(IDrawDevice device)
            {
                menu.OnRender(device);
            }

            bool ICmpRenderer.IsVisible(IDrawDevice device)
            {
                return (device.VisibilityMask & VisibilityFlag.ScreenOverlay) != 0;
            }
        }
    }
}