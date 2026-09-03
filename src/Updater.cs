using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace OledLiveScore
{
    internal sealed class UpdateInfo
    {
        public Version Version;
        public string Tag;
        public string SetupUrl;
        public string PageUrl;
        public string Notes;
    }

    // Checks GitHub Releases for a newer build, then downloads and runs the installer.
    internal static class Updater
    {
        private const string Repo = "kerim42407/steelseries-oled-livescore";
        private const string LatestApi = "https://api.github.com/repos/" + Repo + "/releases/latest";
        public const string ReleasesPage = "https://github.com/" + Repo + "/releases/latest";

        private const string SetupAsset = "OledLiveScore-Setup.exe";

        static Updater()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
            catch { /* older platforms */ }
        }

        public static Version Current
        {
            get { return Trim(Assembly.GetExecutingAssembly().GetName().Version); }
        }

        // Compare on major.minor.patch only; the assembly's 4th part is always 0.
        private static Version Trim(Version v)
        {
            return new Version(v.Major, v.Minor, v.Build < 0 ? 0 : v.Build);
        }

        private static Version ParseTag(string tag)
        {
            var s = (tag ?? "").Trim().TrimStart('v', 'V');
            Version v;
            return Version.TryParse(s, out v) ? Trim(v) : null;
        }

        // Returns null when we are up to date; throws when the check itself fails.
        public static UpdateInfo Check()
        {
            string body;
            using (var wc = new TimedWebClient { TimeoutMs = 15000 })
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers[HttpRequestHeader.UserAgent] = Config.AppName;
                wc.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                body = wc.DownloadString(LatestApi);
            }

            var rel = Json.Parse(body);
            if (Json.Bool(Json.Get(rel, "draft")) || Json.Bool(Json.Get(rel, "prerelease"))) return null;

            var tag = Json.Str(Json.Get(rel, "tag_name"));
            var latest = ParseTag(tag);
            if (latest == null || latest <= Current) return null;

            var asset = Json.Arr(Json.Get(rel, "assets"))
                .FirstOrDefault(a => string.Equals(Json.Str(Json.Get(a, "name")), SetupAsset,
                    StringComparison.OrdinalIgnoreCase));

            return new UpdateInfo
            {
                Version = latest,
                Tag = tag,
                SetupUrl = asset == null ? null : Json.Str(Json.Get(asset, "browser_download_url")),
                PageUrl = Json.Str(Json.Get(rel, "html_url")),
                Notes = Json.Str(Json.Get(rel, "body"))
            };
        }

        // Downloads the installer to temp. Returns the local path.
        public static string Download(UpdateInfo info)
        {
            var dir = Path.Combine(Path.GetTempPath(), Config.AppName + "-update");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, SetupAsset);

            using (var wc = new TimedWebClient { TimeoutMs = 120000 })
            {
                wc.Headers[HttpRequestHeader.UserAgent] = Config.AppName;
                wc.DownloadFile(info.SetupUrl, path);
            }
            return path;
        }

        // Starts the silent installer. The caller must exit right away so the
        // exe is not locked; the installer relaunches us when it is done.
        public static void Install(string setupPath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = setupPath,
                Arguments = "/SILENT /NOCANCEL /SP- /CLOSEAPPLICATIONS",
                UseShellExecute = true
            });
        }

        public static void OpenReleasesPage(string url)
        {
            try { Process.Start(string.IsNullOrEmpty(url) ? ReleasesPage : url); }
            catch { /* no browser */ }
        }
    }
}
