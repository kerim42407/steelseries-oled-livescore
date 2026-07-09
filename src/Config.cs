namespace OledLiveScore
{
    internal static class Config
    {
        public const int PollSeconds = 15;

        public static readonly string[] Leagues =
        {
            "fifa.world", "fifa.cwc", "club.friendly", "uefa.champions", "uefa.europa",
            "tur.1", "eng.1", "esp.1", "ita.1", "ger.1", "fra.1", "rus.1", "fifa.friendly"
        };

        public const string GameId = "LIVESCORE";
        public const string GameName = "Live Score";
        public const string EventName = "SCORE";
        public const string Screen = "screened-128x40";
        public const string UserAgent = "Mozilla/5.0";
        public const string AppName = "OledLiveScore";
    }
}
