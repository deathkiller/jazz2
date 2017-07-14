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

namespace Jazz2.Game
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

#if DEBUG
        private static StringBuilder debugString;
        private static List<Rect> debugRects;
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
            set { activeBoss = value; }
        }

        public Hud()
        {
            canvasBuffer = new CanvasBuffer();

            fontSmall = new BitmapFont("UI/font_small", 17, 18, 15, 32, 256, -2, canvasBuffer);

            // ToDo: Pass palette from LevelHandler to adjust HUD colors
            Metadata m = ContentResolver.Current.RequestMetadata("UI/HUD", null);
            graphics = m.Graphics;

#if DEBUG
            debugString = new StringBuilder();
            debugRects = new List<Rect>();
#endif
        }

        float ICmpRenderer.BoundRadius => float.MaxValue;

        void ICmpRenderer.Draw(IDrawDevice device)
        {
            Canvas c = new Canvas(device, canvasBuffer);

            Vector2 size = device.TargetSize;
            int charOffset = 0;
            int charOffsetShadow = 0;

            DrawDebugStrings(device, c);

            // Health & Lives
            string currentPlayer;
            if (owner.PlayerType == PlayerType.Spaz) {
                currentPlayer = "CharacterSpaz";
            } else if (owner.PlayerType == PlayerType.Lori) {
                currentPlayer = "CharacterLori";
            } else {
                currentPlayer = "CharacterJazz";
            }

            DrawMaterial(c, currentPlayer, 36, size.Y + 1.6f, Alignment.BottomRight, new ColorRgba(0f, 0.4f));
            DrawMaterial(c, currentPlayer, 36, size.Y, Alignment.BottomRight, ColorRgba.White);

            string healthString = new string('|', owner.Health);

            if (owner.Lives > 0) {
                fontSmall.DrawString(device, ref charOffsetShadow, healthString, 36 - 3 - 0.5f, size.Y - 16 + 0.5f,
                    Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                fontSmall.DrawString(device, ref charOffsetShadow, healthString, 36 - 3 + 0.5f, size.Y - 16 - 0.5f,
                    Alignment.BottomLeft, new ColorRgba(0f, 0.42f), 0.7f, charSpacing: 1.1f);
                fontSmall.DrawString(device, ref charOffset, healthString, 36 - 3, size.Y - 16, Alignment.BottomLeft,
                    null, 0.7f, charSpacing: 1.1f);

                fontSmall.DrawString(device, ref charOffsetShadow, "x" + owner.Lives.ToString(CultureInfo.InvariantCulture), 36 - 4, size.Y + 1f,
                    Alignment.BottomLeft, new ColorRgba(0f, 0.3f));
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

            // Weapon
            DrawMaterial(c, currentWeaponString, size.X - 40, size.Y + 1.6f, Alignment.BottomRight, new ColorRgba(0f, 0.4f));
            DrawMaterial(c, currentWeaponString, size.X - 40, size.Y, Alignment.BottomRight, ColorRgba.White);
            string ammoCount;
            if (owner.WeaponAmmo[(int)currentWeapon] < 0) {
                ammoCount = "x\x7f";
            } else {
                ammoCount = "x" + owner.WeaponAmmo[(int)currentWeapon].ToString(CultureInfo.InvariantCulture);
            }
            fontSmall.DrawString(device, ref charOffsetShadow, ammoCount, size.X - 40, size.Y + 1f, Alignment.BottomLeft,
                new ColorRgba(0f, 0.3f), charSpacing: 0.96f);
            fontSmall.DrawString(device, ref charOffset, ammoCount, size.X - 40, size.Y, Alignment.BottomLeft,
                ColorRgba.TransparentBlack, charSpacing: 0.96f);

            // Active Boss
            if (activeBoss != null && activeBoss.MaxHealth != int.MaxValue) {
                fontSmall.DrawString(device, ref charOffset, "Boss", 8, 8, Alignment.TopLeft,
                    new ColorRgba(0.4f, 0.5f), charSpacing: 0.75f);

                const int max = 40;
                int healthPercentage = (max * activeBoss.Health / activeBoss.MaxHealth);
                string healthBoss1String = new string('|', max);
                string healthBoss2String = new string('|', healthPercentage);

                fontSmall.DrawString(device, ref charOffsetShadow, healthBoss1String, 50 - 0.5f, 12 + 0.5f,
                    Alignment.TopLeft, new ColorRgba(0f, 0.42f), 0.65f, charSpacing: 1.1f);
                fontSmall.DrawString(device, ref charOffsetShadow, healthBoss1String, 50 + 0.5f, 12 - 0.5f,
                    Alignment.TopLeft, new ColorRgba(0f, 0.42f), 0.65f, charSpacing: 1.1f);
                fontSmall.DrawString(device, ref charOffset, healthBoss2String, 50, 12, Alignment.TopLeft,
                    new ColorRgba(0.4f, 0.5f), 0.65f, charSpacing: 1.1f);
            }

            // Misc.
            DrawLevelText(device, size, ref charOffset);
            DrawCoins(device, c, size, ref charOffset);
            DrawGems(device, c, size, ref charOffset);

            DrawTouch(device, c, size);
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

                DrawMaterial(c, "PickupCoin", size.X * 0.5f, size.Y * 0.92f + 2.5f + offset, Alignment.Right,
                    new ColorRgba(0f, 0.2f * alpha), 0.8f);
                DrawMaterial(c, "PickupCoin", size.X * 0.5f, size.Y * 0.92f + offset, Alignment.Right,
                    new ColorRgba(1f, alpha * alpha), 0.8f);

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

                DrawMaterial(c, "PickupGem", size.X * 0.5f, size.Y * 0.92f + 2.5f + offset, Alignment.Right,
                    new ColorRgba(0f, 0.4f * alpha), 0.8f);
                DrawMaterial(c, "PickupGem", size.X * 0.5f, size.Y * 0.92f + offset, Alignment.Right,
                    new ColorRgba(210, 110, 145, (byte)(190 * alpha)), 0.8f);

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

        bool ICmpRenderer.IsVisible(IDrawDevice device)
        {
            return (device.VisibilityMask & VisibilityFlag.ScreenOverlay) != 0;
        }

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
                int charOffset = 0;
                fontSmall.DrawString(device, ref charOffset, debugString.ToString(),
                    x, y, Alignment.TopLeft, ColorRgba.TransparentBlack,
                    0.65f, charSpacing: 0.9f, lineSpacing: 0.9f);

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

        private void DrawMaterial(Canvas c, string id, float x, float y, Alignment alignment, ColorRgba color,
            float scale = 1f)
        {
            GraphicResource res;
            if (!graphics.TryGetValue(id, out res)) {
                return;
            }

            // ToDo: HUD Animations are slowed down to 0.86f, adjust this in Metadata files
            //int curAnimFrame = (int)(Time.GameTimer.TotalSeconds * res.FrameCount / res.FrameDuration) % res.FrameCount;
            int curAnimFrame = (int)(Time.GameTimer.TotalSeconds * 0.86f * res.FrameCount / res.FrameDuration) %
                               res.FrameCount;

            Vector2 originPos = new Vector2(x, y);
            alignment.ApplyTo(ref originPos, new Vector2(res.FrameDimensions.X * scale, res.FrameDimensions.Y * scale));

            c.State.SetMaterial(res.Material);
            c.State.ColorTint = color;
            c.State.TextureCoordinateRect = res.Material.Res.MainTexture.Res.LookupAtlas(curAnimFrame);

            c.FillRect(originPos.X, originPos.Y, res.FrameDimensions.X * scale, res.FrameDimensions.Y * scale);
        }

        public void ChangeCurrentWeapon(WeaponType weapon, byte upgrades)
        {
            currentWeapon = weapon;

            if ((upgrades & 0x1) != 0) {
                switch (weapon) {
                    case WeaponType.Blaster:
                        if (owner.PlayerType == PlayerType.Spaz) {
                            currentWeaponString = "WeaponPowerupBlasterSpaz";
                        } else if (owner.PlayerType == PlayerType.Lori) {
                            currentWeaponString = "WeaponPowerupBlasterLori";
                        } else {
                            currentWeaponString = "WeaponPowerupBlasterJazz";
                        }
                        break;

                    case WeaponType.Bouncer: currentWeaponString = "WeaponPowerupBouncer"; break;
                    case WeaponType.Freezer: currentWeaponString = "WeaponPowerupFreezer"; break;
                    case WeaponType.Seeker: currentWeaponString = "WeaponPowerupSeeker"; break;
                    case WeaponType.RF: currentWeaponString = "WeaponPowerupRF"; break;
                    case WeaponType.Toaster: currentWeaponString = "WeaponPowerupToaster"; break;
                    case WeaponType.TNT: currentWeaponString = "WeaponPowerupTNT"; break;
                    case WeaponType.Pepper: currentWeaponString = "WeaponPowerupPepper"; break;
                    case WeaponType.Electro: currentWeaponString = "WeaponPowerupElectro"; break;
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

                    case WeaponType.Bouncer: currentWeaponString = "WeaponBouncer"; break;
                    case WeaponType.Freezer: currentWeaponString = "WeaponFreezer"; break;
                    case WeaponType.Seeker: currentWeaponString = "WeaponSeeker"; break;
                    case WeaponType.RF: currentWeaponString = "WeaponRF"; break;
                    case WeaponType.Toaster: currentWeaponString = "WeaponToaster"; break;
                    case WeaponType.TNT: currentWeaponString = "WeaponTNT"; break;
                    case WeaponType.Pepper: currentWeaponString = "WeaponPepper"; break;
                    case WeaponType.Electro: currentWeaponString = "WeaponElectro"; break;
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