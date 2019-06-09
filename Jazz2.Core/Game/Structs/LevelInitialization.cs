using Jazz2.Actors;

namespace Jazz2.Game.Structs
{
    public enum GameDifficulty
    {
        Default,

        Easy,
        Normal,
        Hard,

        Multiplayer
    }

    public enum ExitType
    {
        None,

        Normal,
        Warp,
        Bonus,
        Special
    }

    public struct PlayerCarryOver
    {
        public PlayerType Type;

        public int Lives;
        public short[] Ammo;
        public byte[] WeaponUpgrades;
        public int Score;
        public int FoodEaten;
        public WeaponType CurrentWeapon;
    }

    public struct LevelInitialization
    {
        public string LevelName;
        public string EpisodeName;

        public GameDifficulty Difficulty;
        public ExitType ExitType;

        public PlayerCarryOver[] PlayerCarryOvers;

        public string LastEpisodeName;

        public LevelInitialization(string episode, string level, GameDifficulty difficulty, PlayerType playerType)
        {
            LevelName = level;
            EpisodeName = episode;
            Difficulty = difficulty;

            ExitType = ExitType.None;

            PlayerCarryOvers = new[] {
                new PlayerCarryOver {
                    Type = playerType,
                    Lives = 3
                }
            };

            LastEpisodeName = null;
        }

        public LevelInitialization(string episode, string level, GameDifficulty difficulty, params PlayerType[] playerTypes)
        {
            LevelName = level;
            EpisodeName = episode;
            Difficulty = difficulty;

            ExitType = ExitType.None;

            PlayerCarryOvers = new PlayerCarryOver[playerTypes.Length];
            for (int i = 0; i < playerTypes.Length; i++) {
                PlayerCarryOvers[i] = new PlayerCarryOver {
                    Type = playerTypes[i],
                    Lives = 3
                };
            }

            LastEpisodeName = null;
        }
    }
}