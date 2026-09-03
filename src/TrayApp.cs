using System;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

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
        private readonly Timer _updateTimer;
        private ScoreTracker _tracker;
        private bool _picking;
        private bool _updating;
        private UpdateInfo _pending;

        public TrayApp(bool openPicker)
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
            menu.Items.Add("Check for updates...", null, (s, e) => CheckForUpdates(true));
            menu.Items.Add("Quit", null, (s, e) => Quit());

            _icon = new NotifyIcon
            {
                Icon = AppIcon.Create(),
                Text = Config.AppName,
                Visible = true,
                ContextMenuStrip = menu
            };
            _icon.DoubleClick += (s, e) => PickMatch();

            _icon.BalloonTipClicked += (s, e) => OfferUpdate();

            ListenForWake();

            if (openPicker)
                _marshal.BeginInvoke((Action)PickMatch); // wait for the message loop
            else
                TryConnect(false);

            _updateTimer = new Timer { Interval = (int)TimeSpan.FromHours(6).TotalMilliseconds };
            _updateTimer.Tick += (s, e) => CheckForUpdates(false);
            _updateTimer.Start();
            CheckForUpdates(false);
        }

        // A second launch of the exe signals this event; show the picker for it.
        private void ListenForWake()
        {
            var wake = new EventWaitHandle(false, EventResetMode.AutoReset, Program.WakeEventName);
            var t = new Thread(() =>
            {
                while (true)
                {
                    wake.WaitOne();
                    try { _marshal.BeginInvoke((Action)PickMatch); }
                    catch { return; /* shutting down */ }
                }
            });
            t.IsBackground = true;
            t.Start();
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
            if (_picking) return;
            if (!TryConnect(true)) return;

            _picking = true;
            try
            {
                using (var f = new MatchPickerForm(_espn))
                {
                    f.Shown += (s, e) => f.Activate();
                    if (f.ShowDialog() != DialogResult.OK) return;
                    StartTracking(f.SelectedEventId);
                }
            }
            finally { _picking = false; }
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

        private void CheckForUpdates(bool loud)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                UpdateInfo info = null;
                string error = null;
                try { info = Updater.Check(); }
                catch (Exception ex) { error = ex.Message; }

                try { _marshal.BeginInvoke((Action)(() => OnUpdateChecked(info, error, loud))); }
                catch { /* shutting down */ }
            });
        }

        private void OnUpdateChecked(UpdateInfo info, string error, bool loud)
        {
            _pending = info;

            if (info == null)
            {
                // A silent check stays silent, whether it failed or found nothing.
                if (!loud) return;
                if (error != null)
                    MessageBox.Show("Could not check for updates.\n\n" + error,
                        Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("You are on the latest version (" + Updater.Current + ").",
                        Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (loud)
                OfferUpdate();
            else
                _icon.ShowBalloonTip(10000, Config.AppName,
                    "Update available: v" + info.Version + " - click to install.", ToolTipIcon.Info);
        }

        private void OfferUpdate()
        {
            var info = _pending;
            if (info == null || _updating) return;

            var msg = "A new version is available.\n\n"
                      + "Installed:  " + Updater.Current + "\n"
                      + "Latest:     " + info.Version + "\n\n"
                      + (info.SetupUrl == null
                          ? "Open the download page?"
                          : "Update now? The app will close, install, and start again.");

            if (MessageBox.Show(msg, Config.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes) return;

            if (info.SetupUrl == null) { Updater.OpenReleasesPage(info.PageUrl); return; }

            _updating = true;
            SetTooltip(Config.AppName + " - downloading update");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string path = null;
                string error = null;
                try { path = Updater.Download(info); }
                catch (Exception ex) { error = ex.Message; }

                try { _marshal.BeginInvoke((Action)(() => OnDownloaded(path, error))); }
                catch { /* shutting down */ }
            });
        }

        private void OnDownloaded(string path, string error)
        {
            if (path == null)
            {
                _updating = false;
                SetTooltip(Config.AppName);
                MessageBox.Show("Download failed, opening the download page instead.\n\n" + error,
                    Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Updater.OpenReleasesPage(_pending == null ? null : _pending.PageUrl);
                return;
            }

            try
            {
                Updater.Install(path);
                Quit(); // let go of the exe so the installer can replace it
            }
            catch (Exception ex)
            {
                _updating = false;
                SetTooltip(Config.AppName);
                MessageBox.Show("Could not start the installer.\n\n" + ex.Message,
                    Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Quit()
        {
            if (_updateTimer != null) _updateTimer.Stop();
            if (_tracker != null) _tracker.Stop();
            _icon.Visible = false;
            _icon.Dispose();
            ExitThread();
        }
    }
}
