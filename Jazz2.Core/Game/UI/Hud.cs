using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Duality.Resources;
using Jazz2.Actors;
using Jazz2.Actors.Bosses;
using Jazz2.Game.Structs;

namespace Jazz2.Game.UI
{
    public partial class Hud : Component, ICmpRenderer
    {
        private Canvas canvas;
        private BitmapFont fontSmall;
        private Dictionary<string, GraphicResource> graphics;

        private Player owner;

        private string levelText;
        private float levelTextTime;

        private TransitionManager transitionManager;

        private int coins, gems;
        private float coinsTime = -1f;
        private float gemsTime = -1f;

        private BossBase activeBoss;
        private float activeBossTime;

#if DEBUG
        private static StringBuilder debugString = new StringBuilder();
        private static List<Rect> debugRects = new List<Rect>();
        private bool enableDebug;
#endif

        public Player Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        public BossBase ActiveBoss
        {
            get { return activeBoss; }
            set {
                activeBoss = value;
                activeBossTime = 0f;
            }
        }

        public Hud()
        {
            canvas = new Canvas();

            fontSmall = new BitmapFont(canvas, "UI/font_small", 17, 18, 15, 32, 256, -2);

            // ToDo: Pass palette from LevelHandler to adjust HUD colors
            Metadata m = ContentResolver.Current.RequestMetadata("UI/HUD");
            graphics = m.Graphics;
        }

        void ICmpRenderer.GetCullingInfo(out CullingInfo info)
        {
            info.Position = Vector3.Zero;
            info.Radius = float.MaxValue;
            info.Visibility = VisibilityFlag.Group0 | VisibilityFlag.ScreenOverlay;
        }

        void ICmpRenderer.Draw(IDrawDevice device)
        {
            canvas.Begin(device);

            Vector2 size = device.TargetSize;

            Rect view = new Rect(size);
            AdjustVisibleZone(ref view);

            float right = view.X + view.W;
            float bottom = view.Y + view.H;

            int charOffset = 0;
            int charOffsetShadow = 0;

            DrawDebugStrings();

            // Health & Lives
            {
                string currentPlayer;
                if (owner.PlayerType == PlayerType.Spaz) {
                    currentPlayer = "CharacterSpaz";
                } else if (owner.PlayerType == PlayerType.Lori) {
                    currentPlayer = "CharacterLori";
                } else if (owner.PlayerType == PlayerType.Frog) {
                    currentPlayer = "CharacterFrog";
                } else {
                    currentPlayer = "CharacterJazz";
                }

                DrawMaterial(currentPlayer, -1, view.X + 36, bottom + 1.6f, Alignment.BottomRight, new ColorRgba(0f, 0.4f));
                DrawMaterial(currentPlayer, -1, view.X + 36, bottom, Alignment.BottomRight, ColorRgba.White);

                string healthString = new string('|', owner.Health);

                if (owner.Lives > 0) {
                    fontSmall.DrawString(ref charOffsetShadow, healthString, view.X + 36 - 3 - 0.5f, bottom - 16 + 0.5f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                    fontSmall.DrawString(ref charOffsetShadow, healthString, view.X + 36 - 3 + 0.5f, bottom - 16 - 0.5f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                    fontSmall.DrawString(ref charOffset, healthString, view.X + 36 - 3, bottom - 16, Alignment.BottomLeft,
                        null, 0.7f, charSpacing: 1.1f);

                    fontSmall.DrawString(ref charOffsetShadow, "x" + owner.Lives.ToString(CultureInfo.InvariantCulture), view.X + 36 - 4, bottom + 1f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.32f));
                    fontSmall.DrawString(ref charOffset, "x" + owner.Lives.ToString(CultureInfo.InvariantCulture), view.X + 36 - 4, bottom,
                        Alignment.BottomLeft, ColorRgba.TransparentBlack);
                } else {
                    fontSmall.DrawString(ref charOffsetShadow, healthString, view.X + 36 - 3 - 0.5f, bottom - 3 + 0.5f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                    fontSmall.DrawString(ref charOffsetShadow, healthString, view.X + 36 - 3 + 0.5f, bottom - 3 - 0.5f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                    fontSmall.DrawString(ref charOffset, healthString, view.X + 36 - 3, bottom - 3, Alignment.BottomLeft,
                        null, 0.7f, charSpacing: 1.1f);
                }
            }

            // Weapon
            {
                WeaponType weapon = owner.CurrentWeapon;
                string currentWeaponString = GetCurrentWeapon(weapon);

                GraphicResource res;
                if (graphics.TryGetValue(currentWeaponString, out res)) {
                    float y = bottom;
                    if (res.Base.FrameDimensions.Y < 20) {
                        y -= MathF.Round((20 - res.Base.FrameDimensions.Y) * 0.5f);
                    }

                    DrawMaterial(currentWeaponString, -1, right - 40, y + 1.6f, Alignment.BottomRight, new ColorRgba(0f, 0.4f));
                    DrawMaterial(currentWeaponString, -1, right - 40, y, Alignment.BottomRight, ColorRgba.White);
                }

                string ammoCount;
                if (owner.WeaponAmmo[(int)weapon] < 0) {
                    ammoCount = "x\x7f";
                } else {
                    ammoCount = "x" + owner.WeaponAmmo[(int)weapon].ToString(CultureInfo.InvariantCulture);
                }
                fontSmall.DrawString(ref charOffsetShadow, ammoCount, right - 40, bottom + 1f, Alignment.BottomLeft,
                    new ColorRgba(0f, 0.32f), charSpacing: 0.96f);
                fontSmall.DrawString(ref charOffset, ammoCount, right - 40, bottom, Alignment.BottomLeft,
                    ColorRgba.TransparentBlack, charSpacing: 0.96f);
            }

            // Active Boss (health bar)
            if (activeBoss != null && activeBoss.MaxHealth != int.MaxValue) {
                const float TransitionTime = 60f;
                float y, alpha;
                if (activeBossTime < TransitionTime) {
                    activeBossTime += Time.TimeMult;

                    if (activeBossTime > TransitionTime) {
                        activeBossTime = TransitionTime;
                    }

                    y = (TransitionTime - activeBossTime) / 8f;
                    y = bottom * 0.1f - (y * y);
                    alpha = MathF.Max(activeBossTime / TransitionTime, 0f);
                } else {
                    y = bottom * 0.1f;
                    alpha = 1f;
                }

                float perc = 0.08f + 0.84f * activeBoss.Health / activeBoss.MaxHealth;

                DrawMaterial("BossHealthBar", 0, size.X * 0.5f, y + 2f, Alignment.Center, new ColorRgba(0f, 0.1f * alpha));
                DrawMaterial("BossHealthBar", 0, size.X * 0.5f, y + 1f, Alignment.Center, new ColorRgba(0f, 0.2f * alpha));

                DrawMaterial("BossHealthBar", 0, size.X * 0.5f, y, Alignment.Center, new ColorRgba(1f, alpha));
                DrawMaterialClipped("BossHealthBar", 1, size.X * 0.5f, y, Alignment.Center, new ColorRgba(1f, alpha), perc, 1f);
            }

            // Misc.
            DrawLevelText(size, ref charOffset);
            DrawCoins(size, ref charOffset);
            DrawGems(size, ref charOffset);

            DrawPlatformSpecific(size);

            if (transitionManager != null) {
                transitionManager.Draw(device, canvas);
                if (transitionManager.IsCompleted && transitionManager.ActiveMode != TransitionManager.Mode.FadeOut) {
                    transitionManager = null;
                }
            }

            canvas.End();
        }

        private void DrawLevelText(Vector2 size, ref int charOffset)
        {
            if (levelText != null) {
                const float StillTime = 350f;
                const float TransitionTime = 100f;
                const float TotalTime = StillTime + TransitionTime * 2f;

                float offset;
                if (levelTextTime < TransitionTime) {
                    offset = MathF.Pow((TransitionTime - levelTextTime) / 12f, 3);
                } else if (levelTextTime > TransitionTime + StillTime) {
                    offset = -MathF.Pow((levelTextTime - TransitionTime - StillTime) / 12f, 3);
                } else {
                    offset = 0;
                }

                int charOffsetShadow = charOffset;
                fontSmall.DrawString(ref charOffsetShadow, levelText, size.X * 0.5f + offset,
                    size.Y * 0.0346f + 2.5f,
                    Alignment.Top, new ColorRgba(0f, 0.3f), 1f, 0.72f, 0.8f, 0.8f);

                fontSmall.DrawString(ref charOffset, levelText, size.X * 0.5f + offset, size.Y * 0.0346f,
                    Alignment.Top, ColorRgba.TransparentBlack, 1f, 0.72f, 0.8f, 0.8f);

                levelTextTime += Time.TimeMult;
                if (levelTextTime > TotalTime) {
                    levelText = null;
                }
            }
        }

        private void DrawCoins(Vector2 size, ref int charOffset)
        {
            if (coinsTime >= 0f) {
                const float StillTime = 120f;
                const float TransitionTime = 60f;
                const float TotalTime = StillTime + TransitionTime * 2f;

                string text = "x" + coins.ToString(CultureInfo.InvariantCulture);

                float offset, alpha;
                if (coinsTime < TransitionTime) {
                    offset = (TransitionTime - coinsTime) / 10f;
                    offset = -(offset * offset);
                    alpha = MathF.Max(coinsTime / TransitionTime, 0.1f);
                } else if (coinsTime > TransitionTime + StillTime) {
                    offset = (coinsTime - TransitionTime - StillTime) / 10f;
                    offset = (offset * offset);
                    alpha = (TotalTime - coinsTime) / TransitionTime;
                } else {
                    offset = 0f;
                    alpha = 1f;
                }

                DrawMaterial("PickupCoin", -1, size.X * 0.5f, size.Y * 0.92f + 2.5f + offset, Alignment.Right,
                    new ColorRgba(0f, 0.2f * alpha), 0.8f, 0.8f);
                DrawMaterial("PickupCoin", -1, size.X * 0.5f, size.Y * 0.92f + offset, Alignment.Right,
                    new ColorRgba(1f, alpha * alpha), 0.8f, 0.8f);

                int charOffsetShadow = charOffset;
                fontSmall.DrawString(ref charOffsetShadow, text, size.X * 0.5f, size.Y * 0.92f + 2.5f + offset,
                    Alignment.Left, new ColorRgba(0f, 0.3f * alpha), 1f, 0f, 0f, 0f);

                fontSmall.DrawString(ref charOffset, text, size.X * 0.5f, size.Y * 0.92f + offset,
                    Alignment.Left, new ColorRgba(0.5f, 0.5f * alpha), 1f, 0f, 0f, 0f);

                if (coinsTime > TotalTime) {
                    coinsTime = -1f;
                } else {
                    coinsTime += Time.TimeMult;
                }
            }
        }

        private void DrawGems(Vector2 size, ref int charOffset)
        {
            if (gemsTime >= 0f) {
                const float StillTime = 120f;
                const float TransitionTime = 60f;
                const float TotalTime = StillTime + TransitionTime * 2f;

                string text = "x" + gems.ToString(CultureInfo.InvariantCulture);

                float offset, alpha;
                if (gemsTime < TransitionTime) {
                    offset = (TransitionTime - gemsTime) / 10f;
                    offset = -(offset * offset);
                    alpha = MathF.Max(gemsTime / TransitionTime, 0.1f);
                } else if (gemsTime > TransitionTime + StillTime) {
                    offset = (gemsTime - TransitionTime - StillTime) / 10f;
                    offset = (offset * offset);
                    alpha = (TotalTime - gemsTime) / TransitionTime;
                } else {
                    offset = 0f;
                    alpha = 1f;
                }

                float animAlpha = alpha * alpha;
                DrawMaterial("PickupGem", -1, size.X * 0.5f, size.Y * 0.92f + 2.5f + offset, Alignment.Right,
                    new ColorRgba(0f, 0.4f * animAlpha), 0.8f, 0.8f);
                DrawMaterial("PickupGem", -1, size.X * 0.5f, size.Y * 0.92f + offset, Alignment.Right,
                    new ColorRgba(1f, animAlpha), 0.8f, 0.8f);

                int charOffsetShadow = charOffset;
                fontSmall.DrawString(ref charOffsetShadow, text, size.X * 0.5f, size.Y * 0.92f + 2.5f + offset,
                    Alignment.Left, new ColorRgba(0f, 0.3f * alpha), 1f, 0f, 0f, 0f);

                fontSmall.DrawString(ref charOffset, text, size.X * 0.5f, size.Y * 0.92f + offset,
                    Alignment.Left, new ColorRgba(0.5f, 0.5f * alpha), 1f, 0f, 0f, 0f);

                if (gemsTime > TotalTime) {
                    gemsTime = -1f;
                } else {
                    gemsTime += Time.TimeMult;
                }
            }
        }

        partial void DrawPlatformSpecific(Vector2 size);

        partial void AdjustVisibleZone(ref Rect view);

        [Conditional("DEBUG")]
        public static void ShowDebugText(string text)
        {
#if DEBUG
            debugString.AppendLine(text);
#endif
        }

        [Conditional("DEBUG")]
        public static void ShowDebugRect(Rect rect)
        {
#if DEBUG
            debugRects.Add(rect);
#endif
        }

        [Conditional("DEBUG")]
        private void DrawDebugStrings()
        {
#if DEBUG
            const int x = 4, y = 4;

            if (enableDebug) {
                // Palette debugging
                ContentRef<Texture> paletteTexture = ContentResolver.Current.Palette;
                if (paletteTexture.IsExplicitNull) {
                    debugString.AppendLine("- Palette not initialized!");
                } else {
                    // Show palette in upper right corner
                    Vector2 originPos = new Vector2(canvas.DrawDevice.TargetSize.X, 0f);
                    Alignment.TopRight.ApplyTo(ref originPos, new Vector2(paletteTexture.Res.InternalWidth, paletteTexture.Res.InternalHeight));

                    BatchInfo material = canvas.DrawDevice.RentMaterial();
                    material.Technique = DrawTechnique.Alpha;
                    material.MainTexture = paletteTexture;
                    canvas.State.SetMaterial(material);

                    canvas.State.ColorTint = ColorRgba.White;
                    canvas.FillRect((int)originPos.X, (int)originPos.Y, paletteTexture.Res.InternalWidth, paletteTexture.Res.InternalHeight);
                }

                // Render debug strings
                int charOffset = 0;
                fontSmall.DrawString(ref charOffset, debugString.ToString(),
                    x, y, Alignment.TopLeft, ColorRgba.TransparentBlack,
                    0.65f, charSpacing: 0.9f, lineSpacing: 0.9f);

                // Render debug rectangles
                {
                    BatchInfo material = canvas.DrawDevice.RentMaterial();
                    material.Technique = DrawTechnique.Alpha;
                    material.MainColor = new ColorRgba(1f, 0.8f);
                    canvas.State.SetMaterial(material);

                    Vector2 offset = canvas.DrawDevice.TargetSize * 0.5f - canvas.DrawDevice.ViewerPos.Xy;
                    for (int i = 0; i < debugRects.Count; i++) {
                        Rect rect = debugRects[i];
                        canvas.DrawRect(rect.X + offset.X, rect.Y + offset.Y, rect.W, rect.H);
                    }
                }
            }

            debugString.Clear();
            debugRects.Clear();

            if (DualityApp.Keyboard.KeyHit(Key.D)) {
                enableDebug ^= true;
            }
#endif
        }

        public void DrawMaterial(string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            GraphicResource res;
            if (graphics.TryGetValue(name, out res)) {
                res.Draw(canvas, frame, x, y, alignment, color, scaleX, scaleY);
            }
        }

        public void DrawMaterialClipped(string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float clipX, float clipY)
        {
            GraphicResource res;
            if (graphics.TryGetValue(name, out res)) {
                Texture texture = res.Material.Res.MainTexture.Res;

                if (frame < 0) {
                    frame = (int)(Time.GameTimer.TotalSeconds * 0.86f * res.FrameCount / res.FrameDuration) % res.FrameCount;
                }

                Rect uv = texture.LookupAtlas(frame);
                float w = texture.InternalWidth * uv.W;
                float h = texture.InternalHeight * uv.H;

                uv.W *= clipX;
                uv.H *= clipY;

                Vector2 originPos = new Vector2(x, y);
                alignment.ApplyTo(ref originPos, new Vector2(w, h));

                canvas.State.SetMaterial(res.Material);
                canvas.State.ColorTint = color;
                canvas.State.TextureCoordinateRect = uv;
                canvas.FillRect((int)originPos.X, (int)originPos.Y, w * clipX, h * clipY);
            }
        }

        public string GetCurrentWeapon(WeaponType weapon)
        {
            if ((owner.WeaponUpgrades[(int)weapon] & 0x1) != 0) {
                switch (weapon) {
                    case WeaponType.Blaster:
                        if (owner.PlayerType == PlayerType.Spaz) {
                            return "WeaponPowerUpBlasterSpaz";
                        } else if (owner.PlayerType == PlayerType.Lori) {
                            return "WeaponPowerUpBlasterLori";
                        } else {
                            return "WeaponPowerUpBlasterJazz";
                        }

                    default:
                        return "WeaponPowerUp" + weapon.ToString("G");
                }
            } else {
                switch (weapon) {
                    case WeaponType.Blaster:
                        if (owner.PlayerType == PlayerType.Spaz) {
                            return "WeaponBlasterSpaz";
                        } else if (owner.PlayerType == PlayerType.Lori) {
                            return "WeaponBlasterLori";
                        } else {
                            return "WeaponBlasterJazz";
                        }

                    default:
                        return "Weapon" + weapon.ToString("G");
                }
            }
        }

        public void ShowLevelText(string text)
        {
            if (levelText == text) {
                return;
            }

            levelText = text;
            levelTextTime = 0f;
        }

        public void ShowCoins(int coins)
        {
            const float StillTime = 120f;
            const float TransitionTime = 60f;

            this.coins = coins;

            if (coinsTime < 0f) {
                coinsTime = 0f;
            } else if (coinsTime > TransitionTime) {
                coinsTime = TransitionTime;
            }

            if (gemsTime >= 0f) {
                if (gemsTime <= TransitionTime + StillTime) {
                    gemsTime = TransitionTime + StillTime;
                } else {
                    gemsTime = -1f;
                }
            }
        }

        public void ShowGems(int gems)
        {
            const float StillTime = 120f;
            const float TransitionTime = 60f;

            this.gems = gems;

            if (gemsTime < 0f) {
                gemsTime = 0f;
            } else if (gemsTime > TransitionTime) {
                gemsTime = TransitionTime;
            }

            if (coinsTime >= 0f) {
                if (coinsTime <= TransitionTime + StillTime) {
                    coinsTime = TransitionTime + StillTime;
                } else {
                    coinsTime = -1f;
                }
            }
        }

        public void BeginFadeIn(bool smooth)
        {
            transitionManager = new TransitionManager(TransitionManager.Mode.FadeIn, LevelRenderSetup.TargetSize, smooth);
        }

        public void BeginFadeOut(bool smooth)
        {
            transitionManager = new TransitionManager(TransitionManager.Mode.FadeOut, LevelRenderSetup.TargetSize, smooth);
        }
    }
}