using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OledLiveScore
{
    // Reads match data from ESPN's public soccer endpoints. No API key.
    internal sealed class EspnClient
    {
        static EspnClient()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
            catch { /* older platforms */ }
        }

        private object GetJson(string url)
        {
            using (var wc = new TimedWebClient { TimeoutMs = 12000 })
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers[HttpRequestHeader.UserAgent] = Config.UserAgent;
                return Json.Parse(wc.DownloadString(url));
            }
        }

        public Team FindTeam(string query)
        {
            var url = "https://site.web.api.espn.com/apis/common/v3/search?query="
                      + Uri.EscapeDataString(query) + "&limit=10&sport=soccer";
            var res = GetJson(url);
            foreach (var item in Json.Arr(Json.Get(res, "items")))
            {
                if (Json.Str(Json.Get(item, "type")) == "team")
                {
                    return new Team
                    {
                        Id = Json.Str(Json.Get(item, "id")),
                        Name = Json.Str(Json.Get(item, "displayName")),
                        League = Json.Str(Json.Get(item, "defaultLeagueSlug"))
                    };
                }
            }
            return null;
        }

        private static int CompetitorScore(object c)
        {
            var score = Json.Get(c, "score");
            var val = Json.Get(score, "value");
            if (val != null) return Json.Int(val);
            if (score != null) return Json.Int(score);
            return 0;
        }

        private static DateTime ParseLocal(string s)
        {
            DateTime dt;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dt))
                return dt.ToLocalTime();
            return DateTime.MinValue;
        }

        private static void AddMatch(Dictionary<string, Match> store, object e)
        {
            var comp = Json.Arr(Json.Get(e, "competitions")).FirstOrDefault();
            if (comp == null) return;
            var competitors = Json.Arr(Json.Get(comp, "competitors"));
            var home = competitors.FirstOrDefault(x => Json.Str(Json.Get(x, "homeAway")) == "home");
            var away = competitors.FirstOrDefault(x => Json.Str(Json.Get(x, "homeAway")) == "away");
            if (home == null || away == null) return;

            var id = Json.Str(Json.Get(e, "id"));
            store[id] = new Match
            {
                Id = id,
                When = ParseLocal(Json.Str(Json.Get(e, "date"))),
                Home = Json.Str(Json.Path(home, "team", "abbreviation")),
                Away = Json.Str(Json.Path(away, "team", "abbreviation")),
                HScore = CompetitorScore(home),
                AScore = CompetitorScore(away),
                Status = Json.Str(Json.Path(comp, "status", "type", "shortDetail")),
                State = Json.Str(Json.Path(comp, "status", "type", "state"))
            };
        }

        public List<Match> GetTeamMatches(Team team)
        {
            var store = new Dictionary<string, Match>();

            try
            {
                var sch = GetJson("https://site.api.espn.com/apis/site/v2/sports/soccer/"
                                  + team.League + "/teams/" + team.Id + "/schedule");
                foreach (var e in Json.Arr(Json.Get(sch, "events"))) AddMatch(store, e);
            }
            catch { /* schedule optional */ }

            foreach (var lig in Config.Leagues)
            {
                object sb;
                try { sb = GetJson("https://site.api.espn.com/apis/site/v2/sports/soccer/" + lig + "/scoreboard"); }
                catch { continue; }

                foreach (var e in Json.Arr(Json.Get(sb, "events")))
                {
                    var comp = Json.Arr(Json.Get(e, "competitions")).FirstOrDefault();
                    var has = Json.Arr(Json.Get(comp, "competitors"))
                        .Any(x => Json.Str(Json.Path(x, "team", "id")) == team.Id);
                    if (has) AddMatch(store, e);
                }
            }

            var now = DateTime.Now;
            var future = now.AddDays(45);
            return store.Values
                .Where(m => IsRelevant(m, now) && m.When <= future)
                .OrderBy(m => m.When)
                .ToList();
        }

        // Keep upcoming/live matches, plus ones that finished within the last day.
        private static bool IsRelevant(Match m, DateTime now)
        {
            return m.State != "post" || m.When >= now.AddDays(-1);
        }

        // All relevant matches on a league's current scoreboard (round/day), earliest first.
        public List<Match> GetLeagueMatches(string slug)
        {
            var store = new Dictionary<string, Match>();
            var sb = GetJson("https://site.api.espn.com/apis/site/v2/sports/soccer/" + slug + "/scoreboard");
            foreach (var e in Json.Arr(Json.Get(sb, "events"))) AddMatch(store, e);

            var now = DateTime.Now;
            return store.Values
                .Where(m => IsRelevant(m, now))
                .OrderBy(m => m.When)
                .ToList();
        }

        public LiveState GetLive(string id)
        {
            var s = GetJson("https://site.api.espn.com/apis/site/v2/sports/soccer/all/summary?event=" + id);
            var comp = Json.Arr(Json.Path(s, "header", "competitions")).FirstOrDefault();
            var competitors = Json.Arr(Json.Get(comp, "competitors"));
            var home = competitors.FirstOrDefault(x => Json.Str(Json.Get(x, "homeAway")) == "home");
            var away = competitors.FirstOrDefault(x => Json.Str(Json.Get(x, "homeAway")) == "away");

            var hs = Json.Int(Json.Get(home, "score"));
            var as_ = Json.Int(Json.Get(away, "score"));
            var homeAbbr = Json.Str(Json.Path(home, "team", "abbreviation"));
            var awayAbbr = Json.Str(Json.Path(away, "team", "abbreviation"));
            var state = Json.Str(Json.Path(comp, "status", "type", "state"));

            var top = homeAbbr + " " + hs + "-" + as_ + " " + awayAbbr;
            string bottom;
            if (state == "pre")
                bottom = ParseLocal(Json.Str(Json.Get(comp, "date")))
                    .ToString("dd MMM HH:mm", CultureInfo.InvariantCulture);
            else
                bottom = Json.Str(Json.Path(comp, "status", "type", "shortDetail"));

            var homeId = Json.Str(Json.Path(home, "team", "id"));
            var plays = new List<ScoringPlay>();
            foreach (var ev in Json.Arr(Json.Get(s, "keyEvents")))
            {
                if (!Json.Bool(Json.Get(ev, "scoringPlay"))) continue;

                string scorer = null;
                var parts = Json.Arr(Json.Get(ev, "participants"));
                if (parts.Length > 0) scorer = Json.Str(Json.Path(parts[0], "athlete", "displayName"));
                if (string.IsNullOrEmpty(scorer))
                    scorer = Regex.Replace(Json.Str(Json.Get(ev, "shortText")),
                        @"\s+(Goal|Penalty.*|Own Goal).*$", "");

                plays.Add(new ScoringPlay
                {
                    Id = Json.Str(Json.Get(ev, "id")),
                    Scorer = TextUtils.FormatScorer(scorer),
                    Minute = Json.Str(Json.Path(ev, "clock", "displayValue")),
                    Side = Json.Str(Json.Path(ev, "team", "id")) == homeId ? "home" : "away"
                });
            }

            return new LiveState
            {
                Top = top,
                Bottom = bottom,
                State = state,
                HomeAbbr = homeAbbr,
                HomeScore = hs,
                AwayAbbr = awayAbbr,
                AwayScore = as_,
                Plays = plays
            };
        }

    }
}
