using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Duality.IO;
using Jazz2.Game.Structs;
using Jazz2.Storage;

namespace Jazz2.Game.UI.Menu
{
    public class EpisodeSelectSection : MainMenuSection
    {
        private List<Episode> episodes = new List<Episode>();

        private int selectedIndex;
        private float animation;

        public EpisodeSelectSection()
        {
            JsonParser jsonParser = new JsonParser();

            foreach (string episode in DirectoryOp.GetDirectories(PathOp.Combine(DualityApp.DataDirectory, "Episodes"))) {
                string pathAbsolute = PathOp.Combine(episode, ".res");
                if (FileOp.Exists(pathAbsolute)) {
                    Episode json;
                    using (Stream s = DualityApp.SystemBackend.FileSystem.OpenFile(pathAbsolute, FileAccessMode.Read)) {
                        json = jsonParser.Parse<Episode>(s);
                    }
                    json.Token = PathOp.GetFileName(episode);

                    if (!DirectoryOp.Exists(PathOp.Combine(episode, json.FirstLevel))) {
                        continue;
                    }

                    if (json.PreviousEpisode != null) {
                        int time = Preferences.Get<int>("EpisodeTime_" + json.PreviousEpisode);
                        json.IsAvailable = (time > 0);
                    } else {
                        json.IsAvailable = true;
                    }

                    if (episodes.Count >= json.Position) {
                        episodes.Insert(json.Position - 1, json);
                    } else {
                        episodes.Add(json);
                    }
                }
            }
        }

        public override void OnShow(MainMenu root)
        {
            animation = 0f;
            base.OnShow(root);
        }

        public override void OnPaint(IDrawDevice device, Canvas c)
        {
            Vector2 center = device.TargetSize * 0.5f;

            const float topLine = 131f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial(c, "MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, ColorRgba.White, 55f, (bottomLine - topLine) * 0.063f, new Rect(0f, 0.3f, 1f, 0.4f));

            int charOffset = 0;
            api.DrawStringShadow(device, ref charOffset, "Select Episode", center.X, 110f,
                Alignment.Center, new ColorRgba(0.5f, 0.5f), 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);

            if (episodes.Count > 0) {
                float topItem = topLine - 5f;
                float bottomItem = bottomLine + 5f;
                float contentHeight = bottomItem - topItem;
                float itemSpacing = contentHeight / (episodes.Count + 1);

                topItem += itemSpacing;

                for (int i = 0; i < episodes.Count; i++) {
                    if (selectedIndex == i) {
                        float size = 0.5f + Ease.OutElastic(animation) * 0.6f;

                        if (episodes[i].IsAvailable) {
                            api.DrawStringShadow(device, ref charOffset, episodes[i].Name, center.X, topItem,
                                Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
                        } else {
                            api.DrawString(device, ref charOffset, episodes[i].Name, center.X, topItem,
                                Alignment.Center, new ColorRgba(0.48f, 0.48f), size);
                        }
                    } else {
                        if (episodes[i].IsAvailable) {
                            api.DrawString(device, ref charOffset, episodes[i].Name, center.X, topItem,
                                Alignment.Center, ColorRgba.TransparentBlack, 0.9f);
                        } else {
                            api.DrawString(device, ref charOffset, episodes[i].Name, center.X, topItem,
                                Alignment.Center, new ColorRgba(0.4f, 0.4f), 0.9f);
                        }
                    }

                    topItem += itemSpacing;
                }
            } else {
                api.DrawStringShadow(device, ref charOffset, "Episodes not found!", center.X, center.Y, Alignment.Center,
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
                if (episodes[selectedIndex].IsAvailable) {
                    api.PlaySound("MenuSelect", 0.5f);
                    api.SwitchToSection(new StartGameOptionsSection(episodes[selectedIndex].Token, episodes[selectedIndex].FirstLevel, episodes[selectedIndex].PreviousEpisode));
                }
            } else if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }

            if (episodes.Count > 0) {
                if (DualityApp.Keyboard.KeyHit(Key.Up)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;
                    if (selectedIndex > 0) {
                        selectedIndex--;
                    } else {
                        selectedIndex = episodes.Count - 1;
                    }
                } else if (DualityApp.Keyboard.KeyHit(Key.Down)) {
                    api.PlaySound("MenuSelect", 0.4f);
                    animation = 0f;
                    if (selectedIndex < episodes.Count - 1) {
                        selectedIndex++;
                    } else {
                        selectedIndex = 0;
                    }
                }
            }
        }
    }
}