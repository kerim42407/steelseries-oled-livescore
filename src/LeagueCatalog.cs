namespace OledLiveScore
{
    internal sealed class League
    {
        public readonly string Name;
        public readonly string Slug;

        public League(string name, string slug)
        {
            Name = name;
            Slug = slug;
        }
    }

    // Friendly names shown in the picker dropdown, mapped to ESPN league slugs.
    internal static class LeagueCatalog
    {
        public static readonly League[] All =
        {
            new League("FIFA World Cup", "fifa.world"),
            new League("FIFA Club World Cup", "fifa.cwc"),
            new League("UEFA Champions League", "uefa.champions"),
            new League("UEFA Europa League", "uefa.europa"),
            new League("Premier League (England)", "eng.1"),
            new League("La Liga (Spain)", "esp.1"),
            new League("Serie A (Italy)", "ita.1"),
            new League("Bundesliga (Germany)", "ger.1"),
            new League("Ligue 1 (France)", "fra.1"),
            new League("Super Lig (Turkey)", "tur.1"),
            new League("International Friendlies", "fifa.friendly")
        };
    }
}
