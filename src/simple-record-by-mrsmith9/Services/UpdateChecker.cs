using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleRecord.Services
{
    /// <summary>
    /// A newer version found on GitHub, with enough information to show the
    /// user and send them to the right place to get it.
    /// </summary>
    public class UpdateInfo
    {
        public required string VersionText { get; init; }
        public required string ReleasePageUrl { get; init; }
    }

    /// <summary>
    /// Checks GitHub Releases for a newer version of the app than the one
    /// currently running. This never throws - if there's no internet, GitHub
    /// is unreachable, or no release has been published yet, it just reports
    /// "no update found" instead of bothering the user with an error.
    /// </summary>
    public static class UpdateChecker
    {
        // Your GitHub repo. Must exactly match your actual GitHub
        // repository's name (https://github.com/MrSmith9/simple-record) -
        // if you ever rename it again, update this to match. The repo also
        // has to be set to Public on GitHub (Settings > Danger Zone > Change
        // visibility) - a private repo looks exactly like "no release
        // published yet" to this check, since it isn't logged in as you.
        private const string GitHubOwner = "MrSmith9";
        private const string GitHubRepo = "simple-record";

        private static readonly HttpClient Http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                // Don't let a slow or stuck connection delay the app - give
                // up quickly and just skip the check instead.
                Timeout = TimeSpan.FromSeconds(6)
            };
            // GitHub's API rejects requests that have no User-Agent header.
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SimpleRecord", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        /// <summary>
        /// The version number of the Simple Record build currently running,
        /// read from the assembly - shared so MainWindow's automatic
        /// startup check and Settings' manual "Check for Updates" button
        /// always compare against (and display) the exact same number.
        /// </summary>
        public static Version GetCurrentVersion() =>
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

        /// <summary>Formats a version as "Major.Minor.Build" - this app never sets the 4th ("Revision") part, so that's left out.</summary>
        public static string FormatVersion(Version version) => $"{version.Major}.{version.Minor}.{version.Build}";

        /// <summary>
        /// Looks up the latest GitHub release and returns its details if it
        /// is newer than <paramref name="currentVersion"/>. Returns null if
        /// there's no update, no internet connection, GitHub can't be
        /// reached, or no release has been published yet - all of those are
        /// treated the same way: quietly do nothing. Used for the automatic
        /// check at startup, which is only ever meant to be a quiet "nice to
        /// have" - it should never bother the user with an error, and never
        /// needs to explain *why* nothing was found.
        /// </summary>
        public static async Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion)
        {
            (bool _, UpdateInfo? update) = await CheckForUpdateCoreAsync(currentVersion).ConfigureAwait(false);
            return update;
        }

        /// <summary>
        /// Same check as <see cref="CheckForUpdateAsync"/>, but also reports
        /// whether the check itself actually succeeded in reaching GitHub
        /// and reading a response, separately from whether an update was
        /// found. Used by the manual "Check for Updates" button in Settings
        /// - unlike the quiet startup check, someone who presses that button
        /// is asking a direct question and deserves an honest answer: saying
        /// "you're using the latest version" when the check couldn't even
        /// reach GitHub (no internet, GitHub down, or - as happened once
        /// already - the repository being set to Private) would be
        /// misleading rather than reassuring.
        /// </summary>
        public static Task<(bool Succeeded, UpdateInfo? Update)> CheckForUpdateWithStatusAsync(Version currentVersion) =>
            CheckForUpdateCoreAsync(currentVersion);

        private static async Task<(bool Succeeded, UpdateInfo? Update)> CheckForUpdateCoreAsync(Version currentVersion)
        {
            try
            {
                string url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
                using HttpResponseMessage response = await Http.GetAsync(url).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // Expected (404) before any release has been published
                    // yet, if the repo is set to Private, or if GitHub is
                    // briefly unavailable - this check can't tell which of
                    // those it is from the response alone, so it's reported
                    // as "couldn't check" rather than guessed at.
                    return (false, null);
                }

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("tag_name", out JsonElement tagElement))
                {
                    return (false, null);
                }

                string? tag = tagElement.GetString();
                if (string.IsNullOrWhiteSpace(tag))
                {
                    return (false, null);
                }

                // Release tags are expected to look like "v0.3.0" - strip the
                // leading "v" so it can be parsed as a version number.
                string versionText = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag[1..] : tag;

                if (!Version.TryParse(versionText, out Version? latestVersion))
                {
                    return (false, null);
                }

                if (latestVersion <= currentVersion)
                {
                    // Successfully checked, genuinely already up to date (or
                    // somehow newer, e.g. a dev build running ahead of the
                    // last published release).
                    return (true, null);
                }

                string releaseUrl = doc.RootElement.TryGetProperty("html_url", out JsonElement urlElement)
                    ? urlElement.GetString() ?? $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases"
                    : $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases";

                return (true, new UpdateInfo
                {
                    VersionText = versionText,
                    ReleasePageUrl = releaseUrl
                });
            }
            catch
            {
                // Any failure here (no internet, DNS failure, timeout,
                // unexpected response shape) should never crash the app or
                // show an alarming error - checking for an update is a
                // "nice to have", not something the app depends on to work.
                return (false, null);
            }
        }
    }
}
