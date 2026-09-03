using System.Windows.Forms;
using Microsoft.Win32;

namespace OledLiveScore
{
    // Optional "run at login" via the per-user Run key (no admin rights needed).
    internal static class Autostart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = Config.AppName;

        public static bool IsEnabled()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey))
                return k != null && k.GetValue(ValueName) != null;
        }

        public static void Set(bool enabled)
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true) ?? Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (k == null) return;
                if (enabled)
                    k.SetValue(ValueName,
                        "\"" + Application.ExecutablePath + "\" " + Program.SilentFlag);
                else if (k.GetValue(ValueName) != null)
                    k.DeleteValue(ValueName, false);
            }
        }

        // Rewrite the entry on startup so an old or moved exe path picks up the flag.
        public static void Refresh()
        {
            try { if (IsEnabled()) Set(true); }
            catch { /* registry locked down */ }
        }
    }
}
