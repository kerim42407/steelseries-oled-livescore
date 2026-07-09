using System;
using System.Collections.Generic;
using System.Threading;

namespace OledLiveScore
{
    // Background loop: keep the OLED lit, fetch data every PollSeconds, animate new goals.
    internal sealed class ScoreTracker
    {
        private readonly GameSenseClient _gs;
        private readonly EspnClient _espn;

        private Thread _thread;
        private CancellationTokenSource _cts;
        private string _eventId;

        public event Action<string> Status;

        public ScoreTracker(GameSenseClient gs, EspnClient espn)
        {
            _gs = gs;
            _espn = espn;
        }

        public bool IsRunning { get { return _thread != null && _thread.IsAlive; } }

        public void Start(string eventId)
        {
            Stop();
            _eventId = eventId;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _thread = new Thread(() => Run(token)) { IsBackground = true, Name = "ScoreTracker" };
            _thread.Start();
        }

        public void Stop()
        {
            if (_cts != null) _cts.Cancel();
            if (_thread != null && _thread.IsAlive) _thread.Join(1500);
            _thread = null;
            _cts = null;
        }

        private void Report(string s)
        {
            var h = Status;
            if (h != null) h(s);
        }

        private void Run(CancellationToken ct)
        {
            const int refreshMs = 1000;
            var fetchEvery = Math.Max(1, Config.PollSeconds); // screen every 1s, data every PollSeconds

            var cache = new LiveState();
            var seen = new HashSet<string>();
            var seeded = false;
            var tick = 0;

            while (!ct.IsCancellationRequested)
            {
                if (tick % fetchEvery == 0)
                {
                    try
                    {
                        cache = _espn.GetLive(_eventId);
                        Report(cache.Top + " | " + cache.Bottom);

                        if (!seeded)
                        {
                            foreach (var p in cache.Plays) seen.Add(p.Id);
                            seeded = true;
                        }
                        else
                        {
                            foreach (var p in cache.Plays)
                            {
                                if (seen.Contains(p.Id)) continue;
                                seen.Add(p.Id);
                                Report("GOAL " + p.Minute + " " + p.Scorer);
                                GoalAnimation.Play(_gs, cache.HomeAbbr, cache.HomeScore,
                                    cache.AwayAbbr, cache.AwayScore, p.Side, p.Scorer, p.Minute, ct);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Report("Error: " + ex.Message);
                    }
                }

                try { _gs.UpdateScreen(cache.Top, cache.Bottom); }
                catch { /* dropped frame */ }

                tick++;
                ct.WaitHandle.WaitOne(refreshMs);
            }
        }
    }
}
