#if MULTIPLAYER

using System;
using System.Collections.Generic;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Duality.Resources;
using Jazz2.Game.Multiplayer;

namespace Jazz2.Game.UI.Menu
{
    public class MultiplayerServerSelectSection : MenuSection
    {
        private ServerDiscovery discovery;

        private int maxVisibleItems = 15;

        private List<ServerDiscovery.Server> serverList;

        private int selectedIndex;
        private int scrollOffset;
        private float animation;
        private int pressedCount;

        public MultiplayerServerSelectSection()
        {
            serverList = new List<ServerDiscovery.Server>();
        }

        public override void OnShow(IMenuContainer root)
        {
            base.OnShow(root);
            animation = 0f;

            discovery = new ServerDiscovery("J²", 10666, OnServerFound);
        }

        public override void OnHide(bool isRemoved)
        {
            base.OnHide(isRemoved);

            if (discovery != null) {
                discovery.Dispose();
                discovery = null;
            }
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

            const float topLine = 96f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial("MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, ColorRgba.White, 55f, (bottomLine - topLine) * 0.063f, new Rect(0f, 0.3f, 1f, 0.4f));

            int charOffset = 0;
            int itemCount = 0;

            if (serverList.Count > 0) {
                const float itemSpacing = 17f;

                float topItem = topLine - 4f;
                float bottomItem = bottomLine - 10f;
                float contentHeight = bottomItem - topItem;

                float maxVisibleItemsFloat = (contentHeight / itemSpacing);
                maxVisibleItems = (int)maxVisibleItemsFloat;

                float currentItem = topItem + itemSpacing + (maxVisibleItemsFloat - maxVisibleItems) * 0.5f * itemSpacing;

                float column2 = device.TargetSize.X * 0.55f;
                float sx = column2 * 1.52f;
                float column1 = column2;
                float column3 = column2;
                column1 *= 0.3f;
                column2 *= 0.78f;
                column3 *= 1.08f;

                for (int i = 0; i < maxVisibleItems; i++) {
                    int idx = i + scrollOffset;
                    if (idx >= serverList.Count) {
                        break;
                    }

                    ServerDiscovery.Server server = serverList[idx];
                    if (server.IsLost && server.LatencyMs < 0) {
                        continue;
                    }
                   
                    string infoText = server.CurrentPlayers + " / " + server.MaxPlayers + "   ";
                    ColorRgba infoColor;
                    if (server.LatencyMs < 0) {
                        infoText += "- ms";
                        infoColor = new ColorRgba(0.48f, 0.5f);
                    } else if (server.LatencyMs > 10000) {
                        infoText = "menu/play custom/multi/unreachable".T();
                        infoColor = new ColorRgba(0.45f, 0.27f, 0.22f, 0.5f);
                    } else {
                        infoText += server.LatencyMs + " ms";

                        float playersRatio = (float)(server.CurrentPlayers / server.MaxPlayers);
                        if (server.LatencyMs < 50 && playersRatio < 0.9f) {
                            infoColor = new ColorRgba(0.2f, 0.45f, 0.2f, 0.5f);
                        } else if (server.LatencyMs < 100 && playersRatio < 0.9f) {
                            infoColor = new ColorRgba(0.45f, 0.45f, 0.21f, 0.5f);
                        } else if (server.LatencyMs < 200 && playersRatio < 0.95f) {
                            infoColor = new ColorRgba(0.5f, 0.4f, 0.2f, 0.5f);
                        } else if (server.LatencyMs < 400 && playersRatio < 0.99f) {
                            infoColor = new ColorRgba(0.47f, 0.35f, 0.3f, 0.5f);
                        } else {
                            infoColor = new ColorRgba(0.45f, 0.27f, 0.22f, 0.5f);
                        }
                    }

                    string name = server.Name;
                    if (name.Length > 32) {
                        name = name.Substring(0, 31) + "...";
                    }

                    if (selectedIndex == idx) {
                        charOffset = 0;

                        float xMultiplier = name.Length * 0.5f;
                        float easing = Ease.OutElastic(animation);
                        float x = column1 + xMultiplier - easing * xMultiplier;
                        float size = 0.7f + easing * 0.1f;

                        // Column 2
                        api.DrawStringShadow(ref charOffset, infoText, column2, currentItem, Alignment.Left,
                            infoColor, 0.8f, 0.4f, 1f, 1f, 8f, charSpacing: 0.88f);

                        // Column 3
                        api.DrawStringShadow(ref charOffset, server.EndPointName, column3, currentItem, Alignment.Left,
                            new ColorRgba(0.48f, 0.5f), 0.8f, 0.4f, 1f, 1f, 8f, charSpacing: 0.88f);

                        // Column 1
                        api.DrawStringShadow(ref charOffset, name, x, currentItem, Alignment.Left,
                            null, size, 0.4f, 1f, 1f, 8f, charSpacing: 0.88f);
                        
                    } else {
                        // Column 2
                        api.DrawString(ref charOffset, infoText, column2, currentItem, Alignment.Left,
                            infoColor, 0.7f);

                        // Column 3
                        api.DrawString(ref charOffset, server.EndPointName, column3, currentItem, Alignment.Left,
                            ColorRgba.TransparentBlack, 0.7f);

                        // Column 1
                        api.DrawString(ref charOffset, name, column1, currentItem, Alignment.Left,
                            ColorRgba.TransparentBlack, 0.7f);
                    }

                    currentItem += itemSpacing;
                    itemCount++;
                }

                // Scrollbar
                if (itemCount > maxVisibleItems) {
                    const float sw = 3f;
                    float sy = ((float)scrollOffset / itemCount) * 18f * maxVisibleItems + topLine;
                    float sh = ((float)maxVisibleItems / itemCount) * 16f * maxVisibleItems;

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
            }

            if (itemCount == 0) {
                api.DrawStringShadow(ref charOffset, "menu/play custom/multi/empty".T(), center.X, center.Y, Alignment.Center,
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
                if (selectedIndex < serverList.Count && serverList[selectedIndex].LatencyMs <= 10000) {
                    ControlScheme.IsSuspended = true;

                    api.PlaySound("MenuSelect", 0.5f);
                    api.BeginFadeOut(() => {
                        ControlScheme.IsSuspended = false;

                        api.SwitchToServer(serverList[selectedIndex].EndPoint);

                        if (discovery != null) {
                            discovery.Dispose();
                            discovery = null;
                        }
                    });
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }

            if (serverList.Count > 1) {
                if (ControlScheme.MenuActionPressed(PlayerActions.Up)) {
                    if (animation >= 1f - (pressedCount * 0.05f) || DualityApp.Keyboard.KeyHit(Key.Up)) {
                        api.PlaySound("MenuSelect", 0.4f);
                        animation = 0f;
                        if (selectedIndex > 0) {
                            selectedIndex--;
                            while (selectedIndex < scrollOffset) {
                                scrollOffset--;
                            }
                        } else {
                            selectedIndex = serverList.Count - 1;
                            scrollOffset = selectedIndex - (maxVisibleItems - 1);
                            if (scrollOffset < 0) {
                                scrollOffset = 0;
                            }
                        }
                        pressedCount = Math.Min(pressedCount + 4, 19);
                    }
                } else if (ControlScheme.MenuActionPressed(PlayerActions.Down)) {
                    if (animation >= 1f - (pressedCount * 0.05f) || DualityApp.Keyboard.KeyHit(Key.Down)) {
                        api.PlaySound("MenuSelect", 0.4f);
                        animation = 0f;
                        if (selectedIndex < serverList.Count - 1) {
                            selectedIndex++;
                            while (selectedIndex >= scrollOffset + maxVisibleItems) {
                                scrollOffset++;
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
            }
        }

        private void OnServerFound(ServerDiscovery.Server server, bool isNew)
        {
            if (!isNew) {
                return;
            }

            serverList.Add(server);
        }
    }
}

#endif