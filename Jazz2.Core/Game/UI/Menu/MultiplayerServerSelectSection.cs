#if MULTIPLAYER

using System;
using System.Collections.Generic;
using System.Net;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Duality.Resources;
using Jazz2.Game.Multiplayer;

namespace Jazz2.Game.UI.Menu
{
    public class MultiplayerServerSelectSection : MainMenuSection
    {
        private class Server
        {
            public IPEndPoint EndPoint;

            public string Name;
            public int CurrentPlayers;
            public int MaxPlayers;
            public int LatencyMs;
        }

        private ServerDiscovery discovery;

        private int itemCount = 15;

        private List<Server> serverList;

        private int selectedIndex;
        private int xOffset;
        private float animation;
        private int pressedCount;

        public MultiplayerServerSelectSection()
        {
            serverList = new List<Server>();
        }

        public override void OnShow(MainMenu root)
        {
            base.OnShow(root);
            animation = 0f;

            discovery = new ServerDiscovery("J²", 10666, OnServerFound);
        }

        public override void OnHide(bool isRemoved)
        {
            base.OnHide(isRemoved);

            discovery.Dispose();
        }

        public override void OnPaint(IDrawDevice device, Canvas c)
        {
            Vector2 center = device.TargetSize * 0.5f;

            const float topLine = 96f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial(c, "MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, ColorRgba.White, 55f, (bottomLine - topLine) * 0.063f, new Rect(0f, 0.3f, 1f, 0.4f));

            int charOffset = 0;
            if (serverList.Count > 0) {
                float topItem = topLine;
                float bottomItem = bottomLine;
                float contentHeight = bottomItem - topItem;
                const float itemSpacing = 17f;
                itemCount = (int)(contentHeight / itemSpacing);

                float currentItem = topItem + itemSpacing;

                // ToDo: ...
                float column2 = device.TargetSize.X * 0.55f;
                float sx = column2 * 1.52f;
                float column1 = column2;
                float column3 = column2;
                column1 *= 0.3f;
                column2 *= 0.78f;
                column3 *= 1.08f;

                for (int i_ = 0; i_ < itemCount; i_++) {
                    int i = xOffset + i_;
                    if (i >= serverList.Count) {
                        break;
                    }

                    Server server = serverList[i];

                    if (selectedIndex == i) {
                        charOffset = 0;

                        float xMultiplier = server.Name.Length * 0.5f;
                        float easing = Ease.OutElastic(animation);
                        float x = column1 + xMultiplier - easing * xMultiplier;
                        float size = 0.7f + easing * 0.1f;

                        // Column 2
                        api.DrawStringShadow(device, ref charOffset, server.CurrentPlayers + " / " + server.MaxPlayers + "  " + server.LatencyMs + " ms", column2, currentItem, Alignment.Left,
                            new ColorRgba(0.48f, 0.5f), 0.8f, 0.4f, 1f, 1f, 8f, charSpacing: 0.8f);

                        // Column 3
                        api.DrawStringShadow(device, ref charOffset, server.EndPoint.ToString(), column3, currentItem, Alignment.Left,
                            new ColorRgba(0.48f, 0.5f), 0.8f, 0.4f, 1f, 1f, 8f, charSpacing: 0.8f);

                        // Column 1
                        api.DrawStringShadow(device, ref charOffset, server.Name, x, currentItem, Alignment.Left,
                            null, size, 0.4f, 1f, 1f, 8f, charSpacing: 0.88f);
                        
                    } else {
                        // Column 2
                        api.DrawString(device, ref charOffset, server.CurrentPlayers + " / " + server.MaxPlayers + "  " + server.LatencyMs + " ms", column2, currentItem, Alignment.Left,
                            ColorRgba.TransparentBlack, 0.7f);

                        // Column 3
                        api.DrawString(device, ref charOffset, server.EndPoint.ToString(), column3, currentItem, Alignment.Left,
                            ColorRgba.TransparentBlack, 0.7f);

                        // Column 1
                        api.DrawString(device, ref charOffset, server.Name, column1, currentItem, Alignment.Left,
                            ColorRgba.TransparentBlack, 0.7f);
                    }

                    currentItem += itemSpacing;
                }

                // Scrollbar
                if (serverList.Count > itemCount) {
                    const float sw = 3f;
                    float sy = ((float)xOffset / serverList.Count) * 18f * itemCount + topLine;
                    float sh = ((float)itemCount / serverList.Count) * 16f * itemCount;

                    c.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, new ColorRgba(0f, 0f, 0f, 0.28f)));
                    c.FillRect(sx + 1f, sy + 1f, sw, sh);

                    c.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, new ColorRgba(0.8f, 0.8f, 0.8f, 0.5f)));
                    c.FillRect(sx, sy, sw, sh);
                }
            } else {
                api.DrawStringShadow(device, ref charOffset, "Servers not found!", center.X, center.Y, Alignment.Center,
                    new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f), 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);
            }

            api.DrawMaterial(c, "MenuLine", 0, center.X, topLine, Alignment.Center, ColorRgba.White, 1.6f);
            api.DrawMaterial(c, "MenuLine", 1, center.X, bottomLine, Alignment.Center, ColorRgba.White, 1.6f);
        }

        public override void OnUpdate()
        {
            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }

            if (DualityApp.Keyboard.KeyHit(Key.Enter)) {
                if (serverList.Count > 0) {
                    api.PlaySound("MenuSelect", 0.5f);
                    api.SwitchToServer(serverList[selectedIndex].EndPoint);
                }
            } else if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }

            if (serverList.Count > 0) {
                if (DualityApp.Keyboard.KeyPressed(Key.Up)) {
                    if (animation >= 1f - (pressedCount * 0.05f) || DualityApp.Keyboard.KeyHit(Key.Up)) {
                        api.PlaySound("MenuSelect", 0.4f);
                        animation = 0f;
                        if (selectedIndex > 0) {
                            selectedIndex--;
                            while (selectedIndex < xOffset) {
                                xOffset--;
                            }
                        } else {
                            selectedIndex = serverList.Count - 1;
                            xOffset = selectedIndex - (itemCount - 1);
                        }
                        pressedCount = Math.Min(pressedCount + 4, 19);
                    }
                } else if (DualityApp.Keyboard.KeyPressed(Key.Down)) {
                    if (animation >= 1f - (pressedCount * 0.05f) || DualityApp.Keyboard.KeyHit(Key.Down)) {
                        api.PlaySound("MenuSelect", 0.4f);
                        animation = 0f;
                        if (selectedIndex < serverList.Count - 1) {
                            selectedIndex++;
                            while (selectedIndex >= xOffset + itemCount) {
                                xOffset++;
                            }
                        } else {
                            selectedIndex = 0;
                            xOffset = 0;
                        }
                        pressedCount = Math.Min(pressedCount + 4, 19);
                    }
                } else {
                    pressedCount = 0;
                }
            }
        }

        private void OnServerFound(string name, IPEndPoint endPoint, int currentPlayers, int maxPlayers, int latencyMs)
        {
            for (int i = 0; i < serverList.Count; i++) {
                Server server = serverList[i];
                if (server.EndPoint == endPoint) {
                    server.Name = name;
                    server.CurrentPlayers = currentPlayers;
                    server.MaxPlayers = maxPlayers;
                    server.LatencyMs = latencyMs;
                    return;
                }
            }

            serverList.Add(new Server {
                EndPoint = endPoint,
                Name = name,
                CurrentPlayers = currentPlayers,
                MaxPlayers = maxPlayers,
                LatencyMs = latencyMs
            });
        }
    }
}

#endif