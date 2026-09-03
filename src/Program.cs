using System;
using System.Threading;
using System.Windows.Forms;

namespace OledLiveScore
{
    internal static class Program
    {
        // Passed by the "Start with Windows" entry and by the post-update relaunch,
        // so neither pops the picker. "--autostart" is the older spelling.
        internal const string SilentFlag = "--silent";
        private static readonly string[] SilentFlags = { SilentFlag, "--autostart" };

        // Set by a second launch to ask the running instance for the picker.
        internal const string WakeEventName = "OledLiveScore.ShowPicker";

        private const string InstanceMutexName = "OledLiveScore.SingleInstance";

        [STAThread]
        private static void Main(string[] args)
        {
            var quiet = Array.Exists(args, a => Array.Exists(SilentFlags,
                f => string.Equals(a, f, StringComparison.OrdinalIgnoreCase)));

            bool first;
            using (new Mutex(true, InstanceMutexName, out first))
            {
                if (!first)
                {
                    // Already in the tray: hand the request over instead of starting twice.
                    if (!quiet) Wake();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Autostart.Refresh();
                Application.Run(new TrayApp(!quiet));
            }
        }

        private static void Wake()
        {
            try
            {
                EventWaitHandle h;
                if (EventWaitHandle.TryOpenExisting(WakeEventName, out h))
                    using (h) h.Set();
            }
            catch { /* nothing we can do */ }
        }
    }
}
