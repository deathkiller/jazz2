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

        partial void UpdateRichPresence(LevelInitialization? levelInit)
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
                string state;
                string smallImage;
                if (levelInit.Value.PlayerCarryOvers.Length == 0 && levelInit.Value.Difficulty == GameDifficulty.Multiplayer) {
                    state = "Playing Online Multiplayer";
                    smallImage = "playing-online";
                } else if (levelInit.Value.PlayerCarryOvers.Length == 1) {
                    if (levelInit.Value.EpisodeName != "unknown") {
                        state = "Playing Story as ";
                    } else {
                        state = "Playing Custom Game as ";
                    }

                    switch (levelInit.Value.PlayerCarryOvers[0].Type) {
                        default:
                        case PlayerType.Jazz: state += "Jazz"; smallImage = "playing-jazz"; break;
                        case PlayerType.Spaz: state += "Spaz"; smallImage = "playing-spaz"; break;
                        case PlayerType.Lori: state += "Lori"; smallImage = "playing-lori"; break;
                    }
                } else {
                    state = "Playing Local Multiplayer";
                    smallImage = null;
                }

                richPresence = new RichPresence {
                    State = state,
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