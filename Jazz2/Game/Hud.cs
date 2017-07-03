using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Jazz2.Actors;
using Jazz2.Structs;

namespace Jazz2.Game
{
    public class Hud : Component, ICmpRenderer
    {
        private CanvasBuffer canvasBuffer;
        private BitmapFont fontSmall;

        private Dictionary<string, GraphicResource> graphics;

        private PlayerType playerType;
        private int currentHealth;
        private string currentWeapon;
        private string levelText;
        private float levelTextTime;

#if DEBUG
        private static StringBuilder debugString;
        private bool enableDebug;
#endif

        public Hud()
        {
            canvasBuffer = new CanvasBuffer();

            fontSmall = new BitmapFont("ui/font_small", 17, 18, 15, 32, 256, -2, canvasBuffer);

            Metadata m = ContentManager.Current.RequestMetadata("UI/HUD");
            graphics = m.Graphics;

#if DEBUG
            debugString = new StringBuilder();
#endif
        }

        float ICmpRenderer.BoundRadius => float.MaxValue;

        void ICmpRenderer.Draw(IDrawDevice device)
        {
            Canvas c = new Canvas(device, canvasBuffer);

            Vector2 size = device.TargetSize;
            int charOffset = 0;
            int charOffsetShadow = 0;

            DrawDebugStrings(device);

            // Health & Lives
            string currentPlayer;
            if (playerType == PlayerType.Spaz) {
                currentPlayer = "UI_CHARACTER_ICON_SPAZ";
            } else if (playerType == PlayerType.Lori) {
                currentPlayer = "UI_CHARACTER_ICON_LORI";
            } else {
                currentPlayer = "UI_CHARACTER_ICON_JAZZ";
            }

            DrawMaterial(c, currentPlayer, 36, size.Y, Alignment.BottomRight);

            string healthString = new string('|', currentHealth);

            fontSmall.DrawString(device, ref charOffsetShadow, healthString, 36 - 3 - 0.5f, size.Y - 16 + 0.5f, Alignment.BottomLeft, new ColorRgba(0f, 0f, 0f, 0.42f), 0.7f, charSpacing: 1.1f);
            fontSmall.DrawString(device, ref charOffsetShadow, healthString, 36 - 3 + 0.5f, size.Y - 16 - 0.5f, Alignment.BottomLeft, new ColorRgba(0f, 0f, 0f, 0.42f), 0.7f, charSpacing: 1.1f);
            fontSmall.DrawString(device, ref charOffset, healthString, 36 - 3, size.Y - 16, Alignment.BottomLeft, null, 0.7f, charSpacing: 1.1f);

            fontSmall.DrawString(device, ref charOffset, "x1", 36 - 4, size.Y, Alignment.BottomLeft, ColorRgba.TransparentBlack);

            // Weapon
            DrawMaterial(c, currentWeapon, size.X - 40, size.Y, Alignment.BottomRight);
            fontSmall.DrawString(device, ref charOffset, "x\x7f", size.X - 40, size.Y, Alignment.BottomLeft, ColorRgba.TransparentBlack);

            // Level Text
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

                charOffsetShadow = charOffset;
                fontSmall.DrawString(device, ref charOffsetShadow, levelText, size.X * 0.5f + offset, 14f + 2.5f,
                    Alignment.Top, new ColorRgba(0f, 0f, 0f, 0.3f), 1f, 0.72f, 0.8f, 0.8f);

                fontSmall.DrawString(device, ref charOffset, levelText, size.X * 0.5f + offset, 14f,
                    Alignment.Top, ColorRgba.TransparentBlack, 1f, 0.72f, 0.8f, 0.8f);

                levelTextTime += Time.TimeMult;
                if (levelTextTime > TotalTime) {
                    levelText = null;
                }
            }
        }

        bool ICmpRenderer.IsVisible(IDrawDevice device)
        {
            return (device.VisibilityMask & VisibilityFlag.ScreenOverlay) != 0;
        }

        [Conditional("DEBUG")]
        public static void ShowDebug(string text)
        {
#if DEBUG
            debugString.AppendLine(text);
#endif
        }

        [Conditional("DEBUG")]
        private void DrawDebugStrings(IDrawDevice device)
        {
#if DEBUG
            const int x = 4, y = 4;

            if (enableDebug) {
                int charOffset = 0;
                fontSmall.DrawString(device, ref charOffset, debugString.ToString(),
                    x, y, Alignment.TopLeft, ColorRgba.TransparentBlack,
                    0.65f, charSpacing: 0.9f, lineSpacing: 0.9f);
            }

            debugString.Clear();

            if (DualityApp.Keyboard.KeyHit(Key.D)) {
                enableDebug ^= true;
            }
#endif
        }

        private void DrawMaterial(Canvas c, string id, float x, float y, Alignment alignment, float scale = 1f)
        {
            GraphicResource res;
            if (!graphics.TryGetValue(id, out res)) {
                return;
            }

            int curAnimFrame = (int)(Time.GameTimer.TotalSeconds * res.FrameCount / res.FrameDuration) % res.FrameCount;

            Vector2 originPos = new Vector2(x, y);
            alignment.ApplyTo(ref originPos, new Vector2(res.FrameDimensions.X * scale, res.FrameDimensions.Y * scale));

            c.State.SetMaterial(res.Material);
            /*c.State.TextureCoordinateRect = new Rect(
                (float)(curAnimFrame % res.FrameConfiguration.X) / res.FrameConfiguration.X,
                (float)(curAnimFrame / res.FrameConfiguration.X) / res.FrameConfiguration.Y,
                (1f / res.FrameConfiguration.X),
                (1f / res.FrameConfiguration.Y)
            );*/
            c.State.TextureCoordinateRect = res.Material.Res.MainTexture.Res.LookupAtlas(curAnimFrame);

            c.FillRect(originPos.X, originPos.Y, res.FrameDimensions.X * scale, res.FrameDimensions.Y * scale);
        }

        public void ChangePlayerType(PlayerType player)
        {
            playerType = player;
        }

        public void ChangeCurrentWeapon(WeaponType weapon, byte upgrades)
        {
            if ((upgrades & 0x1) != 0) {
                switch (weapon) {
                    case WeaponType.Blaster:
                        if (playerType == PlayerType.Spaz) {
                            currentWeapon = "UI_WEAPON_POWERUP_BLASTER_SPAZ";
                        } else if (playerType == PlayerType.Lori) {
                            currentWeapon = "UI_WEAPON_POWERUP_BLASTER_LORI";
                        } else {
                            currentWeapon = "UI_WEAPON_POWERUP_BLASTER_JAZZ";
                        }
                        break;

                    case WeaponType.Bouncer: currentWeapon = "UI_WEAPON_POWERUP_BOUNCER"; break;
                    case WeaponType.Freezer: currentWeapon = "UI_WEAPON_POWERUP_FREEZER"; break;
                    case WeaponType.Seeker: currentWeapon = "UI_WEAPON_POWERUP_SEEKER"; break;
                    case WeaponType.RF: currentWeapon = "UI_WEAPON_POWERUP_RF"; break;
                    case WeaponType.Toaster: currentWeapon = "UI_WEAPON_POWERUP_TOASTER"; break;
                    case WeaponType.TNT: currentWeapon = "UI_WEAPON_POWERUP_TNT"; break;
                    case WeaponType.Pepper: currentWeapon = "UI_WEAPON_POWERUP_PEPPER"; break;
                    case WeaponType.Electro: currentWeapon = "UI_WEAPON_POWERUP_ELECTRO"; break;
                }
            } else {
                switch (weapon) {
                    case WeaponType.Blaster:
                        if (playerType == PlayerType.Spaz) {
                            currentWeapon = "UI_WEAPON_BLASTER_SPAZ";
                        } else if (playerType == PlayerType.Lori) {
                            currentWeapon = "UI_WEAPON_BLASTER_LORI";
                        } else {
                            currentWeapon = "UI_WEAPON_BLASTER_JAZZ";
                        }
                        break;

                    case WeaponType.Bouncer: currentWeapon = "UI_WEAPON_BOUNCER"; break;
                    case WeaponType.Freezer: currentWeapon = "UI_WEAPON_FREEZER"; break;
                    case WeaponType.Seeker: currentWeapon = "UI_WEAPON_SEEKER"; break;
                    case WeaponType.RF: currentWeapon = "UI_WEAPON_RF"; break;
                    case WeaponType.Toaster: currentWeapon = "UI_WEAPON_TOASTER"; break;
                    case WeaponType.TNT: currentWeapon = "UI_WEAPON_TNT"; break;
                    case WeaponType.Pepper: currentWeapon = "UI_WEAPON_PEPPER"; break;
                    case WeaponType.Electro: currentWeapon = "UI_WEAPON_ELECTRO"; break;
                }
            }
        }

        public void ChangeHealth(int health)
        {
            currentHealth = health;
        }

        public void ShowLevelText(string text)
        {
            if (levelText == text) {
                return;
            }

            levelText = text;
            levelTextTime = 0f;
        }
    }
}