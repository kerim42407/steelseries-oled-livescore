using System;
using System.Threading;

namespace OledLiveScore
{
    // Flash GOAL! while blinking the scoring team's number, then reveal the scorer.
    internal static class GoalAnimation
    {
        public static void Play(GameSenseClient gs,
            string homeAbbr, int homeScore, string awayAbbr, int awayScore,
            string side, string scorer, string minute, CancellationToken ct)
        {
            var hs = homeScore.ToString();
            var as_ = awayScore.ToString();
            var on = homeAbbr + " " + hs + "-" + as_ + " " + awayAbbr;
            var off = side == "home"
                ? homeAbbr + " " + new string(' ', hs.Length) + "-" + as_ + " " + awayAbbr
                : homeAbbr + " " + hs + "-" + new string(' ', as_.Length) + " " + awayAbbr;

            for (var i = 0; i < 5; i++)
            {
                if (ct.IsCancellationRequested) return;
                SafeUpdate(gs, on, "*** GOAL! ***");
                Wait(220, ct);
                SafeUpdate(gs, off, "");
                Wait(180, ct);
            }

            Marquee(gs, on, (minute + " " + scorer).Trim(), 3, ct);
        }

        // Show a line; if it is too long, scroll it across the width for a few seconds.
        private static void Marquee(GameSenseClient gs, string top, string text, int seconds, CancellationToken ct)
        {
            if (text.Length <= 21)
            {
                SafeUpdate(gs, top, text);
                Wait(seconds * 1000, ct);
                return;
            }

            var pad = text + "   ";
            var doubled = pad + pad;
            var delay = Math.Max(120, seconds * 1000 / pad.Length);
            for (var i = 0; i < pad.Length; i++)
            {
                if (ct.IsCancellationRequested) return;
                SafeUpdate(gs, top, doubled.Substring(i, 21));
                Wait(delay, ct);
            }
        }

        private static void SafeUpdate(GameSenseClient gs, string top, string bottom)
        {
            try { gs.UpdateScreen(top, bottom); }
            catch { /* GG may be closing; ignore a dropped frame */ }
        }

        private static void Wait(int ms, CancellationToken ct)
        {
            ct.WaitHandle.WaitOne(ms);
        }
    }
}
