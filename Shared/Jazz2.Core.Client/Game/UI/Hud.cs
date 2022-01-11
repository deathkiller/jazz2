using System;
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
using MathF = Duality.MathF;

namespace Jazz2.Game.UI
{
    public partial class Hud : Component, ICmpRenderer
    {
        private Canvas canvas;
        private BitmapFont fontSmall, fontMedium;
        private Dictionary<string, GraphicResource> graphics;

        private ILevelHandler levelHandler;
        private Player owner;

        private string levelText;
        private float levelTextTime;
        private bool levelTextBigger;

        private TransitionManager transitionManager;

        private int coins, gems;
        private float coinsTime = -1f;
        private float gemsTime = -1f;

        private float weaponWheelAnim;
        private int lastWeaponWheelIndex;

        private BossBase activeBoss;
        private float activeBossTime;

#if DEBUG
        private static StringBuilder debugString = new StringBuilder();
        private static List<Rect> debugRects = new List<Rect>();
        private bool enableDebug;
#endif

        public ILevelHandler LevelHandler
        {
            get { return levelHandler; }
            set { levelHandler = value; }
        }

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

            fontSmall = new BitmapFont(canvas, "_custom/font_small");
            fontMedium = new BitmapFont(canvas, "_custom/font_medium");

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

            if (owner != null) {
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

                        string livesString = "x" + owner.Lives.ToString(CultureInfo.InvariantCulture);
                        fontSmall.DrawString(ref charOffsetShadow, livesString, view.X + 36 - 4, bottom + 1f,
                            Alignment.BottomLeft, new ColorRgba(0f, 0.32f));
                        fontSmall.DrawString(ref charOffset, livesString, view.X + 36 - 4, bottom,
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

                // Stats / Score
#if MULTIPLAYER
                if (levelHandler is MultiplayerLevelHandler mlh) {
                    switch (mlh.CurrentLevelType) {
                        case MultiplayerLevelType.Battle: {
                            string statsString = "\f[c:4]\f[s:100]+" + mlh.StatsKills.ToString(CultureInfo.InvariantCulture) + " \f[c:2]\f[s:85]/\f[c:0]\f[s:100] -" + mlh.StatsDeaths.ToString(CultureInfo.InvariantCulture);
                            fontSmall.DrawString(ref charOffsetShadow, statsString, 14, 5 + 1, Alignment.TopLeft,
                                new ColorRgba(0f, 0.32f), 1f, charSpacing: 0.88f);
                            fontSmall.DrawString(ref charOffset, statsString, 14, 5, Alignment.TopLeft,
                                ColorRgba.TransparentBlack, 1f, charSpacing: 0.88f);

                            break;
                        }

                        case MultiplayerLevelType.Race: {
                            if (mlh.StatsLapsTotal > 0 && mlh.StatsLaps < mlh.StatsLapsTotal) {
                                string statsString1 = (mlh.StatsLaps + 1).ToString(CultureInfo.InvariantCulture);
                                fontMedium.DrawString(ref charOffsetShadow, statsString1, 32, 10 + 1, Alignment.TopRight,
                                    new ColorRgba(0f, 0.32f), 1f, charSpacing: 1f);
                                fontMedium.DrawString(ref charOffset, statsString1, 32, 10, Alignment.TopRight,
                                    new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f), 1f, charSpacing: 1f);

                                string statsString2 = "/" + mlh.StatsLapsTotal.ToString(CultureInfo.InvariantCulture);
                                fontSmall.DrawString(ref charOffsetShadow, statsString2, 36, 20 + 1, Alignment.TopLeft,
                                    new ColorRgba(0f, 0.32f), 1f, charSpacing: 1.1f);
                                fontSmall.DrawString(ref charOffset, statsString2, 36, 20, Alignment.TopLeft,
                                    new ColorRgba(0.56f, 0.50f, 0.42f, 0.5f), 1f, charSpacing: 1.1f);
                            }
                            break;
                        }

                        case MultiplayerLevelType.TreasureHunt: {
                            DrawMaterial("PickupGem", -1, 14, 20, Alignment.Left,
                                new ColorRgba(0f, 0.4f), 0.6f, 0.6f);
                            DrawMaterial("PickupGem", -1, 14, 20, Alignment.Left,
                                new ColorRgba(1f, 1f), 0.6f, 0.6f);

                            string statsString1 = "\f[c:1]\f[s:80]" + mlh.StatsLaps.ToString(CultureInfo.InvariantCulture);
                            fontMedium.DrawString(ref charOffsetShadow, statsString1, 38, 10 + 1, Alignment.TopLeft,
                                new ColorRgba(0f, 0.32f), 1f, charSpacing: 0.8f);
                            fontMedium.DrawString(ref charOffset, statsString1, 38, 10, Alignment.TopLeft,
                                new ColorRgba(0.7f, 0.45f, 0.42f, 0.5f), 1f, charSpacing: 0.8f);

                            string statsString2 = "\f[c:8]\f[s:110]/" + mlh.StatsLapsTotal.ToString(CultureInfo.InvariantCulture);
                            fontSmall.DrawString(ref charOffsetShadow, statsString2, 60, 18 + 1, Alignment.TopLeft,
                                new ColorRgba(0f, 0.32f), 1f, charSpacing: 1.1f);
                            fontSmall.DrawString(ref charOffset, statsString2, 60, 18, Alignment.TopLeft,
                                new ColorRgba(0.56f, 0.50f, 0.42f, 0.5f), 1f, charSpacing: 1.1f);

                            break;
                        }
                    }
                } else {
#endif
                    DrawMaterial("PickupFood", -1, 3, 3 + 1.6f, Alignment.TopLeft, new ColorRgba(0f, 0.4f));
                    DrawMaterial("PickupFood", -1, 3, 3, Alignment.TopLeft, ColorRgba.White);

                    string scoreString = owner.Score.ToString("D8");
                    fontSmall.DrawString(ref charOffsetShadow, scoreString, 14, 5 + 1, Alignment.TopLeft,
                        new ColorRgba(0f, 0.32f), 1f, charSpacing: 0.88f);
                    fontSmall.DrawString(ref charOffset, scoreString, 14, 5, Alignment.TopLeft,
                        ColorRgba.TransparentBlack, 1f, charSpacing: 0.88f);
#if MULTIPLAYER
                }
#endif

                // Weapon
                if (owner.PlayerType != PlayerType.Frog) {
                    WeaponType weapon = owner.CurrentWeapon;
                    Vector2 pos = new Vector2(right - 40, bottom);
                    string currentWeaponString = GetCurrentWeapon(weapon, ref pos);

                    string ammoCount;
                    if (owner.WeaponAmmo[(int)weapon] < 0) {
                        ammoCount = "x∞";
                    } else {
                        ammoCount = "x" + (owner.WeaponAmmo[(int)weapon] / 100).ToString(CultureInfo.InvariantCulture);
                    }
                    fontSmall.DrawString(ref charOffsetShadow, ammoCount, right - 40, bottom + 1f, Alignment.BottomLeft,
                        new ColorRgba(0f, 0.32f), charSpacing: 0.96f);
                    fontSmall.DrawString(ref charOffset, ammoCount, right - 40, bottom, Alignment.BottomLeft,
                        ColorRgba.TransparentBlack, charSpacing: 0.96f);

                    GraphicResource res;
                    if (graphics.TryGetValue(currentWeaponString, out res)) {
                        if (res.Base.FrameDimensions.Y < 20) {
                            pos.Y -= MathF.Round((20 - res.Base.FrameDimensions.Y) * 0.5f);
                        }

                        DrawMaterial(currentWeaponString, -1, pos.X, pos.Y + 1.6f, Alignment.BottomRight, new ColorRgba(0f, 0.4f));
                        DrawMaterial(currentWeaponString, -1, pos.X, pos.Y, Alignment.BottomRight, ColorRgba.White);
                    }
                }
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

            // Misc
            DrawLevelText(size, ref charOffset);
            DrawCoins(size, ref charOffset);
            DrawGems(size, ref charOffset);
            DrawWeaponWheel(size);

            DrawPlatformSpecific(size);

            if (transitionManager != null) {
                transitionManager.Draw(device);
                if (transitionManager.IsCompleted && transitionManager.CurrentMode != TransitionManager.Mode.FadeOut) {
                    transitionManager = null;
                }
            }

            DrawDebugStrings();

            canvas.End();
        }

        private void DrawLevelText(Vector2 size, ref int charOffset)
        {
            if (levelText == null) {
                return;
            }

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

            BitmapFont font = (levelTextBigger ? fontMedium : fontSmall);

            int charOffsetShadow = charOffset;
            font.DrawString(ref charOffsetShadow, levelText, size.X * 0.5f + offset,
                size.Y * 0.0346f + 2.5f,
                Alignment.Top, new ColorRgba(0f, 0.3f), 1f, 0.72f, 0.8f, 0.8f);

            font.DrawString(ref charOffset, levelText, size.X * 0.5f + offset, size.Y * 0.0346f,
                Alignment.Top, ColorRgba.TransparentBlack, 1f, 0.72f, 0.8f, 0.8f);

            if (levelTextBigger) {
                levelTextTime += 1.2f * Time.TimeMult;
            } else {
                levelTextTime += Time.TimeMult;
            }

            if (levelTextTime > TotalTime) {
                levelText = null;
            }
        }

        private void DrawCoins(Vector2 size, ref int charOffset)
        {
            if (coinsTime < 0f) {
                return;
            }

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

        private void DrawGems(Vector2 size, ref int charOffset)
        {
            if (gemsTime < 0f) {
                return;
            }

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

        private void DrawWeaponWheel(Vector2 size)
        {
            bool isVisible = PrepareWeaponWheel(out int weaponCount);

            const float WeaponWheelAnimMax = 20f;

            if (isVisible) {
                if (weaponWheelAnim < WeaponWheelAnimMax) {
                    weaponWheelAnim += Time.TimeMult;
                    if (weaponWheelAnim > WeaponWheelAnimMax) {
                        weaponWheelAnim = WeaponWheelAnimMax;
                    }
                }
            } else {
                if (weaponWheelAnim > 0f) {
                    weaponWheelAnim -= Time.TimeMult * 2f;
                    if (weaponWheelAnim <= 0f) {
                        weaponWheelAnim = 0f;

                        ControlScheme.EnableFreezeAnalog(owner.PlayerIndex, false);
                        owner.WeaponWheelVisible = false;
                    }
                }
            }

            if (weaponWheelAnim <= 0f) {
                return;
            }

            if (!graphics.TryGetValue("WeaponWheel", out GraphicResource weaponWheelRes)) {
                return;
            }

            ControlScheme.EnableFreezeAnalog(owner.PlayerIndex, true);
            owner.WeaponWheelVisible = true;

            var center = size * 0.5f;
            var angleStep = MathF.TwoPi / weaponCount;

            ControlScheme.GetWeaponWheel(owner.PlayerIndex, out float h, out float v);

            float requestedAngle;
            int requestedIndex;
            if (h == 0 && v == 0) {
                requestedAngle = float.NaN;
                requestedIndex = -1;
            } else {
                requestedAngle = MathF.Atan2(v, h);
                if (requestedAngle < 0) {
                    requestedAngle += MathF.TwoPi;
                }

                float adjustedAngle = requestedAngle + MathF.PiOver2 + angleStep * 0.5f;
                if (adjustedAngle >= MathF.TwoPi) {
                    adjustedAngle -= MathF.TwoPi;
                }

                requestedIndex = (int)(weaponCount * adjustedAngle / MathF.TwoPi);
            }

            float alpha = weaponWheelAnim / WeaponWheelAnimMax;
            float easing = Ease.OutCubic(alpha);
            float distance = 20 + (70 * easing);
            float distance2 = 10 + (50 * easing);

            if (!float.IsNaN(requestedAngle)) {
                float distance3 = distance * 0.86f;
                float distance4 = distance2 * 0.93f;

                canvas.State.TextureCoordinateRect = new Rect(0, 0, 1, 1);
                canvas.State.SetMaterial(weaponWheelRes.Material);

                var cx = (float)Math.Cos(requestedAngle);
                var cy = (float)Math.Sin(requestedAngle);
                canvas.State.ColorTint = new ColorRgba(1f, 0.6f, 0.3f, alpha * 0.4f);
                canvas.FillThickLine(center.X + cx * distance4, center.Y + cy * distance4, center.X + cx * distance3, center.Y + cy * distance3, 3f);
                canvas.State.ColorTint = new ColorRgba(1f, 0.6f, 0.3f, alpha);
                canvas.FillThickLine(center.X + cx * distance4, center.Y + cy * distance4, center.X + cx * distance3, center.Y + cy * distance3, 1f);
            }

            var ammo = owner.WeaponAmmo;
            var angle = -MathF.PiOver2;
            for (int i = 0, j = 0; i < ammo.Length; i++) {
                if (ammo[i] != 0) {
                    float x = MathF.Cos(angle) * distance;
                    float y = MathF.Sin(angle) * distance;
                    
                    var pos = new Vector2(center.X + x, center.Y + y);
                    var weapon = GetCurrentWeapon((WeaponType)i, ref pos);
                    var isSelected = (j == requestedIndex);
                    if (isSelected) {
                        lastWeaponWheelIndex = i;
                    }

                    DrawMaterial("WeaponWheelDim", -1, pos.X, pos.Y, Alignment.Center, ColorRgba.Black.WithAlpha(alpha * 0.6f), 5f, 5f);
                    DrawMaterial(weapon, -1, pos.X, pos.Y, Alignment.Center, ColorRgba.White.WithAlpha(isSelected ? alpha : alpha * 0.7f));

                    canvas.State.TextureCoordinateRect = new Rect(0, 0, 1, 1);
                    canvas.State.SetMaterial(weaponWheelRes.Material);

                    var angle2 = MathF.TwoPi - angle;
                    float angleFrom = angle2 - angleStep * 0.4f;
                    float angleTo = angle2 + angleStep * 0.4f;

                    canvas.State.ColorTint = new ColorRgba(0f, 0f, 0f, alpha * 0.3f);
                    DrawWeaponWheelSegment(center.X - distance2 - 1, center.Y - distance2 - 1, distance2 * 2, distance2 * 2, angleFrom, angleTo);
                    DrawWeaponWheelSegment(center.X - distance2 - 1, center.Y - distance2 + 1, distance2 * 2, distance2 * 2, angleFrom, angleTo);
                    DrawWeaponWheelSegment(center.X - distance2 + 1, center.Y - distance2 - 1, distance2 * 2, distance2 * 2, angleFrom, angleTo);
                    DrawWeaponWheelSegment(center.X - distance2 + 1, center.Y - distance2 + 1, distance2 * 2, distance2 * 2, angleFrom, angleTo);

                    canvas.State.ColorTint = (isSelected ? new ColorRgba(1f, 0.8f, 0.5f, alpha) : new ColorRgba(1f, 1f, 1f, alpha * 0.7f));
                    DrawWeaponWheelSegment(center.X - distance2, center.Y - distance2, distance2 * 2, distance2 * 2, angleFrom, angleTo);

                    angle += angleStep;
                    j++;
                }
            }
        }

        private bool PrepareWeaponWheel(out int weaponCount)
        {
            weaponCount = 0;

            if (owner == null || !SettingsCache.EnableWeaponWheel || !owner.IsControllable || owner.PlayerType == PlayerType.Frog) {
                if (weaponWheelAnim > 0f) {
                    lastWeaponWheelIndex = -1;
                }
                return false;
            }

            if (!ControlScheme.PlayerActionPressed(owner.PlayerIndex, PlayerActions.SwitchWeapon, true, out bool isGamepad) || !isGamepad) {
                if (weaponWheelAnim > 0f) {
                    if (lastWeaponWheelIndex != -1) {
                        owner.SwitchToWeaponByIndex(lastWeaponWheelIndex);
                        lastWeaponWheelIndex = -1;
                    }

                    weaponCount  = GetWeaponCount(owner);
                }
                return false;
            }

            weaponCount = GetWeaponCount(owner);
            return (weaponCount > 0);
        }

        private static int GetWeaponCount(Player owner)
        {
            int weaponCount = 0;

            var ammo = owner.WeaponAmmo;
            for (int i = 0; i < ammo.Length; i++) {
                if (ammo[i] != 0) {
                    weaponCount++;
                }
            }

            // Player must have at least 2 weapons
            if (weaponCount < 2) {
                weaponCount = 0;
            }

            return weaponCount;
        }

        private void DrawWeaponWheelSegment(float x, float y, float width, float height, float minAngle, float maxAngle)
        {
            var device = canvas.DrawDevice;

            width *= 0.5f; x += width;
            height *= 0.5f; y += height;

            var pos = new Vector2(x, y);

            float angleRange = MathF.Min(maxAngle - minAngle, MathF.RadAngle360);

            float projectedSizeAtPos = device.GetScaleAtZ(0f);
            int segmentNum = MathF.Clamp(MathF.RoundToInt(MathF.Pow(MathF.Max(width, height) * projectedSizeAtPos, 0.65f) * 3.5f * angleRange / MathF.RadAngle360), 4, 128);
            float angleStep = angleRange / (segmentNum - 1);
            Vector2 shapeHandle = pos - new Vector2(width, height);
            ColorRgba shapeColor = canvas.State.ColorTint;
            Rect texCoordRect = canvas.State.TextureCoordinateRect;
            int vertexCount = segmentNum + 2;
            VertexC1P3T2[] vertices = canvas.RentVertices(vertexCount);
            float angle = minAngle;

            const float mult = 2.2f;

            {
                int j = 0;
                vertices[j].Pos.X = pos.X + (float)Math.Cos(angle) * (width * mult - 0.5f);
                vertices[j].Pos.Y = pos.Y - (float)Math.Sin(angle) * (height * mult - 0.5f);
                vertices[j].Pos.Z = 0f;
                vertices[j].TexCoord.X = texCoordRect.X;
                vertices[j].TexCoord.Y = texCoordRect.Y;
                vertices[j].Color = shapeColor;
            }

            // XY circle
            for (int i = 1; i < vertexCount - 1; i++) {
                vertices[i].Pos.X = pos.X + (float)Math.Cos(angle) * (width - 0.5f);
                vertices[i].Pos.Y = pos.Y - (float)Math.Sin(angle) * (height - 0.5f);
                vertices[i].Pos.Z = 0f;
                //vertices[i].TexCoord.X = texCoordRect.X + texCoordRect.W * (float)i / (vertexCount - 1);
                vertices[i].TexCoord.X = texCoordRect.X + 0.15f + (texCoordRect.W * 0.7f * (float)(i - 1) / (vertexCount - 3));
                vertices[i].TexCoord.Y = texCoordRect.Y;
                vertices[i].Color = shapeColor;
                angle += angleStep;
            }

            {
                angle -= angleStep;

                int j = vertexCount - 1;
                vertices[j].Pos.X = pos.X + (float)Math.Cos(angle) * (width * mult - 0.5f);
                vertices[j].Pos.Y = pos.Y - (float)Math.Sin(angle) * (height * mult - 0.5f);
                vertices[j].Pos.Z = 0f;
                vertices[j].TexCoord.X = texCoordRect.X + texCoordRect.W;
                vertices[j].TexCoord.Y = texCoordRect.Y;
                vertices[j].Color = shapeColor;
            }

            canvas.State.TransformVertices(vertices, shapeHandle);
            device.AddVertices(canvas.State.MaterialDirect, VertexMode.LineStrip, vertices, 0, vertexCount);
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
            const int DebugX = 4, DebugY = 4;

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
                    DebugX, DebugY, Alignment.TopLeft, ColorRgba.TransparentBlack,
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
            if (graphics.TryGetValue(name, out GraphicResource res)) {
                res.Draw(canvas, frame, x, y, alignment, color, scaleX, scaleY);
            }
        }

        public void DrawMaterialClipped(string name, int frame, float x, float y, Alignment alignment, ColorRgba color, float clipX, float clipY)
        {
            if (graphics.TryGetValue(name, out GraphicResource res)) {
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

        private string GetCurrentWeapon(WeaponType weapon, ref Vector2 offset)
        {
            if (weapon == WeaponType.Toaster && owner.InWater) {
                offset.X += 2;
                offset.Y += 2;
                return "WeaponToasterDisabled";
            } else if (weapon == WeaponType.Seeker) {
                offset.X += 2;
            } else if (weapon == WeaponType.TNT) {
                offset.X += 2;
            } else if (weapon == WeaponType.Electro) {
                offset.X += 6;
            }

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

        public void ShowLevelText(string text, bool bigger)
        {
            if (levelText == text) {
                return;
            }

            levelText = text;
            levelTextTime = 0f;
            levelTextBigger = bigger;
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
            transitionManager = new TransitionManager(TransitionManager.Mode.FadeIn, smooth);
        }

        public void BeginFadeOut(bool smooth)
        {
            transitionManager = new TransitionManager(TransitionManager.Mode.FadeOut, smooth);
        }
    }
}