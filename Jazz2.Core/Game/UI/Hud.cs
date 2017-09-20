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
        private CanvasBuffer canvasBuffer;
        private BitmapFont fontSmall;

        private Dictionary<string, GraphicResource> graphics;

        private Player owner;

        private WeaponType currentWeapon;
        private string currentWeaponString;
        private string levelText;
        private float levelTextTime;

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
            canvasBuffer = new CanvasBuffer();

            fontSmall = new BitmapFont("UI/font_small", 17, 18, 15, 32, 256, -2, canvasBuffer);

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
            Canvas c = new Canvas(device, canvasBuffer);

            Vector2 size = device.TargetSize;
            int charOffset = 0;
            int charOffsetShadow = 0;

            DrawDebugStrings(device, c);

            // Health & Lives
            {
                string currentPlayer;
                if (owner.PlayerType == PlayerType.Spaz) {
                    currentPlayer = "CharacterSpaz";
                } else if (owner.PlayerType == PlayerType.Lori) {
                    currentPlayer = "CharacterLori";
                } else {
                    currentPlayer = "CharacterJazz";
                }

                DrawMaterial(c, currentPlayer, -1, 36, size.Y + 1.6f, Alignment.BottomRight, new ColorRgba(0f, 0.4f));
                DrawMaterial(c, currentPlayer, -1, 36, size.Y, Alignment.BottomRight, ColorRgba.White);

                string healthString = new string('|', owner.Health);

                if (owner.Lives > 0) {
                    fontSmall.DrawString(device, ref charOffsetShadow, healthString, 36 - 3 - 0.5f, size.Y - 16 + 0.5f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                    fontSmall.DrawString(device, ref charOffsetShadow, healthString, 36 - 3 + 0.5f, size.Y - 16 - 0.5f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                    fontSmall.DrawString(device, ref charOffset, healthString, 36 - 3, size.Y - 16, Alignment.BottomLeft,
                        null, 0.7f, charSpacing: 1.1f);

                    fontSmall.DrawString(device, ref charOffsetShadow, "x" + owner.Lives.ToString(CultureInfo.InvariantCulture), 36 - 4, size.Y + 1f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.32f));
                    fontSmall.DrawString(device, ref charOffset, "x" + owner.Lives.ToString(CultureInfo.InvariantCulture), 36 - 4, size.Y,
                        Alignment.BottomLeft, ColorRgba.TransparentBlack);
                } else {
                    fontSmall.DrawString(device, ref charOffsetShadow, healthString, 36 - 3 - 0.5f, size.Y - 3 + 0.5f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                    fontSmall.DrawString(device, ref charOffsetShadow, healthString, 36 - 3 + 0.5f, size.Y - 3 - 0.5f,
                        Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                    fontSmall.DrawString(device, ref charOffset, healthString, 36 - 3, size.Y - 3, Alignment.BottomLeft,
                        null, 0.7f, charSpacing: 1.1f);
                }
            }

            // Weapon
            {
                GraphicResource res;
                if (graphics.TryGetValue(currentWeaponString, out res)) {
                    float y = size.Y;
                    if (res.Base.FrameDimensions.Y < 20) {
                        y -= MathF.Round((20 - res.Base.FrameDimensions.Y) * 0.5f);
                    }

                    DrawMaterial(c, currentWeaponString, -1, size.X - 40, y + 1.6f, Alignment.BottomRight, new ColorRgba(0f, 0.4f));
                    DrawMaterial(c, currentWeaponString, -1, size.X - 40, y, Alignment.BottomRight, ColorRgba.White);
                }
                
                string ammoCount;
                if (owner.WeaponAmmo[(int)currentWeapon] < 0) {
                    ammoCount = "x\x7f";
                } else {
                    ammoCount = "x" + owner.WeaponAmmo[(int)currentWeapon].ToString(CultureInfo.InvariantCulture);
                }
                fontSmall.DrawString(device, ref charOffsetShadow, ammoCount, size.X - 40, size.Y + 1f, Alignment.BottomLeft,
                    new ColorRgba(0f, 0.32f), charSpacing: 0.96f);
                fontSmall.DrawString(device, ref charOffset, ammoCount, size.X - 40, size.Y, Alignment.BottomLeft,
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
                    y = size.Y * 0.1f - (y * y);
                    alpha = MathF.Max(activeBossTime / TransitionTime, 0f);
                } else {
                    y = size.Y * 0.1f;
                    alpha = 1f;
                }

                float perc = 0.08f + 0.84f * activeBoss.Health / activeBoss.MaxHealth;

                DrawMaterial(c, "BossHealthBar", 0, size.X * 0.5f, y + 2f, Alignment.Center, new ColorRgba(0f, 0.1f * alpha));
                DrawMaterial(c, "BossHealthBar", 0, size.X * 0.5f, y + 1f, Alignment.Center, new ColorRgba(0f, 0.2f * alpha));

                DrawMaterial(c, "BossHealthBar", 0, size.X * 0.5f, y, Alignment.Center, new ColorRgba(1f, alpha));
                DrawMaterialClipped(c, "BossHealthBar", 1, size.X * 0.5f, y, Alignment.Center, new ColorRgba(1f, alpha), perc, 1f);
            }

            // Misc.
            DrawLevelText(device, size, ref charOffset);
            DrawCoins(device, c, size, ref charOffset);
            DrawGems(device, c, size, ref charOffset);

            DrawTouch(device, c, size);

#if !DEBUG && __ANDROID__
            fontSmall.DrawString(device, ref charOffset, Time.Fps.ToString(), 2, 2, Alignment.TopLeft, ColorRgba.TransparentBlack, 0.8f);
#endif
        }

        private void DrawLevelText(IDrawDevice device, Vector2 size, ref int charOffset)
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
                fontSmall.DrawString(device, ref charOffsetShadow, levelText, size.X * 0.5f + offset,
                    size.Y * 0.0346f + 2.5f,
                    Alignment.Top, new ColorRgba(0f, 0.3f), 1f, 0.72f, 0.8f, 0.8f);

                fontSmall.DrawString(device, ref charOffset, levelText, size.X * 0.5f + offset, size.Y * 0.0346f,
                    Alignment.Top, ColorRgba.TransparentBlack, 1f, 0.72f, 0.8f, 0.8f);

                levelTextTime += Time.TimeMult;
                if (levelTextTime > TotalTime) {
                    levelText = null;
                }
            }
        }

        private void DrawCoins(IDrawDevice device, Canvas c, Vector2 size, ref int charOffset)
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

                DrawMaterial(c, "PickupCoin", -1, size.X * 0.5f, size.Y * 0.92f + 2.5f + offset, Alignment.Right,
                    new ColorRgba(0f, 0.2f * alpha), 0.8f, 0.8f);
                DrawMaterial(c, "PickupCoin", -1, size.X * 0.5f, size.Y * 0.92f + offset, Alignment.Right,
                    new ColorRgba(1f, alpha * alpha), 0.8f, 0.8f);

                int charOffsetShadow = charOffset;
                fontSmall.DrawString(device, ref charOffsetShadow, text, size.X * 0.5f, size.Y * 0.92f + 2.5f + offset,
                    Alignment.Left, new ColorRgba(0f, 0.3f * alpha), 1f, 0f, 0f, 0f);

                fontSmall.DrawString(device, ref charOffset, text, size.X * 0.5f, size.Y * 0.92f + offset,
                    Alignment.Left, new ColorRgba(0.5f, 0.5f * alpha), 1f, 0f, 0f, 0f);

                if (coinsTime > TotalTime) {
                    coinsTime = -1f;
                } else {
                    coinsTime += Time.TimeMult;
                }
            }
        }

        private void DrawGems(IDrawDevice device, Canvas c, Vector2 size, ref int charOffset)
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
                DrawMaterial(c, "PickupGem", -1, size.X * 0.5f, size.Y * 0.92f + 2.5f + offset, Alignment.Right,
                    new ColorRgba(0f, 0.4f * animAlpha), 0.8f, 0.8f);
                DrawMaterial(c, "PickupGem", -1, size.X * 0.5f, size.Y * 0.92f + offset, Alignment.Right,
                    new ColorRgba(1f, animAlpha), 0.8f, 0.8f);

                int charOffsetShadow = charOffset;
                fontSmall.DrawString(device, ref charOffsetShadow, text, size.X * 0.5f, size.Y * 0.92f + 2.5f + offset,
                    Alignment.Left, new ColorRgba(0f, 0.3f * alpha), 1f, 0f, 0f, 0f);

                fontSmall.DrawString(device, ref charOffset, text, size.X * 0.5f, size.Y * 0.92f + offset,
                    Alignment.Left, new ColorRgba(0.5f, 0.5f * alpha), 1f, 0f, 0f, 0f);

                if (gemsTime > TotalTime) {
                    gemsTime = -1f;
                } else {
                    gemsTime += Time.TimeMult;
                }
            }
        }

        partial void DrawTouch(IDrawDevice device, Canvas c, Vector2 size);

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
        private void DrawDebugStrings(IDrawDevice device, Canvas c)
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
                    Vector2 originPos = new Vector2(device.TargetSize.X, 0f);
                    Alignment.TopRight.ApplyTo(ref originPos, new Vector2(paletteTexture.Res.InternalWidth, paletteTexture.Res.InternalHeight));

                    c.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, ColorRgba.White, paletteTexture));
                    c.State.ColorTint = ColorRgba.White;
                    c.FillRect((int)originPos.X, (int)originPos.Y, paletteTexture.Res.InternalWidth, paletteTexture.Res.InternalHeight);
                }

                // Render debug strings
                int charOffset = 0;
                fontSmall.DrawString(device, ref charOffset, debugString.ToString(),
                    x, y, Alignment.TopLeft, ColorRgba.TransparentBlack,
                    0.65f, charSpacing: 0.9f, lineSpacing: 0.9f);

                // Render debug rectangles
                c.State.SetMaterial(new BatchInfo {
                    Technique = DrawTechnique.Alpha,
                    MainColor = new ColorRgba(1f, 0.8f)
                });

                Vector2 offset = device.TargetSize * 0.5f - device.RefCoord.Xy;
                for (int i = 0; i < debugRects.Count; i++) {
                    Rect rect = debugRects[i];
                    c.DrawRect(rect.X + offset.X, rect.Y + offset.Y, rect.W, rect.H);
                }
            }

            debugString.Clear();
            debugRects.Clear();

            if (DualityApp.Keyboard.KeyHit(Key.D)) {
                enableDebug ^= true;
            }
#endif
        }

        public void DrawMaterial(Canvas c, string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float scaleX = 1f, float scaleY = 1f)
        {
            GraphicResource res;
            if (graphics.TryGetValue(name, out res)) {
                res.Draw(c, frame, x, y, alignment, color, scaleX, scaleY);
            }
        }

        public void DrawMaterialClipped(Canvas c, string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float clipX, float clipY)
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

                c.State.SetMaterial(res.Material);
                c.State.ColorTint = color;
                c.State.TextureCoordinateRect = uv;
                c.FillRect((int)originPos.X, (int)originPos.Y, w * clipX, h * clipY);
            }
        }

        public void ChangeCurrentWeapon(WeaponType weapon, byte upgrades)
        {
            currentWeapon = weapon;

            if ((upgrades & 0x1) != 0) {
                switch (weapon) {
                    case WeaponType.Blaster:
                        if (owner.PlayerType == PlayerType.Spaz) {
                            currentWeaponString = "WeaponPowerUpBlasterSpaz";
                        } else if (owner.PlayerType == PlayerType.Lori) {
                            currentWeaponString = "WeaponPowerUpBlasterLori";
                        } else {
                            currentWeaponString = "WeaponPowerUpBlasterJazz";
                        }
                        break;

                    default:
                        currentWeaponString = "WeaponPowerUp" + weapon.ToString("G");
                        break;
                }
            } else {
                switch (weapon) {
                    case WeaponType.Blaster:
                        if (owner.PlayerType == PlayerType.Spaz) {
                            currentWeaponString = "WeaponBlasterSpaz";
                        } else if (owner.PlayerType == PlayerType.Lori) {
                            currentWeaponString = "WeaponBlasterLori";
                        } else {
                            currentWeaponString = "WeaponBlasterJazz";
                        }
                        break;

                    default:
                        currentWeaponString = "Weapon" + weapon.ToString("G");
                        break;
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
    }
}