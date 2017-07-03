using Jazz2.Actors;

namespace Jazz2.Game.Structs
{
    public enum GameDifficulty
    {
        Default,

        Easy,
        Normal,
        Hard
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
        public int[] Ammo;
        public byte[] WeaponUpgrades;
        public int FastFires;
        public int Score;
        public int FoodCounter;
        public WeaponType CurrentWeapon;
    }

    public struct InitLevelData
    {
        public string LevelName;
        public string EpisodeName;

        public GameDifficulty Difficulty;
        public ExitType ExitType;

        public PlayerCarryOver[] PlayerCarryOvers;

        public InitLevelData(string episode, string level, GameDifficulty difficulty, PlayerType playerType)
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
        }
    }
}