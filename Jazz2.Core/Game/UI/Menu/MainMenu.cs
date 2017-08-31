using System;
using System.Collections.Generic;
using Duality;
using Duality.Audio;
using Duality.Components;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Game.UI.Menu
{
    public partial class MainMenu : Scene
    {
        private readonly Controller root;
        private readonly GameObject rootObject;

        private Stack<MainMenuSection> sectionStack;

        private CanvasBuffer canvasBuffer;
        private BitmapFont fontSmall, fontMedium;

        private Metadata metadata;

        public ContentRef<Material> TopLine, BottomLine, Dim;

        private OpenMptStream music;

        private static string newVersion;

        public MainMenu(Controller root)
        {
            this.root = root;

            root.Title = null;
            root.Immersive = true;

            rootObject = new GameObject("MainMenu");
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
            ColorRgba[] defaultPalette = TileSet.LoadPalette(PathOp.Combine("Content", "Animations", ".palette"));

            ContentResolver.Current.ApplyBasePalette(defaultPalette);

            fontSmall = new BitmapFont("UI/font_small", 17, 18, 15, 32, 256, -2, canvasBuffer);
            fontMedium = new BitmapFont("UI/font_medium", 29, 31, 15, 32, 256, -1, canvasBuffer);

            metadata = ContentResolver.Current.RequestMetadata("UI/MainMenu");

            PrerenderTexturedBackground();

            // Play music
            string musicPath = PathOp.Combine(DualityApp.DataDirectory, "Music", MathF.Rnd.OneOf(new[] {
                "bonus2.j2b", "bonus3.j2b"
            }));
            if (!FileOp.Exists(musicPath)) {
                musicPath = PathOp.Combine(DualityApp.DataDirectory, "Music", "menu.j2b");
            }
            music = DualityApp.Sound.PlaySound(new OpenMptStream(musicPath));
            music.BeginFadeIn(0.5f);

            InitTouch();

            // Show Begin section
            sectionStack = new Stack<MainMenuSection>();

            SwitchToSection(new BeginSection());

            if (newVersion == null) {
                Updater.CheckUpdates(OnCheckUpdates);
            }
        }

        protected override void OnDisposing(bool manually)
        {
            if (music != null) {
                music.FadeOut(1f);
                music = null;
            }

            base.OnDisposing(manually);
        }

        public void SwitchToSection(MainMenuSection section)
        {
            if (sectionStack.Count > 0) {
                //renderSetup.BeginPageTransition();
                sectionStack.Peek().OnHide(false);
            }

            sectionStack.Push(section);
            section.OnShow(this);
        }

        public void LeaveSection(MainMenuSection section)
        {
            if (sectionStack.Count > 0) {
                MainMenuSection activeSection = sectionStack.Pop();
                if (activeSection != section) {
                    throw new InvalidOperationException();
                }

                //renderSetup.BeginPageTransition();

                activeSection.OnHide(true);

                if (sectionStack.Count > 0) {
                    sectionStack.Peek().OnShow(this);
                }
            }
        }

        public void SwitchToLevel(LevelInitialization data)
        {
            root.ChangeLevel(data);
        }

#if MULTIPLAYER
        public void SwitchToServer(System.Net.IPEndPoint endPoint)
        {
            root.ConnectToServer(endPoint);
        }
#endif

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
                res.Draw(c, x, y, alignment, color, scaleX, scaleY);
            }
        }

        public void DrawMaterial(Canvas c, string name, float x, float y, Alignment alignment, ColorRgba color, float scaleX, float scaleY, Rect texRect)
        {
            GraphicResource res;
            if (metadata.Graphics.TryGetValue(name, out res)) {
                res.Draw(c, x, y, alignment, color, scaleX, scaleY, texRect);
            }
        }

        public void DrawMaterial(Canvas c, string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            GraphicResource res;
            if (metadata.Graphics.TryGetValue(name, out res)) {
                res.Draw(c, frame, x, y, alignment, color, scaleX, scaleY);
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

        public bool IsAnimationPresent(string name)
        {
            return metadata.Graphics.ContainsKey(name);
        }

        private void OnUpdate()
        {
            if (sectionStack.Count > 0) {
                sectionStack.Peek().OnUpdate();
            }
        }

        private void OnRender(IDrawDevice device)
        {
            Vector2 center = device.TargetSize * 0.5f;

            Canvas c = new Canvas(device, canvasBuffer);

            int charOffset = 0;
            int charOffsetShadow = 0;

            RenderTexturedBackground(device);

            // Title
            DrawMaterial(c, "MenuCarrot", -1, center.X - 76f, 64f + 2f, Alignment.Center, new ColorRgba(0f, 0.3f), 0.8f, 0.8f);
            DrawMaterial(c, "MenuCarrot", -1, center.X - 76f, 64f, Alignment.Center, ColorRgba.White, 0.8f, 0.8f);

            fontMedium.DrawString(device, ref charOffsetShadow, "Jazz", center.X - 63f, 70f + 2f, Alignment.Left,
                new ColorRgba(0f, 0.32f), 0.75f, 1.63f, 3f, 3f, 0f, 0.92f);
            fontMedium.DrawString(device, ref charOffsetShadow, "2", center.X - 19f, 70f - 8f + 2f, Alignment.Left,
                new ColorRgba(0f, 0.32f), 0.5f, 0f, 0f, 0f, 0f);
            fontMedium.DrawString(device, ref charOffsetShadow, "Resurrection", center.X - 10f, 70f + 4f + 2.5f, Alignment.Left,
                new ColorRgba(0f, 0.3f), 0.5f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

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
                ColorRgba.TransparentBlack, 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            // Copyright
            Vector2 bottomLeft = bottomRight;
            bottomLeft.X = 24f;
            DrawStringShadow(device, ref charOffset, "(c) 2016-2017  Dan R.", bottomLeft.X, bottomLeft.Y, Alignment.BottomLeft,
                ColorRgba.TransparentBlack, 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            // New Version
            if (!string.IsNullOrEmpty(newVersion)) {
                DrawStringShadow(device, ref charOffset, "New version available: " + newVersion, (bottomLeft.X + bottomRight.X) * 0.5f, bottomLeft.Y, Alignment.Bottom,
                    new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f), 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.9f);
            }

            // Current section
            if (sectionStack.Count > 0) {
                sectionStack.Peek().OnPaint(device, c);
            }

            DrawTouch(device, c, device.TargetSize);
        }

        private void OnCheckUpdates(bool newAvailable, string version)
        {
            if (newAvailable) {
                newVersion = version;
            } else {
                newVersion = "";
            }
        }

        partial void InitTouch();

        partial void DrawTouch(IDrawDevice device, Canvas c, Vector2 size);

        private class LocalController : Component, ICmpUpdatable, ICmpRenderer
        {
            private readonly MainMenu mainMenu;

            public LocalController(MainMenu mainMenu)
            {
                this.mainMenu = mainMenu;
            }

            void ICmpUpdatable.OnUpdate()
            {
                mainMenu.OnUpdate();
            }

            float ICmpRenderer.BoundRadius => float.MaxValue;

            void ICmpRenderer.Draw(IDrawDevice device)
            {
                mainMenu.OnRender(device);
            }

            bool ICmpRenderer.IsVisible(IDrawDevice device)
            {
                return (device.VisibilityMask & VisibilityFlag.ScreenOverlay) != 0;
            }
        }
    }
}