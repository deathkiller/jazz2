using System;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Jazz2.Actors;
using Jazz2.Game.Structs;
using Jazz2.Storage;

namespace Jazz2.Game.UI.Menu
{
    public class StartGameOptionsSection : MenuSection
    {
        private readonly string episodeName, levelName, previousEpisodeName;

        private readonly string[] items = {
            "Character",
            "Difficulty",
            "Start Game"
        };

        private int selectedIndex = 2;
        private int selectedPlayerType;
        private int selectedDifficulty = 1;
        private float animation;

        private int availableCharacters;

        public StartGameOptionsSection(string episodeName, string levelName, string previousEpisodeName)
        {
            this.episodeName = episodeName;
            this.levelName = levelName;
            this.previousEpisodeName = previousEpisodeName;
        }

        public override void OnShow(IMenuContainer root)
        {
            animation = 0f;
            base.OnShow(root);

            availableCharacters = (api.IsAnimationPresent("MenuDifficultyLori") ? 3 : 2);
        }

        public override void OnPaint(Canvas canvas)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;
            center.Y *= 0.8f;

            string difficultyName;
            switch (selectedPlayerType) {
                default:
                case 0: difficultyName = "MenuDifficultyJazz"; break;
                case 1: difficultyName = "MenuDifficultySpaz"; break;
                case 2: difficultyName = "MenuDifficultyLori"; break;
            }

            api.DrawMaterial("MenuDim", center.X * 0.36f, center.Y * 1.4f, Alignment.Center, ColorRgba.White, 24f, 36f);
            api.DrawMaterial(difficultyName, selectedDifficulty, center.X * 0.36f, center.Y * 1.4f + 3f, Alignment.Center, new ColorRgba(0f, 0.2f), 0.88f, 0.88f);
            api.DrawMaterial(difficultyName, selectedDifficulty, center.X * 0.36f, center.Y * 1.4f, Alignment.Center, ColorRgba.White, 0.88f, 0.88f);

            int charOffset = 0;
            for (int i = 0; i < items.Length; i++) {
                if (selectedIndex == i) {
                    float size = 0.5f + Ease.OutElastic(animation) * 0.6f;

                    api.DrawMaterial("MenuGlow", center.X, center.Y, Alignment.Center, ColorRgba.White.WithAlpha(0.4f * size), (items[i].Length + 3) * 0.5f * size, 4f * size);

                    api.DrawStringShadow(ref charOffset, items[i], center.X, center.Y,
                        Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
                } else {
                    api.DrawString(ref charOffset, items[i], center.X, center.Y, Alignment.Center,
                        ColorRgba.TransparentBlack, 0.9f);
                }

                if (i == 0) {
                    string[] playerTypes = { "Jazz", "Spaz", "Lori" };
                    ColorRgba[] playerColors = {
                        new ColorRgba(0.2f, 0.45f, 0.2f, 0.5f),
                        new ColorRgba(0.45f, 0.27f, 0.22f, 0.5f),
                        new ColorRgba(0.5f, 0.45f, 0.22f, 0.5f)
                    };

                    float offset, spacing;
                    if (availableCharacters == 1) {
                        offset = spacing = 0f;
                    } else if (availableCharacters == 2) {
                        offset = 50f;
                        spacing = 100f;
                    } else {
                        offset = 100f;
                        spacing = 300f / availableCharacters;
                    }

                    for (int j = 0; j < /*playerTypes.Length*/availableCharacters; j++) {
                        float x = center.X - offset + j * spacing;
                        if (selectedPlayerType == j) {
                            api.DrawMaterial("MenuGlow", x, center.Y + 28f, Alignment.Center, ColorRgba.White.WithAlpha(0.2f), (playerTypes[j].Length + 3) * 0.4f, 2.2f);

                            api.DrawStringShadow(ref charOffset, playerTypes[j], x, center.Y + 28f, Alignment.Center,
                                /*null*/playerColors[j], 0.9f, 0.4f, 0.55f, 0.55f, 8f, 0.9f);
                        } else {
                            api.DrawString(ref charOffset, playerTypes[j], x, center.Y + 28f, Alignment.Center,
                                ColorRgba.TransparentBlack, 0.8f, charSpacing: 0.9f);
                        }
                    }

                    api.DrawStringShadow(ref charOffset, "<", center.X - (100f + 40f), center.Y + 28f, Alignment.Center,
                        ColorRgba.TransparentBlack, 0.7f);
                    api.DrawStringShadow(ref charOffset, ">", center.X + (100f + 40f), center.Y + 28f, Alignment.Center,
                        ColorRgba.TransparentBlack, 0.7f);
                } else if (i == 1) {
                    string[] difficultyTypes = { "Easy", "Medium", "Hard" };
                    for (int j = 0; j < difficultyTypes.Length; j++) {
                        if (selectedDifficulty == j) {
                            api.DrawMaterial("MenuGlow", center.X + (j - 1) * 100f, center.Y + 28f, Alignment.Center, ColorRgba.White.WithAlpha(0.2f), (difficultyTypes[j].Length + 3) * 0.4f, 2.2f);

                            api.DrawStringShadow(ref charOffset, difficultyTypes[j], center.X + (j - 1) * 100f, center.Y + 28f, Alignment.Center,
                                null, 0.9f, 0.4f, 0.55f, 0.55f, 8f, 0.9f);
                        } else {
                            api.DrawString(ref charOffset, difficultyTypes[j], center.X + (j - 1) * 100f, center.Y + 28f, Alignment.Center,
                                ColorRgba.TransparentBlack, 0.8f, charSpacing: 0.9f);
                        }
                    }

                    api.DrawStringShadow(ref charOffset, "<", center.X - (100f + 40f), center.Y + 28f, Alignment.Center,
                        ColorRgba.TransparentBlack, 0.7f);
                    api.DrawStringShadow(ref charOffset, ">", center.X + (100f + 40f), center.Y + 28f, Alignment.Center,
                        ColorRgba.TransparentBlack, 0.7f);
                }

                center.Y += 70f;
            }
        }

        public override void OnUpdate()
        {
            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                if (selectedIndex == 2) {
                    ControlScheme.IsSuspended = true;

                    api.PlaySound("MenuSelect", 0.5f);
                    api.BeginFadeOut(() => {
                        ControlScheme.IsSuspended = false;

                        LevelInitialization carryOver = new LevelInitialization(
                            episodeName,
                            levelName,
                            (GameDifficulty.Easy + selectedDifficulty),
                            (PlayerType.Jazz + selectedPlayerType)
                        );

                        if (!string.IsNullOrEmpty(previousEpisodeName)) {
                            ref PlayerCarryOver player = ref carryOver.PlayerCarryOvers[0];

                            byte lives = Preferences.Get<byte>("EpisodeEnd_Lives_" + previousEpisodeName);
                            int[] ammo = Preferences.Get<int[]>("EpisodeEnd_Ammo_" + previousEpisodeName);
                            byte[] upgrades = Preferences.Get<byte[]>("EpisodeEnd_Upgrades_" + previousEpisodeName);

                            if (lives > 0) {
                                player.Lives = lives;
                            }
                            if (ammo != null) {
                                player.Ammo = ammo;
                            }
                            if (upgrades != null) {
                                player.WeaponUpgrades = upgrades;
                            }
                        }

                        api.SwitchToLevel(carryOver);
                    });
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Left)) {
                if (selectedIndex == 0) {
                    if (selectedPlayerType > 0) {
                        selectedPlayerType--;
                    } else {
                        selectedPlayerType = availableCharacters - 1;
                    }
                } else if (selectedIndex == 1) {
                    if (selectedDifficulty > 0) {
                        selectedDifficulty--;
                    }
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Right)) {
                if (selectedIndex == 0) {
                    if (selectedPlayerType < availableCharacters - 1) {
                        selectedPlayerType++;
                    } else {
                        selectedPlayerType = 0;
                    }
                } else if (selectedIndex == 1) {
                    if (selectedDifficulty < 3 - 1) {
                        selectedDifficulty++;
                    }
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Up)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex > 0) {
                    selectedIndex--;
                } else {
                    selectedIndex = items.Length - 1;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Down)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex < items.Length - 1) {
                    selectedIndex++;
                } else {
                    selectedIndex = 0;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }
        }
    }
}