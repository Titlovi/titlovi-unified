using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Titlovi.Common
{
    internal sealed class TitloviApiClient : ITitloviApiClient
    {
        private sealed class SearchCacheEntry
        {
            public TitloviSearchPage Value { get; set; }
            public DateTime ExpiresUtc { get; set; }
        }

        private static readonly SemaphoreSlim SearchGate = new SemaphoreSlim(1, 1);
        private static readonly object SearchCacheSync = new object();
        private static readonly Dictionary<string, SearchCacheEntry> SearchCache = new Dictionary<string, SearchCacheEntry>(StringComparer.Ordinal);
        private static DateTime _nextSearchUtc = DateTime.MinValue;
        private static DateTime _searchCooldownUntilUtc = DateTime.MinValue;
        private static readonly TimeSpan SearchInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan RateLimitCooldown = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromMinutes(15);
        private const string KodiBase = "https://kodi.titlovi.com/api/subtitles/";
        private const string DownloadBase = "https://titlovi.com/download";
        private readonly ITitloviJsonDeserializer _json;
        private readonly HttpClient _http;
        private readonly HttpClient _downloadHttp;

        public TitloviApiClient(ITitloviJsonDeserializer json)
        {
            _json = json ?? throw new ArgumentNullException(nameof(json));
            _http = new HttpClient();
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");
            // Inherited from the upstream open-source client. Authentication is still
            // performed with the user's own Titlovi.com account/API access.
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-App", "titlovi-jellyfin");

            // Download endpoint: imitate the legacy official Kodi add-on as closely
            // as practical.  Kodi uses Python requests with a fresh GET, no cookies,
            // no API token and no X-App header.  The peculiar User-Agent value below
            // is intentional: the Kodi source literally includes the "User-Agent="
            // prefix inside the header value.
            var downloadHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _downloadHttp = new HttpClient(downloadHandler);
            _downloadHttp.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "User-Agent=Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.3)                              Gecko/20100401 Firefox/3.6.3 ( .NET CLR 3.5.30729)");
            _downloadHttp.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "www.titlovi.com");
            _downloadHttp.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            _downloadHttp.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            _downloadHttp.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
        }

        public async Task<TitloviToken> GetTokenAsync(string username, string password, CancellationToken cancellationToken)
        {
            try
            {
                TitloviDiagnostics.Debug("API token request started. Credentials are intentionally not logged.");
                var url = KodiBase + "gettoken?username=" + Esc(username) + "&password=" + Esc(password);
                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
                    using (var response = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false))
                    {
                        TitloviDiagnostics.Debug("API token response HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase + ".");
                        response.EnsureSuccessStatusCode();
                        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var token = _json.Deserialize<TitloviToken>(json);
                        return token;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                TitloviDiagnostics.Log("API token request FAILED: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
        }

        public async Task<TitloviSearchPage> SearchAsync(
            string query,
            TitloviSubtitleType type,
            IEnumerable<int> seasons,
            IEnumerable<int> episodes,
            string imdbId,
            int page,
            string language,
            TitloviToken token,
            CancellationToken cancellationToken)
        {
            var seasonList = seasons == null ? string.Empty : string.Join(",", seasons.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            var episodeList = episodes == null ? string.Empty : string.Join(",", episodes.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            TitloviDiagnostics.Debug(
                "API search requested: type=" + type
                + ", query='" + (query ?? string.Empty) + "'"
                + ", imdbId='" + (imdbId ?? string.Empty) + "'"
                + ", seasons='" + seasonList + "'"
                + ", episodes='" + episodeList + "'"
                + ", page=" + page
                + ", language='" + (language ?? string.Empty) + "'. Token/user id are not logged.");

            var cacheKey = string.Join("|", new[]
            {
                ((int)type).ToString(CultureInfo.InvariantCulture),
                query ?? string.Empty, imdbId ?? string.Empty, seasonList, episodeList,
                page.ToString(CultureInfo.InvariantCulture), language ?? string.Empty
            });

            lock (SearchCacheSync)
            {
                SearchCacheEntry cached;
                if (SearchCache.TryGetValue(cacheKey, out cached))
                {
                    if (cached.ExpiresUtc > DateTime.UtcNow)
                    {
                        TitloviDiagnostics.Debug("API search cache HIT before queue wait.");
                        return cached.Value;
                    }
                    TitloviDiagnostics.Debug("API search cache entry expired before queue wait.");
                    SearchCache.Remove(cacheKey);
                }
            }

            TitloviDiagnostics.Debug("API search waiting for serialized search gate.");
            await SearchGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            TitloviDiagnostics.Debug("API search acquired serialized search gate.");
            var apiAttempted = false;
            try
            {
                // Another request may have populated the same cache entry while we
                // were waiting for the serialized search gate, so check once more.
                lock (SearchCacheSync)
                {
                    SearchCacheEntry cached;
                    if (SearchCache.TryGetValue(cacheKey, out cached))
                    {
                        if (cached.ExpiresUtc > DateTime.UtcNow)
                        {
                            TitloviDiagnostics.Debug("API search cache HIT after queue wait.");
                            return cached.Value;
                        }
                        TitloviDiagnostics.Debug("API search cache entry expired after queue wait.");
                        SearchCache.Remove(cacheKey);
                    }
                }

                var now = DateTime.UtcNow;
                var waitUntilUtc = _nextSearchUtc > _searchCooldownUntilUtc ? _nextSearchUtc : _searchCooldownUntilUtc;
                if (now < waitUntilUtc)
                {
                    var wait = waitUntilUtc - now;
                    TitloviDiagnostics.Debug("API search throttled locally for " + Math.Ceiling(wait.TotalMilliseconds) + " ms.");
                    await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
                }
                apiAttempted = true;
                var parts = new List<string>();
                Add(parts, "query", query);
                Add(parts, "type", ((int)type).ToString(CultureInfo.InvariantCulture));
                if (seasons != null)
                    foreach (var season in seasons) Add(parts, "season", season.ToString(CultureInfo.InvariantCulture));
                if (episodes != null)
                    foreach (var episode in episodes) Add(parts, "episode", episode.ToString(CultureInfo.InvariantCulture));
                Add(parts, "imdbId", imdbId);
                Add(parts, "pg", page.ToString(CultureInfo.InvariantCulture));
                Add(parts, "ignoreLangAndEpisode", "false");
                Add(parts, "lang", language);
                Add(parts, "token", token.Token);
                Add(parts, "userid", token.UserId.ToString(CultureInfo.InvariantCulture));

                var url = KodiBase + "search?" + string.Join("&", parts);
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false))
                {
                    TitloviDiagnostics.Debug(
                        "API search response HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase
                        + ", contentType=" + (response.Content.Headers.ContentType == null ? "<none>" : response.Content.Headers.ContentType.ToString()) + ".");

                    if ((int)response.StatusCode == 429)
                    {
                        _searchCooldownUntilUtc = DateTime.UtcNow.Add(RateLimitCooldown);
                        _nextSearchUtc = _searchCooldownUntilUtc;
                        TitloviDiagnostics.Log("API search entered local 90s cooldown after HTTP 429.");
                        return EmptySearchPage();
                    }

                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = _json.Deserialize<TitloviSearchPage>(json);
                    TitloviDiagnostics.Debug(
                        "API search payload parsed: resultsFound=" + (result == null ? 0 : result.ResultsFound)
                        + ", currentPage=" + (result == null ? 0 : result.CurrentPage)
                        + ", pagesAvailable=" + (result == null ? 0 : result.PagesAvailable)
                        + ", returned=" + (result?.SubtitleResults == null ? 0 : result.SubtitleResults.Count) + ".");

                    if (result != null)
                    {
                        lock (SearchCacheSync)
                        {
                            SearchCache[cacheKey] = new SearchCacheEntry
                            {
                                Value = result,
                                ExpiresUtc = DateTime.UtcNow.Add(SearchCacheDuration)
                            };
                        }
                    }
                    return result;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                TitloviDiagnostics.Log("API search FAILED: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                // Space actual API attempts apart. Requests wait for their slot rather
                // than being discarded, so legitimate manual searches are preserved.
                if (apiAttempted && DateTime.UtcNow >= _searchCooldownUntilUtc)
                    _nextSearchUtc = DateTime.UtcNow.Add(SearchInterval);
                SearchGate.Release();
            }
        }

        private static TitloviSearchPage EmptySearchPage()
        {
            return new TitloviSearchPage
            {
                ResultsFound = 0,
                PagesAvailable = 0,
                CurrentPage = 0,
                SubtitleResults = new List<TitloviSubtitle>()
            };
        }

        public async Task<byte[]> DownloadAsync(int mediaId, TitloviSubtitleType type, CancellationToken cancellationToken)
        {
            try
            {
                TitloviDiagnostics.Debug("API download started: mediaId=" + mediaId + ", type=" + type + ".");
                // Match the legacy official Kodi add-on URL exactly.
                var url = DownloadBase + "/?type=" + ((int)type).ToString(CultureInfo.InvariantCulture)
                    + "&mediaid=" + mediaId.ToString(CultureInfo.InvariantCulture);

                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Version = HttpVersion.Version11;
                    using (var response = await _downloadHttp.SendAsync(
                        req,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false))
                    {
                        TitloviDiagnostics.Debug(
                            "API download response HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase
                            + ", contentType=" + (response.Content.Headers.ContentType == null ? "<none>" : response.Content.Headers.ContentType.ToString())
                            + ", contentLength=" + (response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value.ToString(CultureInfo.InvariantCulture) : "<unknown>")
                            + ", server=" + (response.Headers.Server == null ? "<none>" : response.Headers.Server.ToString()) + ".");

                        if (!response.IsSuccessStatusCode)
                        {
                            TitloviDiagnostics.Log("API download response HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase
                                + ", contentType=" + (response.Content.Headers.ContentType == null ? "<none>" : response.Content.Headers.ContentType.ToString())
                                + ", server=" + (response.Headers.Server == null ? "<none>" : response.Headers.Server.ToString()));

                            string failureBody = string.Empty;
                            try
                            {
                                failureBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                if (failureBody.Length > 500)
                                    failureBody = failureBody.Substring(0, 500);
                                failureBody = failureBody.Replace("\r", " ").Replace("\n", " ");
                            }
                            catch
                            {
                                // Best-effort failure diagnostics only.
                            }

                            if (!string.IsNullOrWhiteSpace(failureBody))
                                TitloviDiagnostics.Debug("API download failure body (first 500 chars): " + failureBody);
                        }

                        response.EnsureSuccessStatusCode();
                        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        TitloviDiagnostics.Debug("API download completed successfully: " + bytes.Length + " bytes received.");
                        return bytes;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                TitloviDiagnostics.Log("API download FAILED: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
        }

        private static void Add(ICollection<string> parts, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add(Esc(name) + "=" + Esc(value));
        }

        private static string Esc(string value) => Uri.EscapeDataString(value ?? string.Empty);
    }
}
