using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Duality.IO;
using Duality.Resources;
using Jazz2.Storage.Content;
using static Jazz2.Game.LevelHandler;
using MathF = Duality.MathF;

namespace Jazz2.Game.UI.Menu
{
    public class CustomLevelSelectSection : MenuSection
    {
        private class CustomLevel
        {
            public string DisplayName;
            public string EpisodeName;
            public string LevelName;
            public string Icon;

            public CustomLevel(string displayName, string episodeName, string levelName, string icon)
            {
                DisplayName = displayName;
                EpisodeName = episodeName;
                LevelName = levelName;
                Icon = icon;
            }
        }

        private int maxVisibleItems = 15;

        private List<CustomLevel> levelList;

        private int selectedIndex;
        private int scrollOffset;
        private float animation;
        private int pressedCount;

        private bool isLoading;
        private float isLoadingAnimation;

        public CustomLevelSelectSection()
        {
            levelList = new List<CustomLevel>();

            isLoading = true;
            isLoadingAnimation = 1f;

            ThreadPool.UnsafeQueueUserWorkItem(_ => {
                JsonParser json = new JsonParser();

                try {
                    string path = PathOp.Combine(DualityApp.DataDirectory, "Episodes", "unknown");
                    if (DirectoryOp.Exists(path)) {
                        foreach (string levelPath in DirectoryOp.GetFiles(path)) {
                            if (api == null) {
                                break;
                            }

                            if (!levelPath.EndsWith(".level")) {
                                continue;
                            }

                            IFileSystem levelPackage = new CompressedContent(levelPath);

                            using (
                                Stream s = levelPackage.OpenFile(".res", FileAccessMode.Read)) {
                                string levelToken = PathOp.GetFileNameWithoutExtension(levelPath);

                                LevelConfigJson config = json.Parse<LevelConfigJson>(s);

                                string icon = "";
#if DEBUG
                                if ((config.Description.Flags & LevelFlags.FastCamera) != 0) {
                                    icon += " Fast";
                                }
                                if ((config.Description.Flags & LevelFlags.HasPit) != 0) {
                                    icon += " Pit";
                                }
#endif
                                if ((config.Description.Flags & LevelFlags.Multiplayer) != 0 && (config.Description.Flags & (LevelFlags.MultiplayerRace | LevelFlags.MultiplayerFlags)) == 0) {
                                    icon += " Battle";
                                }
                                if ((config.Description.Flags & LevelFlags.MultiplayerRace) != 0) {
                                    icon += " Race";
                                }
                                if ((config.Description.Flags & LevelFlags.MultiplayerFlags) != 0) {
                                    icon += " CTF";
                                }

                                levelList.Add(new CustomLevel(config.Description.Name, "unknown", levelToken, icon));
                            }
                        }

                        levelList.Sort((x, y) => {
                            string xs = BitmapFont.StripFormatting(x.DisplayName);
                            string ys = BitmapFont.StripFormatting(y.DisplayName);
                            int i = string.Compare(xs, ys, StringComparison.InvariantCulture);
                            if (i != 0) {
                                return i;
                            }

                            return string.Compare(x.LevelName, y.LevelName, StringComparison.InvariantCulture);
                        });
                    }
                } catch {
                    // ToDo: Handle exceptions
                } finally {
                    isLoading = false;
                }
            }, null);
        }

        public override void OnShow(IMenuContainer root)
        {
            base.OnShow(root);
            animation = 0f;
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

            const float topLine = 96f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial("MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, ColorRgba.White, 55f, (bottomLine - topLine) * 0.063f, new Rect(0f, 0.3f, 1f, 0.4f));

            int charOffset = 0;
            if (levelList.Count > 0) {
                const float itemSpacing = 17f;

                float topItem = topLine - 4f;
                float bottomItem = bottomLine - 10f;
                float contentHeight = bottomItem - topItem;

                float maxVisibleItemsFloat = (contentHeight / itemSpacing);
                maxVisibleItems = (int)maxVisibleItemsFloat;

                float currentItem = topItem + itemSpacing + (maxVisibleItemsFloat - maxVisibleItems) * 0.5f * itemSpacing;


                // ToDo: ...
                float column2 = device.TargetSize.X * 0.55f;
                float sx = column2 * 1.52f;
                float column1 = column2 * 0.36f + view.X;
                column2 *= 1.1f;

                for (int i = 0; i < maxVisibleItems; i++) {
                    int idx = i + scrollOffset;
                    if (idx >= levelList.Count) {
                        break;
                    }

                    if (selectedIndex == idx) {
                        charOffset = 0;

                        float xMultiplier = levelList[idx].DisplayName.Length * 0.5f;
                        float easing = Ease.OutElastic(animation);
                        float x = column1 + xMultiplier - easing * xMultiplier;
                        float size = 0.7f + easing * 0.12f;

                        // Column 2
                        api.DrawStringShadow(ref charOffset, levelList[idx].LevelName, column2, currentItem, Alignment.Left,
                            new ColorRgba(0.48f, 0.5f), 0.8f, 0.4f, 1f, 1f, 8f, charSpacing: 0.88f);

                        // Column 1
                        api.DrawStringShadow(ref charOffset, levelList[idx].DisplayName, x, currentItem, Alignment.Left,
                            null, size, 0.4f, 1f, 1f, 8f, charSpacing: 0.88f);

                        // Column 0
                        api.DrawStringShadow(ref charOffset, levelList[idx].Icon, column1 - 16f, currentItem, Alignment.Right,
                            new ColorRgba(0.48f, 0.5f), size, 0.4f, 1f, 1f, 8f, charSpacing: 0.68f);
                    } else {
                        // Column 2
                        api.DrawString(ref charOffset, levelList[idx].LevelName, column2, currentItem, Alignment.Left,
                            ColorRgba.TransparentBlack, 0.7f);

                        // Column 1
                        api.DrawString(ref charOffset, levelList[idx].DisplayName, column1, currentItem, Alignment.Left,
                            ColorRgba.TransparentBlack, 0.7f);

                        // Column 0
                        api.DrawString(ref charOffset, levelList[idx].Icon, column1 - 16f, currentItem, Alignment.Right,
                            ColorRgba.TransparentBlack, 0.7f, charSpacing: 0.7f);
                    }

                    currentItem += itemSpacing;
                }

                // Scrollbar
                if (levelList.Count > maxVisibleItems) {
                    const float sw = 3f;
                    float sy = ((float)scrollOffset / levelList.Count) * 18f * maxVisibleItems + topLine;
                    float sh = ((float)maxVisibleItems / levelList.Count) * 16f * maxVisibleItems;

                    BatchInfo mat1 = device.RentMaterial();
                    mat1.Technique = DrawTechnique.Alpha;
                    mat1.MainColor = new ColorRgba(0f, 0f, 0f, 0.28f);
                    canvas.State.SetMaterial(mat1);
                    canvas.FillRect(sx + 1f, sy + 1f, sw, sh);

                    BatchInfo mat2 = device.RentMaterial();
                    mat2.Technique = DrawTechnique.Alpha;
                    mat2.MainColor = new ColorRgba(0.8f, 0.8f, 0.8f, 0.5f);
                    canvas.State.SetMaterial(mat2);
                    canvas.FillRect(sx, sy, sw, sh);
                }

                // Loading
                if (isLoadingAnimation > 0f) {
                    if (!isLoading) {
                        isLoadingAnimation -= Time.TimeMult * 0.03f;
                    } else {
                        isLoadingAnimation = 1f;
                    }

                    float loadingX = center.X - 50f;
                    float loadingY = center.Y;

                    float startAngle = (float)(Time.GameTimer.TotalSeconds * 6.0f);

                    float time = (float)(Time.GameTimer.TotalSeconds * 1.3f) % 2f;
                    bool reverse = (time >= 1f);
                    if (reverse) {
                        time -= 1f;
                    }
                    float timeCubed = MathF.Pow(time, 3);
                    float timeQuad = MathF.Pow(time, 4);
                    float timeQuint = MathF.Pow(time, 5);

                    float endAngle;
                    if (reverse) {
                        endAngle = startAngle + MathF.TwoPi * (1 - ((6 * timeQuint) + (-15 * timeQuad) + (10 * timeCubed)));
                    } else {
                        endAngle = startAngle + MathF.TwoPi * ((6 * timeQuint) + (-15 * timeQuad) + (10 * timeCubed));
                    }

                    const float r1 = 7f;
                    const float r2 = r1 - 0.4f;
                    const float r3 = r2 - 0.4f;
                    const float r4 = r3 - 0.4f;

                    api.DrawMaterial("MenuDim", loadingX + 50f, loadingY, Alignment.Center, new ColorRgba(1f, 0.7f * isLoadingAnimation), 40f, 10f);

                    BatchInfo mat1 = device.RentMaterial();
                    mat1.Technique = DrawTechnique.Alpha;
                    mat1.MainColor = new ColorRgba(0f, 0.2f * isLoadingAnimation);
                    canvas.State.SetMaterial(mat1);
                    canvas.DrawCircleSegment(loadingX + 1.6f, loadingY + 1.6f, r1, startAngle, endAngle);
                    canvas.DrawCircleSegment(loadingX + 1.6f, loadingY + 1.6f, r2, startAngle, endAngle);
                    canvas.DrawCircleSegment(loadingX + 1.6f, loadingY + 1.6f, r3, startAngle, endAngle);
                    canvas.DrawCircleSegment(loadingX + 1.6f, loadingY + 1.6f, r4, startAngle, endAngle);

                    BatchInfo mat2 = device.RentMaterial();
                    mat2.Technique = DrawTechnique.Alpha;
                    mat2.MainColor = new ColorRgba(0.95f, 0.8f * isLoadingAnimation);
                    canvas.State.SetMaterial(mat2);
                    canvas.DrawCircleSegment(loadingX, loadingY, r1, startAngle, endAngle);
                    canvas.DrawCircleSegment(loadingX, loadingY, r2, startAngle, endAngle);
                    canvas.DrawCircleSegment(loadingX, loadingY, r3, startAngle, endAngle);
                    canvas.DrawCircleSegment(loadingX, loadingY, r4, startAngle, endAngle);

                    api.DrawStringShadow(ref charOffset, "loading".T(), loadingX + r1 + 10f, loadingY + 2f, Alignment.Left,
                        new ColorRgba(0.48f, 0.5f * isLoadingAnimation), 0.8f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);
                }
            } else {
                api.DrawStringShadow(ref charOffset, "menu/play custom/single/empty".T(), center.X, center.Y, Alignment.Center,
                    new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f), 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);
            }

            api.DrawMaterial("MenuLine", 0, center.X, topLine, Alignment.Center, ColorRgba.White, 1.6f);
            api.DrawMaterial("MenuLine", 1, center.X, bottomLine, Alignment.Center, ColorRgba.White, 1.6f);
        }

        public override void OnUpdate()
        {
            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                if (levelList.Count > 0) {
                    api.PlaySound("MenuSelect", 0.5f);
                    api.SwitchToSection(new StartGameOptionsSection(levelList[selectedIndex].EpisodeName, levelList[selectedIndex].LevelName, null));
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }

            if (levelList.Count > 1) {
                if (ControlScheme.MenuActionPressed(PlayerActions.Up)) {
                    if (animation >= 1f - (pressedCount * 0.05f) || ControlScheme.MenuActionHit(PlayerActions.Up)) {
                        api.PlaySound("MenuSelect", 0.4f);
                        animation = 0f;
                        if (selectedIndex > 0) {
                            selectedIndex--;
                            if (selectedIndex < scrollOffset) {
                                scrollOffset = selectedIndex;
                            }
                        } else {
                            selectedIndex = levelList.Count - 1;
                            scrollOffset = Math.Max(0, selectedIndex - (maxVisibleItems - 1));
                        }
                        pressedCount = Math.Min(pressedCount + 4, 19);
                    }
                } else if (ControlScheme.MenuActionPressed(PlayerActions.Down)) {
                    if (animation >= 1f - (pressedCount * 0.05f) || ControlScheme.MenuActionHit(PlayerActions.Down)) {
                        api.PlaySound("MenuSelect", 0.4f);
                        animation = 0f;
                        if (selectedIndex < levelList.Count - 1) {
                            selectedIndex++;
                            if (selectedIndex >= scrollOffset + maxVisibleItems) {
                                scrollOffset = selectedIndex - (maxVisibleItems - 1);
                            }
                        } else {
                            selectedIndex = 0;
                            scrollOffset = 0;
                        }
                        pressedCount = Math.Min(pressedCount + 4, 19);
                    }
                } else {
                    pressedCount = 0;
                }

                if (DualityApp.Keyboard.KeyHit(Key.PageUp)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;

                    selectedIndex = MathF.Max(0, selectedIndex - maxVisibleItems);
                    if (selectedIndex < scrollOffset) {
                        scrollOffset = selectedIndex;
                    }
                } else if (DualityApp.Keyboard.KeyHit(Key.PageDown)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;

                    selectedIndex = MathF.Min(levelList.Count - 1, selectedIndex + maxVisibleItems);
                    if (selectedIndex >= scrollOffset + maxVisibleItems) {
                        scrollOffset = selectedIndex - (maxVisibleItems - 1);
                    }
                }
            }
        }
    }
}