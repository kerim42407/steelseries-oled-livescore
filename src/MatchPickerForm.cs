using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OledLiveScore
{
    // Pick a league to list its matches, or search a team. Then track a match.
    internal sealed class MatchPickerForm : Form
    {
        private readonly EspnClient _espn;
        private readonly ComboBox _league;
        private readonly TextBox _team;
        private readonly Button _search;
        private readonly ListBox _list;
        private readonly Button _track;
        private readonly Label _status;
        private List<Match> _matches = new List<Match>();

        public string SelectedEventId { get; private set; }

        public MatchPickerForm(EspnClient espn)
        {
            _espn = espn;

            Text = "OledLiveScore - pick a match";
            Icon = AppIcon.Create();
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(440, 330);

            var lblLeague = new Label { Text = "Choose a league or competition:", Left = 12, Top = 12, Width = 416 };
            _league = new ComboBox { Left = 12, Top = 34, Width = 416, DropDownStyle = ComboBoxStyle.DropDownList };
            _league.Items.Add("Select a league...");
            foreach (var lg in LeagueCatalog.All) _league.Items.Add(lg.Name);
            _league.SelectedIndex = 0;

            var lblTeam = new Label { Text = "...or search a team:", Left = 12, Top = 68, Width = 416 };
            _team = new TextBox { Left = 12, Top = 90, Width = 320 };
            _search = new Button { Text = "Search", Left = 340, Top = 88, Width = 88 };
            var lblHint = new Label
            {
                Text = "e.g. Arsenal, Fenerbahce, Real Madrid",
                Left = 12, Top = 116, Width = 416, ForeColor = Color.Gray
            };

            _list = new ListBox { Left = 12, Top = 138, Width = 416, Height = 148, IntegralHeight = false };
            _track = new Button { Text = "Track", Left = 340, Top = 294, Width = 88, Enabled = false };
            _status = new Label { Left = 12, Top = 298, Width = 320, Text = "" };

            _league.SelectedIndexChanged += (s, e) => LoadLeague();
            _search.Click += (s, e) => SearchTeam();
            _team.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SearchTeam(); }
            };
            _list.DoubleClick += (s, e) => Track();
            _list.SelectedIndexChanged += (s, e) => { _track.Enabled = _list.SelectedIndex >= 0; };
            _track.Click += (s, e) => Track();

            Controls.Add(lblLeague);
            Controls.Add(_league);
            Controls.Add(lblTeam);
            Controls.Add(_team);
            Controls.Add(_search);
            Controls.Add(lblHint);
            Controls.Add(_list);
            Controls.Add(_track);
            Controls.Add(_status);
        }

        private void LoadLeague()
        {
            if (_league.SelectedIndex <= 0) return;
            var lg = LeagueCatalog.All[_league.SelectedIndex - 1];
            _team.Clear();
            LoadAsync(() => _espn.GetLeagueMatches(lg.Slug), lg.Name);
        }

        private void SearchTeam()
        {
            var q = _team.Text.Trim();
            if (q.Length == 0) return;
            _league.SelectedIndex = 0;
            LoadAsync(() =>
            {
                var team = _espn.FindTeam(q);
                if (team == null) throw new Exception("Team not found: " + q);
                return _espn.GetTeamMatches(team);
            }, q);
        }

        private void LoadAsync(Func<List<Match>> fetch, string label)
        {
            _status.Text = "Loading...";
            _search.Enabled = false;
            _league.Enabled = false;
            _track.Enabled = false;
            _list.Items.Clear();
            _matches.Clear();

            Task.Run(() =>
            {
                try
                {
                    var list = fetch() ?? new List<Match>();
                    OnUi(() =>
                    {
                        _search.Enabled = true;
                        _league.Enabled = true;
                        if (list.Count == 0)
                        {
                            _status.Text = "No matches found for " + label + ".";
                            return;
                        }
                        _matches = list;
                        foreach (var m in list) _list.Items.Add(Describe(m));
                        _status.Text = label + " - " + list.Count + " match(es)";
                    });
                }
                catch (Exception ex)
                {
                    OnUi(() =>
                    {
                        _search.Enabled = true;
                        _league.Enabled = true;
                        _status.Text = ex.Message;
                    });
                }
            });
        }

        private static string Describe(Match m)
        {
            return string.Format("{0}  {1} {2}-{3} {4}   {5}",
                m.When.ToString("dd MMM HH:mm", CultureInfo.InvariantCulture),
                m.Home, m.HScore, m.AScore, m.Away, m.Status);
        }

        private void Track()
        {
            if (_list.SelectedIndex < 0 || _list.SelectedIndex >= _matches.Count) return;
            SelectedEventId = _matches[_list.SelectedIndex].Id;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnUi(Action action)
        {
            if (IsDisposed) return;
            try { BeginInvoke(action); }
            catch { /* form closed */ }
        }
    }
}
