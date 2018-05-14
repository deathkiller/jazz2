using System;
using System.IO;
using System.Text.Json;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Duality.IO;
using Duality.Resources;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Storage;

namespace Jazz2.Game.UI.Menu
{
    public class EpisodeSelectSection : MainMenuSection
    {
        private struct EpisodeEntry
        {
            public Episode Episode;

            public bool IsAvailable;
            public bool CanContinue;
            public ContentRef<Material> Logo;
        }

        private RawList<EpisodeEntry> episodes = new RawList<EpisodeEntry>();

        private int selectedIndex;
        private float selectAnimation;

        private bool expanded;
        private float expandedAnimation;

        public EpisodeSelectSection()
        {
            JsonParser jsonParser = new JsonParser();
            IImageCodec imageCodec = ImageCodec.GetRead(ImageCodec.FormatPng);

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

                    EpisodeEntry entry;
                    entry.Episode = json;
                    if (json.PreviousEpisode != null) {
                        int time = Preferences.Get<int>("EpisodeEnd_Time_" + json.PreviousEpisode);
                        entry.IsAvailable = (time > 0);
                    } else {
                        entry.IsAvailable = true;
                    }

                    entry.CanContinue = Preferences.Get<byte[]>("EpisodeContinue_Misc_" + entry.Episode.Token) != null;

                    string logoPath = PathOp.Combine(episode, ".png");
                    if (FileOp.Exists(logoPath)) {
                        PixelData pixelData;
                        using (Stream s = FileOp.Open(logoPath, FileAccessMode.Read)) {
                            pixelData = imageCodec.Read(s);
                        }

                        Texture texture = new Texture(new Pixmap(pixelData), TextureSizeMode.NonPowerOfTwo);
                        entry.Logo = new Material(DrawTechnique.Alpha, texture);
                    } else {
                        entry.Logo = null;
                    }

                    episodes.Add(entry);
                }
            }

            episodes.Sort((x, y) => x.Episode.Position.CompareTo(y.Episode.Position));
        }

        public override void OnShow(MainMenu root)
        {
            selectAnimation = 0f;
            base.OnShow(root);
        }

        public override void OnPaint(Canvas canvas)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

            const float topLine = 131f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial("MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, ColorRgba.White, 55f, (bottomLine - topLine) * 0.063f, new Rect(0f, 0.3f, 1f, 0.4f));

            api.DrawMaterial("MenuLine", 0, center.X, topLine, Alignment.Center, ColorRgba.White, 1.6f);
            api.DrawMaterial("MenuLine", 1, center.X, bottomLine, Alignment.Center, ColorRgba.White, 1.6f);

            int charOffset = 0;
            api.DrawStringShadow(ref charOffset, "Select Episode", center.X, 110f,
                Alignment.Center, new ColorRgba(0.5f, 0.5f), 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);

            if (episodes.Count > 0) {
                float topItem = topLine - 5f;
                float bottomItem = bottomLine + 5f;
                float contentHeight = bottomItem - topItem;
                float itemSpacing = contentHeight / (episodes.Count + 1);

                topItem += itemSpacing;

                for (int i = 0; i < episodes.Count; i++) {
                    if (selectedIndex == i) {
                        float size = 0.5f + Ease.OutElastic(selectAnimation) * 0.5f + (1f - expandedAnimation) * 0.2f;

                        if (episodes[i].IsAvailable) {
                            if (episodes[i].Logo.IsAvailable) {
                                api.DrawString(ref charOffset, episodes[i].Episode.Name, center.X, topItem,
                                    Alignment.Center, new ColorRgba(0.44f, 0.5f * MathF.Max(0f, 1f - selectAnimation * 2f)), 0.9f - selectAnimation * 0.5f);

                                ContentRef<Material> logo = episodes[i].Logo;
                                Texture texture = logo.Res.MainTexture.Res;

                                Vector2 originPos = new Vector2(center.X, topItem);
                                Alignment.Center.ApplyTo(ref originPos, new Vector2(texture.InternalWidth * size, texture.InternalHeight * size));

                                canvas.State.SetMaterial(logo);
                                canvas.State.ColorTint = ColorRgba.White.WithAlpha(1f - expandedAnimation * 0.5f);
                                canvas.FillRect(originPos.X, originPos.Y, texture.InternalWidth * size, texture.InternalHeight * size);
                            } else {
                                api.DrawStringShadow(ref charOffset, episodes[i].Episode.Name, center.X, topItem,
                                    Alignment.Center, null, size, charSpacing: 0.9f);
                            }

                            if (episodes[i].CanContinue) {
                                float moveX = expandedAnimation * -26f;

                                api.DrawString(ref charOffset, ">", center.X + 80f + moveX, topItem,
                                    Alignment.Right, new ColorRgba(0.5f, 0.5f * MathF.Min(1f, 0.4f + selectAnimation)), 1f, charSpacing: 0.9f);

                                if (expanded) {
                                    api.DrawStringShadow(ref charOffset, "Restart episode", center.X + 90f + moveX, topItem,
                                        Alignment.Left, new ColorRgba(0.48f, 0.40f, 0.22f, 0.5f * MathF.Min(1f, 0.4f + expandedAnimation)), 0.8f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.8f);
                                }
                            }
                        } else {
                            api.DrawString(ref charOffset, episodes[i].Episode.Name, center.X, topItem,
                                Alignment.Center, new ColorRgba(0.4f, MathF.Max(0.3f, 0.4f - selectAnimation * 0.4f)), MathF.Max(0.7f, 0.9f - selectAnimation * 0.6f));

                            int index = episodes.IndexOfFirst(entry => entry.Episode.Token == episodes[i].Episode.PreviousEpisode);
                            Episode previousEpisode;
                            if (index == -1) {
                                previousEpisode = null;
                            } else {
                                previousEpisode = episodes[index].Episode;
                            }

                            string info;
                            if (previousEpisode == null) {
                                info = "Episode is locked!";
                            } else {
                                info = "You must complete \"" + previousEpisode.Name + "\" first!";
                            }

                            api.DrawStringShadow(ref charOffset, info, center.X, topItem,
                                Alignment.Center, new ColorRgba(0.66f, 0.42f, 0.32f, MathF.Min(0.5f, 0.2f + 2f * selectAnimation)), 0.7f * size, charSpacing: 0.9f);
                        }
                    } else {
                        if (episodes[i].IsAvailable) {
                            api.DrawString(ref charOffset, episodes[i].Episode.Name, center.X, topItem,
                                Alignment.Center, ColorRgba.TransparentBlack, 0.9f);
                        } else {
                            api.DrawString(ref charOffset, episodes[i].Episode.Name, center.X, topItem,
                                Alignment.Center, new ColorRgba(0.4f, 0.4f), 0.9f);
                        }
                    }

                    topItem += itemSpacing;
                }
            } else {
                api.DrawStringShadow(ref charOffset, "No episode found!", center.X, center.Y, Alignment.Center,
                    new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f), 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);
            }
        }

        public override void OnUpdate()
        {
            if (selectAnimation < 1f) {
                selectAnimation = Math.Min(selectAnimation + Time.TimeMult * 0.016f, 1f);
            }

            if (expanded && expandedAnimation < 1f) {
                expandedAnimation = Math.Min(expandedAnimation + Time.TimeMult * 0.06f, 1f);
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                if (episodes[selectedIndex].IsAvailable) {
                    api.PlaySound("MenuSelect", 0.5f);
                    if (episodes[selectedIndex].CanContinue) {
                        if (expanded) {
                            // Restart episode
                            // Clear continue data
                            string episodeName = episodes[selectedIndex].Episode.Token;
                            Preferences.Remove("EpisodeContinue_Misc_" + episodeName);
                            Preferences.Remove("EpisodeContinue_Level_" + episodeName);
                            Preferences.Remove("EpisodeContinue_Ammo_" + episodeName);
                            Preferences.Remove("EpisodeContinue_Upgrades_" + episodeName);

                            Preferences.Commit();

                            episodes.Data[selectedIndex].CanContinue = false;
                            expanded = false;
                            expandedAnimation = 0f;

                            api.SwitchToSection(new StartGameOptionsSection(
                                episodes[selectedIndex].Episode.Token,
                                episodes[selectedIndex].Episode.FirstLevel,
                                episodes[selectedIndex].Episode.PreviousEpisode
                            ));
                        } else {
                            string episodeName = episodes[selectedIndex].Episode.Token;
                            string levelName = Preferences.Get<string>("EpisodeContinue_Level_" + episodeName);

                            // Lives, Difficulty and PlayerType is saved in Misc array [Jazz2.Core/Game/Controller.cs: ~146]
                            byte[] misc = Preferences.Get<byte[]>("EpisodeContinue_Misc_" + episodeName);

                            LevelInitialization carryOver = new LevelInitialization(
                                episodeName,
                                levelName,
                                (GameDifficulty)misc[1],
                                (PlayerType)misc[2]
                            );

                            ref PlayerCarryOver player = ref carryOver.PlayerCarryOvers[0];

                            if (misc[0] > 0) {
                                player.Lives = misc[0];
                            }

                            int[] ammo = Preferences.Get<int[]>("EpisodeContinue_Ammo_" + episodeName);
                            if (ammo != null) {
                                player.Ammo = ammo;
                            }

                            byte[] upgrades = Preferences.Get<byte[]>("EpisodeContinue_Upgrades_" + episodeName);
                            if (upgrades != null) {
                                player.WeaponUpgrades = upgrades;
                            }

                            api.SwitchToLevel(carryOver);
                        }
                    } else {
                        // Start the episode from the first level
                        api.SwitchToSection(new StartGameOptionsSection(
                            episodes[selectedIndex].Episode.Token,
                            episodes[selectedIndex].Episode.FirstLevel,
                            episodes[selectedIndex].Episode.PreviousEpisode
                        ));
                    }
                }
            } else if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }

            if (episodes.Count > 1) {
                if (ControlScheme.MenuActionHit(PlayerActions.Up)) {
                    if (episodes.Count > 1) {
                        api.PlaySound("MenuSelect", 0.4f);
                        selectAnimation = 0f;

                        expanded = false;
                        expandedAnimation = 0f;

                        if (selectedIndex > 0) {
                            selectedIndex--;
                        } else {
                            selectedIndex = episodes.Count - 1;
                        }
                    }
                } else if (ControlScheme.MenuActionHit(PlayerActions.Down)) {
                    if (episodes.Count > 1) {
                        api.PlaySound("MenuSelect", 0.4f);
                        selectAnimation = 0f;

                        expanded = false;
                        expandedAnimation = 0f;

                        if (selectedIndex < episodes.Count - 1) {
                            selectedIndex++;
                        } else {
                            selectedIndex = 0;
                        }
                    }
                }
            }
            
            if (ControlScheme.MenuActionHit(PlayerActions.Left)) {
                expanded = false;
                expandedAnimation = 0f;
            } else if (ControlScheme.MenuActionHit(PlayerActions.Right)) {
                if (episodes[selectedIndex].CanContinue) {
                    expanded = true;
                }
            }
        }
    }
}