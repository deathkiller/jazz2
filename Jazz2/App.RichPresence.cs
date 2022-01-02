using Jazz2.Actors;
using Jazz2.Discord;
using Jazz2.Game.Structs;

namespace Jazz2.Game
{
    partial class App
    {
        private DiscordRpcClient discord;

        partial void InitRichPresence()
        {
            discord = new DiscordRpcClient("591586859960762378");
            if (!discord.Connect()) {
                discord = null;
            }
        }

        partial void UpdateRichPresence(LevelInitialization? levelInit, string targetName)
        {
            if (discord == null) {
                return;
            }

            RichPresence richPresence;
            if (levelInit == null) {
                richPresence = new RichPresence {
                    State = "Resting in main menu",
                    LargeImage = "main-transparent"
                };
            } else {
                string details, state, smallImage;
                string largeImage = null;

                if (levelInit.Value.PlayerCarryOvers.Length == 0 && levelInit.Value.Difficulty == GameDifficulty.Multiplayer) {
                    details = "Playing online multiplayer";
                    smallImage = "playing-online";
                    largeImage = "main-transparent";
                } else if (levelInit.Value.PlayerCarryOvers.Length == 1) {
                    switch (levelInit.Value.EpisodeName) {
                        case "prince":
                            if (levelInit.Value.LevelName == "01_castle1" || levelInit.Value.LevelName == "02_castle1n") {
                                largeImage = "level-prince-01";
                            } else if (levelInit.Value.LevelName == "03_carrot1" || levelInit.Value.LevelName == "04_carrot1n") {
                                largeImage = "level-prince-02";
                            } else if (levelInit.Value.LevelName == "05_labrat1" || levelInit.Value.LevelName == "06_labrat2" || levelInit.Value.LevelName == "bonus_labrat3") {
                                largeImage = "level-prince-03";
                            }
                            break;
                        case "rescue":
                            if (levelInit.Value.LevelName == "01_colon1" || levelInit.Value.LevelName == "02_colon2") {
                                largeImage = "level-rescue-01";
                            } else if (levelInit.Value.LevelName == "03_psych1" || levelInit.Value.LevelName == "04_psych2" || levelInit.Value.LevelName == "bonus_psych3") {
                                largeImage = "level-rescue-02";
                            } else if (levelInit.Value.LevelName == "05_beach" || levelInit.Value.LevelName == "06_beach2") {
                                largeImage = "level-rescue-03";
                            }
                            break;
                        case "flash":
                            if (levelInit.Value.LevelName == "01_diam1" || levelInit.Value.LevelName == "02_diam3") {
                                largeImage = "level-flash-01";
                            } else if (levelInit.Value.LevelName == "03_tube1" || levelInit.Value.LevelName == "04_tube2" || levelInit.Value.LevelName == "bonus_tube3") {
                                largeImage = "level-flash-02";
                            } else if (levelInit.Value.LevelName == "05_medivo1" || levelInit.Value.LevelName == "06_medivo2" || levelInit.Value.LevelName == "bonus_garglair") {
                                largeImage = "level-flash-03";
                            }
                            break;
                        case "monk":
                            if (levelInit.Value.LevelName == "01_jung1" || levelInit.Value.LevelName == "02_jung2") {
                                largeImage = "level-monk-01";
                            } else if (levelInit.Value.LevelName == "03_hell" || levelInit.Value.LevelName == "04_hell2") {
                                largeImage = "level-monk-02";
                            } else if (levelInit.Value.LevelName == "05_damn" || levelInit.Value.LevelName == "06_damn2") {
                                largeImage = "level-monk-03";
                            }
                            break;
                        case "secretf":
                            if (levelInit.Value.LevelName == "01_easter1" || levelInit.Value.LevelName == "02_easter2" || levelInit.Value.LevelName == "03_easter3") {
                                largeImage = "level-secretf-01";
                            } else if (levelInit.Value.LevelName == "04_haunted1" || levelInit.Value.LevelName == "05_haunted2" || levelInit.Value.LevelName == "06_haunted3") {
                                largeImage = "level-secretf-02";
                            } else if (levelInit.Value.LevelName == "07_town1" || levelInit.Value.LevelName == "08_town2" || levelInit.Value.LevelName == "09_town3") {
                                largeImage = "level-secretf-03";
                            }
                            break;
                        case "xmas98":
                        case "xmas99":
                            largeImage = "level-xmas";
                            break;
                        case "share":
                            largeImage = "level-share";
                            break;
                    }

                    if (largeImage == null) {
                        details = "Playing as ";
                        largeImage = "main-transparent";

                        switch (levelInit.Value.PlayerCarryOvers[0].Type) {
                            default:
                            case PlayerType.Jazz: smallImage = "playing-jazz"; break;
                            case PlayerType.Spaz: smallImage = "playing-spaz"; break;
                            case PlayerType.Lori: smallImage = "playing-lori"; break;
                        }
                    } else {
                        details = "Playing story as ";
                        smallImage = null;
                    }

                    switch (levelInit.Value.PlayerCarryOvers[0].Type) {
                        default:
                        case PlayerType.Jazz: details += "Jazz"; break;
                        case PlayerType.Spaz: details += "Spaz"; break;
                        case PlayerType.Lori: details += "Lori"; break;
                    }
                } else {
                    details = "Playing local multiplayer";
                    largeImage = "main-transparent";
                    smallImage = null;
                }

                if (!string.IsNullOrEmpty(targetName)) {
                    state = "— on " + targetName;
                } else {
                    state = null;
                }

                richPresence = new RichPresence {
                    State = state,
                    Details = details,
                    LargeImage = largeImage,
                    SmallImage = smallImage,
                    SmallImageTooltip = state
                };
            }

            discord.SetRichPresence(richPresence);
        }

        public string TryGetDefaultUserName()
        {
            if (discord == null) {
                return null;
            }

            return discord.UserName;
        }
    }
}