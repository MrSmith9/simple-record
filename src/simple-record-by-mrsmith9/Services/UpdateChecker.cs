using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
        // if you ever rename it again, update this to match.
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
        /// Looks up the latest GitHub release and returns its details if it
        /// is newer than <paramref name="currentVersion"/>. Returns null if
        /// there's no update, no internet connection, GitHub can't be
        /// reached, or no release has been published yet - all of those are
        /// treated the same way: quietly do nothing.
        /// </summary>
        public static async Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion)
        {
            try
            {
                string url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
                using HttpResponseMessage response = await Http.GetAsync(url).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // Expected (404) before any release has been published
                    // yet, or if GitHub is briefly unavailable.
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("tag_name", out JsonElement tagElement))
                {
                    return null;
                }

                string? tag = tagElement.GetString();
                if (string.IsNullOrWhiteSpace(tag))
                {
                    return null;
                }

                // Release tags are expected to look like "v0.3.0" - strip the
                // leading "v" so it can be parsed as a version number.
                string versionText = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag[1..] : tag;

                if (!Version.TryParse(versionText, out Version? latestVersion))
                {
                    return null;
                }

                if (latestVersion <= currentVersion)
                {
                    // Already up to date (or somehow newer, e.g. a dev build
                    // running ahead of the last published release).
                    return null;
                }

                string releaseUrl = doc.RootElement.TryGetProperty("html_url", out JsonElement urlElement)
                    ? urlElement.GetString() ?? $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases"
                    : $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases";

                return new UpdateInfo
                {
                    VersionText = versionText,
                    ReleasePageUrl = releaseUrl
                };
            }
            catch
            {
                // Any failure here (no internet, DNS failure, timeout,
                // unexpected response shape) should never crash the app or
                // show an alarming error - checking for an update is a
                // "nice to have", not something the app depends on to work.
                return null;
            }
        }
    }
}
