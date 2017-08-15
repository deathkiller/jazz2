namespace Jazz2.Game.Structs
{
    public class Episode
    {
        public string Token;

        public string Name { get; set; }
        public int Position { get; set; }
        public string FirstLevel { get; set; }

        public string PreviousEpisode { get; set; }
        public string NextEpisode { get; set; }

        public bool IsAvailable;
    }
}