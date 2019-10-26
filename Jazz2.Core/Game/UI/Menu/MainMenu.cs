using System;
using System.Collections.Generic;
using System.IO;
using Duality;
using Duality.Audio;
using Duality.Components;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;
using MathF = Duality.MathF;

namespace Jazz2.Game.UI.Menu
{
    public partial class MainMenu : Scene, IMenuContainer
    {
        private App root;

        private Stack<MenuSection> sectionStack;

        private Canvas canvas;
        private BitmapFont fontSmall, fontMedium;

        private TransitionManager transitionManager;
        private Action transitionAction;
        private float transitionText = 0.8f;
        private float transitionWhite;

        private Metadata metadata;

        public ContentRef<Material> TopLine, BottomLine, Dim;

        private OpenMptStream music;

        private static string newVersion;

        public RefreshMode RefreshMode
        {
            get
            {
                return root.RefreshMode;
            }
            set
            {
                root.RefreshMode = value;
            }
        }

        public ScreenMode ScreenMode
        {
            get
            {
                return root.ScreenMode;
            }
            set
            {
                root.ScreenMode = value;
            }
        }

        public MainMenu(App root, bool isInstallationComplete, bool afterIntro)
        {
            this.root = root;

            transitionWhite = (afterIntro ? 1f : 0f);

            root.Title = null;
            root.Immersive = true;

            GameObject rootObject = new GameObject();
            rootObject.AddComponent(new LocalController(this));
            AddObject(rootObject);

            // Setup camera
            GameObject camera = new GameObject();
            camera.AddComponent<Transform>();

            Camera cameraInner = camera.AddComponent<Camera>();
            cameraInner.Projection = ProjectionMode.Orthographic;
            cameraInner.NearZ = 10;
            cameraInner.FarZ = 1000;

            cameraInner.RenderingSetup = new MainMenuRenderSetup();
            camera.Parent = rootObject;

            canvas = new Canvas();

            // Load resources
#if UNCOMPRESSED_CONTENT
            using (Stream s = FileOp.Open(PathOp.Combine(DualityApp.DataDirectory, "Animations", "Main.palette"), FileAccessMode.Read))
#else
            using (Stream s = FileOp.Open(PathOp.Combine(DualityApp.DataDirectory, "Main.dz", "Animations", "Main.palette"), FileAccessMode.Read))
#endif
            {
                ContentResolver.Current.ApplyBasePalette(TileSet.LoadPalette(s));
            }

            fontSmall = new BitmapFont(canvas, "_custom/font_small");
            fontMedium = new BitmapFont(canvas, "_custom/font_medium");

            metadata = ContentResolver.Current.RequestMetadata("UI/MainMenu");

            PrerenderTexturedBackground();

            // Play music
            string musicPath = PathOp.Combine(DualityApp.DataDirectory, "Music", MathF.Rnd.OneOf(new[] {
                "bonus2.j2b", "bonus3.j2b"
            }));
            if (!FileOp.Exists(musicPath)) {
                musicPath = PathOp.Combine(DualityApp.DataDirectory, "Music", "menu.j2b");
            }
            music = new OpenMptStream(musicPath, true);
            music.BeginFadeIn(0.5f);
            DualityApp.Sound.PlaySound(music);

            InitPlatformSpecific();

            // Show Begin section
            sectionStack = new Stack<MenuSection>();

            if (isInstallationComplete) {
                SwitchToSection(new BeginSection());
            } else {
                SwitchToSection(new ReinstallNeededSection());
            }

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

        public void SwitchToSection(MenuSection section)
        {
            if (sectionStack.Count > 0) {
                //renderSetup.BeginPageTransition();
                sectionStack.Peek().OnHide(false);
            }

            sectionStack.Push(section);
            section.OnShow(this);
        }

        public void LeaveSection(MenuSection section)
        {
            if (sectionStack.Count > 0) {
                MenuSection activeSection = sectionStack.Pop();
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

        public void BeginFadeOut(Action action)
        {
            transitionAction = action;
            transitionManager = new TransitionManager(TransitionManager.Mode.FadeOut, MainMenuRenderSetup.TargetSize, true);
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

        public void DrawString(ref int charOffset, string text, float x,
            float y, Alignment align, ColorRgba? color, float scale = 1f, float angleOffset = 0f, float varianceX = 4f, float varianceY = 4f,
            float speed = 4f, float charSpacing = 1f, float lineSpacing = 1f)
        {
            if (transitionText > 0f) {
                angleOffset += 0.5f * transitionText;
                varianceX += 400f * transitionText;
                varianceY += 400f * transitionText;
                speed -= speed * 0.6f * transitionText;
            }

            fontSmall.DrawString(ref charOffset, text, x, y, align,
                color, scale, angleOffset, varianceX, varianceY, speed, charSpacing, lineSpacing);
        }

        public void DrawStringShadow(ref int charOffset, string text, float x,
            float y, Alignment align, ColorRgba? color, float scale = 1f, float angleOffset = 0f, float varianceX = 4f, float varianceY = 4f,
            float speed = 4f, float charSpacing = 1f, float lineSpacing = 1f)
        {
            if (transitionText > 0f) {
                angleOffset += 0.5f * transitionText;
                varianceX += 400f * transitionText;
                varianceY += 400f * transitionText;
                speed -= speed * 0.6f * transitionText;
            }

            int charOffsetShadow = charOffset;
            fontSmall.DrawString(ref charOffsetShadow, text, x, y + 2.8f * scale, align,
                new ColorRgba(0f, 0.29f), scale, angleOffset, varianceX, varianceY, speed, charSpacing, lineSpacing);
            fontSmall.DrawString(ref charOffset, text, x, y, align,
                color, scale, angleOffset, varianceX, varianceY, speed, charSpacing, lineSpacing);
        }

        public void DrawMaterial(string name, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            GraphicResource res;
            if (metadata.Graphics.TryGetValue(name, out res)) {
                res.Draw(canvas, x, y, alignment, color, scaleX, scaleY);
            }
        }

        public void DrawMaterial(string name, float x, float y, Alignment alignment, ColorRgba color, float scaleX, float scaleY, Rect texRect)
        {
            GraphicResource res;
            if (metadata.Graphics.TryGetValue(name, out res)) {
                res.Draw(canvas, x, y, alignment, color, scaleX, scaleY, texRect);
            }
        }

        public void DrawMaterial(string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            GraphicResource res;
            if (metadata.Graphics.TryGetValue(name, out res)) {
                res.Draw(canvas, frame, x, y, alignment, color, scaleX, scaleY);
            }
        }

        public void PlaySound(string name, float volume = 1f)
        {
            SoundResource res;
            if (metadata.Sounds.TryGetValue(name, out res)) {
                SoundInstance instance = DualityApp.Sound.PlaySound(res.Sound);
                instance.Volume = volume * SettingsCache.SfxVolume;
            }
        }

        public bool IsAnimationAvailable(string name)
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
            Vector2 size = device.TargetSize;

            Rect view = new Rect(size);
            AdjustVisibleZone(ref view);

            Vector2 center = size * 0.5f;

            canvas.Begin(device);

            int charOffset = 0;
            int charOffsetShadow = 0;

            RenderTexturedBackground(device);

            // Title
            DrawMaterial("MenuCarrot", -1, center.X - 76f, 64f + 2f, Alignment.Center, new ColorRgba(0f, 0.3f), 0.8f, 0.8f);
            DrawMaterial("MenuCarrot", -1, center.X - 76f, 64f, Alignment.Center, ColorRgba.White, 0.8f, 0.8f);

            fontMedium.DrawString(ref charOffsetShadow, "Jazz", center.X - 63f, 70f + 2f, Alignment.Left,
                new ColorRgba(0f, 0.32f), 0.75f, 1.63f, 3f, 3f, 0f, 0.92f);
            fontMedium.DrawString(ref charOffsetShadow, "2", center.X - 19f, 70f - 8f + 2f, Alignment.Left,
                new ColorRgba(0f, 0.32f), 0.5f, 0f, 0f, 0f, 0f);
            fontMedium.DrawString(ref charOffsetShadow, "Resurrection", center.X - 10f, 70f + 4f + 2.5f, Alignment.Left,
                new ColorRgba(0f, 0.3f), 0.5f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            fontMedium.DrawString(ref charOffset, "Jazz", center.X - 63f, 70f, Alignment.Left,
                new ColorRgba(0.54f, 0.44f, 0.34f, 0.5f), 0.75f, 1.63f, 3f, 3f, 0f, 0.92f);
            fontMedium.DrawString(ref charOffset, "2", center.X - 19f, 70f - 8f, Alignment.Left,
                new ColorRgba(0.54f, 0.44f, 0.34f, 0.5f), 0.5f, 0f, 0f, 0f, 0f);
            fontMedium.DrawString(ref charOffset, "Resurrection", center.X - 10f, 70f + 4f, Alignment.Left,
                new ColorRgba(0.6f, 0.42f, 0.42f, 0.5f), 0.5f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            // Version
            Vector2 bottomRight = size;
            bottomRight.X = view.X + view.W - 24f;
            bottomRight.Y -= 10f;
            DrawStringShadow(ref charOffset, "v" + App.AssemblyVersion, bottomRight.X, bottomRight.Y, Alignment.BottomRight,
                ColorRgba.TransparentBlack, 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            // Copyright
            Vector2 bottomLeft = bottomRight;
            bottomLeft.X = view.X + 24f;
            DrawStringShadow(ref charOffset, "© 2016-" + DateTime.Now.Year + "  Dan R.", bottomLeft.X, bottomLeft.Y, Alignment.BottomLeft,
                ColorRgba.TransparentBlack, 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.8f);

            // New version available
            if (newVersion != null) {
                DrawStringShadow(ref charOffset, "menu/update".T(), (bottomLeft.X + bottomRight.X) * 0.5f, bottomLeft.Y - 12, Alignment.Bottom,
                    new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f), 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.9f);

                DrawStringShadow(ref charOffset, newVersion, (bottomLeft.X + bottomRight.X) * 0.5f, bottomLeft.Y + 2, Alignment.Bottom,
                    new ColorRgba(0.6f, 0.4f, 0.3f, 0.5f), 0.7f, 0.4f, 1.2f, 1.2f, 7f, 0.9f);
            }

            // Current section
            if (sectionStack.Count > 0) {
                sectionStack.Peek().OnPaint(canvas, view);
            }

            DrawPlatformSpecific(size);

            if (transitionManager != null) {
                transitionManager.Draw(device, canvas);
                if (transitionManager.IsCompleted) {
                    if (transitionManager.ActiveMode != TransitionManager.Mode.FadeOut) {
                        transitionManager = null;
                    }

                    if (transitionAction != null) {
                        transitionAction();
                        transitionAction = null;
                    }
                }
            }

            if (transitionText > 0f) {
                transitionText -= 0.02f * Time.TimeMult;
            }

            if (transitionWhite > 0f) {
                canvas.State.SetMaterial(DrawTechnique.Add);
                canvas.State.ColorTint = new ColorRgba(transitionWhite);
                canvas.FillRect(0, 0, size.X, size.Y);
                transitionWhite -= 0.03f * Time.TimeMult;
            }

            canvas.End();
        }

        private void OnCheckUpdates(bool newAvailable, string version)
        {
            if (newAvailable) {
                newVersion = version;
            } else {
                newVersion = null;
            }
        }

        partial void InitPlatformSpecific();

        partial void DrawPlatformSpecific(Vector2 size);

        partial void AdjustVisibleZone(ref Rect view);

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

            void ICmpRenderer.GetCullingInfo(out CullingInfo info)
            {
                info.Position = Vector3.Zero;
                info.Radius = float.MaxValue;
                info.Visibility = VisibilityFlag.Group0 | VisibilityFlag.ScreenOverlay;
            }

            void ICmpRenderer.Draw(IDrawDevice device)
            {
                mainMenu.OnRender(device);
            }
        }
    }
}