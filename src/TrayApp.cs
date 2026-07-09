using System;
using System.Windows.Forms;

namespace OledLiveScore
{
    // Lives in the system tray. No console, no window unless you open the picker.
    internal sealed class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _icon;
        private readonly Control _marshal;
        private readonly GameSenseClient _gs = new GameSenseClient();
        private readonly EspnClient _espn = new EspnClient();
        private readonly ToolStripMenuItem _stopItem;
        private readonly ToolStripMenuItem _autostartItem;
        private ScoreTracker _tracker;

        public TrayApp()
        {
            _marshal = new Control();
            _marshal.CreateControl(); // force a handle so we can marshal to the UI thread

            var menu = new ContextMenuStrip();
            menu.Items.Add("Pick match...", null, (s, e) => PickMatch());
            _stopItem = new ToolStripMenuItem("Stop", null, (s, e) => StopTracking()) { Enabled = false };
            menu.Items.Add(_stopItem);
            menu.Items.Add(new ToolStripSeparator());
            _autostartItem = new ToolStripMenuItem("Start with Windows", null, (s, e) => ToggleAutostart())
            {
                Checked = Autostart.IsEnabled()
            };
            menu.Items.Add(_autostartItem);
            menu.Items.Add("Quit", null, (s, e) => Quit());

            _icon = new NotifyIcon
            {
                Icon = AppIcon.Create(),
                Text = Config.AppName,
                Visible = true,
                ContextMenuStrip = menu
            };
            _icon.DoubleClick += (s, e) => PickMatch();

            TryConnect(false);
        }

        private bool TryConnect(bool loud)
        {
            try
            {
                _gs.Connect();
                _gs.RegisterGame();
                return true;
            }
            catch (Exception ex)
            {
                if (loud)
                    MessageBox.Show("SteelSeries GG is not running.\n\n" + ex.Message,
                        Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    _icon.ShowBalloonTip(5000, Config.AppName,
                        "SteelSeries GG not detected. Start it, then pick a match.", ToolTipIcon.Warning);
                return false;
            }
        }

        private void PickMatch()
        {
            if (!TryConnect(true)) return;

            using (var f = new MatchPickerForm(_espn))
            {
                if (f.ShowDialog() != DialogResult.OK) return;
                StartTracking(f.SelectedEventId);
            }
        }

        private void StartTracking(string eventId)
        {
            if (_tracker == null)
            {
                _tracker = new ScoreTracker(_gs, _espn);
                _tracker.Status += OnStatus;
            }
            _tracker.Start(eventId);

            _stopItem.Enabled = true;
            SetTooltip(Config.AppName + " - tracking");
            _icon.ShowBalloonTip(3000, Config.AppName, "Tracking started.", ToolTipIcon.Info);
        }

        private void StopTracking()
        {
            if (_tracker != null) _tracker.Stop();
            _stopItem.Enabled = false;
            SetTooltip(Config.AppName);
        }

        private void OnStatus(string s)
        {
            // Called from the tracker thread; marshal to the UI thread.
            if (_marshal.IsHandleCreated)
            {
                try { _marshal.BeginInvoke((Action)(() => SetTooltip(Config.AppName + " - " + s))); }
                catch { /* shutting down */ }
            }
        }

        private void SetTooltip(string s)
        {
            _icon.Text = s.Length > 63 ? s.Substring(0, 63) : s;
        }

        private void ToggleAutostart()
        {
            var now = !Autostart.IsEnabled();
            Autostart.Set(now);
            _autostartItem.Checked = now;
        }

        private void Quit()
        {
            if (_tracker != null) _tracker.Stop();
            _icon.Visible = false;
            _icon.Dispose();
            ExitThread();
        }
    }
}
