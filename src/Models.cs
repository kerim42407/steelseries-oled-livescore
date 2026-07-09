using System;
using System.Collections.Generic;

namespace OledLiveScore
{
    internal sealed class Team
    {
        public string Id;
        public string Name;
        public string League;
    }

    internal sealed class Match
    {
        public string Id;
        public DateTime When;
        public string Home;
        public string Away;
        public int HScore;
        public int AScore;
        public string Status;
        public string State;
    }

    internal sealed class ScoringPlay
    {
        public string Id;
        public string Scorer;
        public string Minute;
        public string Side; // "home" or "away"
    }

    internal sealed class LiveState
    {
        public string Top = "Loading...";
        public string Bottom = "";
        public string State = "pre";
        public string HomeAbbr = "HOME";
        public int HomeScore;
        public string AwayAbbr = "AWAY";
        public int AwayScore;
        public List<ScoringPlay> Plays = new List<ScoringPlay>();
    }
}
