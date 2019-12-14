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
                    State = "Resting In Main Menu",
                    LargeImage = "main-transparent"
                };
            } else {
                string details, state, smallImage;
                if (levelInit.Value.PlayerCarryOvers.Length == 0 && levelInit.Value.Difficulty == GameDifficulty.Multiplayer) {
                    details = "Playing Online Multiplayer";
                    smallImage = "playing-online";
                } else if (levelInit.Value.PlayerCarryOvers.Length == 1) {
                    if (levelInit.Value.EpisodeName != "unknown") {
                        details = "Playing Story as ";
                    } else {
                        details = "Playing Custom Game as ";
                    }

                    switch (levelInit.Value.PlayerCarryOvers[0].Type) {
                        default:
                        case PlayerType.Jazz: details += "Jazz"; smallImage = "playing-jazz"; break;
                        case PlayerType.Spaz: details += "Spaz"; smallImage = "playing-spaz"; break;
                        case PlayerType.Lori: details += "Lori"; smallImage = "playing-lori"; break;
                    }
                } else {
                    details = "Playing Local Multiplayer";
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
                    LargeImage = "main-transparent",
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