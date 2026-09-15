using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Titlovi.Common
{
    public sealed class TitloviEngine
    {
        private static readonly SemaphoreSlim TokenLock = new SemaphoreSlim(1, 1);
        // Based on the episode matcher used by Titlovi.com's official API controller.
        // The official logic prefers matches that contain both season and episode,
        // then falls back to the strongest episode-only match.  We extend it with
        // Serbian "Epizoda" / "Ep" spellings and keep a separate leading-number
        // parser for season packs named 001, 002, 101, etc.
        private static readonly Regex EpisodeCandidateRegex = new Regex(
            @"(?:S|Season)[\-. ]*0*(?<season>\d{1,2})[\-. ]*(?:E|Episode)[\-. ]*0*(?<episode>\d{1,3})|0*(?<season>\d{1,2})[\-. ]*x[\-. ]*0*(?<episode>\d{1,3})|(?:E|Ep|Episode|Epizoda)[\-. ]*0*(?<episode>\d{1,3})|Part[\-. ]*0*(?<episode>\d{1,3})|^(?<episode>\d{1,2})\D|\D(?<episode>\d{1,2})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex LeadingNumberRegex = new Regex(
            @"^[\s._\-\[\(]*(?<number>\d{1,4})(?=[\s._\-\]\)]|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Additional real-world pack conventions observed in older Titlovi.com uploads.
        // These are deliberately separate from the official regex so that broad legacy
        // matching cannot weaken the high-confidence SxxExx / NxM matches above.
        private static readonly Regex SeasonTokenRegex = new Regex(
            @"(?:^|[^a-z0-9])(?:s(?:eason)?|sezona)[\s._\-]*0*(?<season>\d{1,2})(?=$|[^0-9])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex EpisodeTokenRegex = new Regex(
            @"(?:^|[^a-z0-9])(?:e|ep|episode|epizoda)[\s._\-]*0*(?<episode>\d{1,3})(?=$|[^0-9])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex EpisodeListRegex = new Regex(
            @"(?:^|[^a-z0-9])(?:e|ep|episode|epizoda)[\s._\-]*(?<list>\d{1,3}(?:\s*[,;+&/]\s*\d{1,3})+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RomanSeasonNumberRegex = new Regex(
            @"(?:^|[\s._\-])(?<roman>xii|xi|x|ix|viii|vii|vi|v|iv|iii|ii|i)[\s._\-]+0*(?<number>\d{1,4})(?=$|[\s._\-])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CompactEpisodeBundleRegex = new Regex(
            @"(?<!\d)(?<bundle>[1-9]{2,4})(?!\d)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ITitloviApiClient _api;
        private readonly ITitloviConfigurationProvider _configuration;

        public TitloviEngine(ITitloviJsonDeserializer jsonDeserializer, ITitloviConfigurationProvider configuration)
        {
            _api = new TitloviApiClient(jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer)));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // Test seam only: production hosts continue to use the public constructor above.
        // Keeping the transport behind this tiny internal interface lets the regression
        // suite exercise the complete Common search/download flow without live API calls.
        internal TitloviEngine(ITitloviApiClient api, ITitloviConfigurationProvider configuration)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string ProviderName => "Titlovi.com (Unofficial)";

        public async Task<IEnumerable<CoreRemoteSubtitleInfo>> Search(
            CoreSubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            TitloviDiagnostics.Debug(
                "Search requested: contentType=" + request.ContentType
                + ", name='" + (request.Name ?? string.Empty) + "'"
                + ", series='" + (request.SeriesName ?? string.Empty) + "'"
                + ", mediaPath='" + (request.MediaPath ?? string.Empty) + "'"
                + ", season=" + request.ParentIndexNumber.GetValueOrDefault()
                + ", episode=" + request.IndexNumber.GetValueOrDefault()
                + ", episodeEnd=" + request.IndexNumberEnd.GetValueOrDefault()
                + ", language='" + (request.Language ?? string.Empty) + "'.");

            if (request.ProviderIds != null)
            {
                string episodeImdbForLog;
                request.ProviderIds.TryGetValue("Imdb", out episodeImdbForLog);
                TitloviDiagnostics.Debug("Episode/item IMDb id from ProviderIds='" + (episodeImdbForLog ?? string.Empty) + "'.");
            }
            if (request.SeriesProviderIds != null)
            {
                string seriesImdbForLog;
                request.SeriesProviderIds.TryGetValue("Imdb", out seriesImdbForLog);
                TitloviDiagnostics.Debug("Parent series IMDb id from SeriesProviderIds='" + (seriesImdbForLog ?? string.Empty) + "'.");
            }

            var config = _configuration;
            if (config == null || string.IsNullOrWhiteSpace(config.Username) || string.IsNullOrWhiteSpace(config.Password))
            {
                TitloviDiagnostics.Debug("Subtitle search skipped because Titlovi.com credentials are not configured.");
                return Array.Empty<CoreRemoteSubtitleInfo>();
            }

            var searchLanguages = LanguageMap.GetSearchLanguages(
                request.Language,
                config.IncludeBshAlternativeLanguages);
            if (searchLanguages.Count == 0)
            {
                TitloviDiagnostics.Debug("Search skipped: requested language has no Titlovi.com mapping.");
                return Array.Empty<CoreRemoteSubtitleInfo>();
            }

            TitloviDiagnostics.Debug(
                "Provider languages for this search: " + string.Join(" -> ", searchLanguages)
                + ". B/S/H alternatives=" + (config.IncludeBshAlternativeLanguages ? "enabled" : "disabled") + ".");

            var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
            var type = request.ContentType == TitloviContentType.Episode
                ? TitloviSubtitleType.Episode
                : TitloviSubtitleType.Movie;

            string imdbId = null;
            if (request.ProviderIds != null)
                request.ProviderIds.TryGetValue("Imdb", out imdbId);

            string seriesImdbId = null;
            if (request.SeriesProviderIds != null)
                request.SeriesProviderIds.TryGetValue("Imdb", out seriesImdbId);

            string query;
            string titleFallbackQuery = null;
            IEnumerable<int> seasons = null;
            IEnumerable<int> episodes = null;

            if (type == TitloviSubtitleType.Episode)
            {
                seasons = new[] { 0, request.ParentIndexNumber.GetValueOrDefault() };
                episodes = new[] { 0, request.IndexNumber.GetValueOrDefault() };

                // Some hosts expose provider ids of the parent series separately from
                // the episode's own ProviderIds. Prefer the series IMDb id because a
                // short title such as "Friends" can otherwise match unrelated shows.
                if (!string.IsNullOrWhiteSpace(seriesImdbId))
                {
                    query = seriesImdbId;
                    imdbId = seriesImdbId;
                    titleFallbackQuery = request.SeriesName;
                }
                else
                {
                    query = request.SeriesName;
                    imdbId = null;
                }
            }
            else
            {
                // Movies also prefer the item's IMDb id when the host supplies one.
                // Keep the title only as a fallback for older Titlovi.com records
                // that may not be linked to IMDb correctly.
                if (!string.IsNullOrWhiteSpace(imdbId))
                {
                    query = imdbId;
                    titleFallbackQuery = request.Name;
                }
                else
                {
                    query = request.Name;
                }
            }

            TitloviDiagnostics.Debug(
                "Search parameters resolved: type=" + type
                + ", query='" + (query ?? string.Empty) + "'"
                + ", imdbId='" + (imdbId ?? string.Empty) + "'"
                + ", seasons=" + (seasons == null ? "<none>" : string.Join(",", seasons))
                + ", episodes=" + (episodes == null ? "<none>" : string.Join(",", episodes)) + ".");

            var mergedById = new Dictionary<int, TitloviSubtitle>();
            for (var languagePriority = 0; languagePriority < searchLanguages.Count; languagePriority++)
            {
                var providerLanguage = searchLanguages[languagePriority];
                var languageResults = await SearchOneLanguageAsync(
                    query,
                    type,
                    seasons,
                    episodes,
                    imdbId,
                    titleFallbackQuery,
                    providerLanguage,
                    token,
                    cancellationToken).ConfigureAwait(false);

                foreach (var result in languageResults)
                {
                    // Titlovi.com can return a B/S/H subtitle whose actual language
                    // differs from the language used for this API request. Preserve
                    // the per-result language reported by the API whenever it maps
                    // to a supported Titlovi language. The requested language is only
                    // a fallback for legacy/empty/unknown API values.
                    var apiReportedLanguage = result.Lang;
                    var effectiveProviderLanguage = LanguageMap.ToProviderLanguage(apiReportedLanguage);
                    if (string.IsNullOrWhiteSpace(effectiveProviderLanguage))
                    {
                        effectiveProviderLanguage = providerLanguage;
                        TitloviDiagnostics.Debug(
                            "API result language '" + (apiReportedLanguage ?? string.Empty)
                            + "' is empty or unsupported; falling back to requested provider language '"
                            + providerLanguage + "' for mediaId=" + result.Id + ".");
                    }
                    else if (!effectiveProviderLanguage.Equals(providerLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        TitloviDiagnostics.Debug(
                            "API result mediaId=" + result.Id
                            + " reports actual language '" + effectiveProviderLanguage
                            + "' although the API search was requested as '" + providerLanguage
                            + "'; preserving the API-reported language.");
                    }

                    result.Lang = effectiveProviderLanguage;
                    result.LanguageCode = LanguageMap.FromProviderLanguage(effectiveProviderLanguage);
                    result.LanguagePriority = GetLanguagePriority(searchLanguages, effectiveProviderLanguage, languagePriority);
                    result.MatchScore = CalculateReleaseMatchScore(request.MediaPath, result.Release);

                    if (type == TitloviSubtitleType.Episode)
                    {
                        // Package records may report Season=0 / Episode=0. Internal
                        // metadata must always target the episode the host actually asked for.
                        result.Season = request.ParentIndexNumber.GetValueOrDefault();
                        result.Episode = request.IndexNumber.GetValueOrDefault();
                    }

                    TitloviSubtitle existing;
                    if (!mergedById.TryGetValue(result.Id, out existing)
                        || result.LanguagePriority < existing.LanguagePriority)
                    {
                        mergedById[result.Id] = result;
                    }
                }
            }

            var results = mergedById.Values.ToList();
            TitloviDiagnostics.Debug("Search completed: total merged results=" + results.Count + ".");
            foreach (var result in results)
            {
                TitloviDiagnostics.Debug(
                    "Search result: mediaId=" + result.Id
                    + ", title='" + (result.Title ?? string.Empty) + "'"
                    + ", language='" + (result.Lang ?? string.Empty) + "'"
                    + ", languagePriority=" + result.LanguagePriority
                    + ", bestMatchScore=" + result.MatchScore
                    + ", season=" + result.Season
                    + ", episode=" + result.Episode
                    + ", downloads=" + result.DownloadCount
                    + ", rating=" + result.Rating
                    + ", release='" + (result.Release ?? string.Empty) + "'.");
            }

            // Deterministic result order:
            //   1) language selected in the host, then B/S/H alternatives when enabled;
            //   2) best release match to the local media filename;
            //   3) download count;
            //   4) community rating.
            var showLanguageInResultName = config.IncludeBshAlternativeLanguages && searchLanguages.Count > 1;
            var remoteResults = results
                .OrderBy(x => x.LanguagePriority)
                .ThenByDescending(x => x.MatchScore)
                .ThenByDescending(x => x.DownloadCount)
                .ThenByDescending(x => x.Rating)
                .Select(x => ToRemoteInfo(
                    x,
                    request.Name,
                    request.IndexNumberEnd.GetValueOrDefault(),
                    showLanguageInResultName))
                .ToList();
            return remoteResults;
        }

        private static int GetLanguagePriority(
            IReadOnlyList<string> searchLanguages,
            string actualProviderLanguage,
            int fallbackPriority)
        {
            if (searchLanguages == null || string.IsNullOrWhiteSpace(actualProviderLanguage))
                return fallbackPriority;

            for (var i = 0; i < searchLanguages.Count; i++)
            {
                if (actualProviderLanguage.Equals(searchLanguages[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return fallbackPriority;
        }

        private async Task<List<TitloviSubtitle>> SearchOneLanguageAsync(
            string initialQuery,
            TitloviSubtitleType type,
            IEnumerable<int> seasons,
            IEnumerable<int> episodes,
            string initialImdbId,
            string titleFallbackQuery,
            string providerLanguage,
            TitloviToken token,
            CancellationToken cancellationToken)
        {
            var results = new List<TitloviSubtitle>();
            var activeQuery = initialQuery;
            var activeImdbId = initialImdbId;
            var page = 1;
            var pagesAvailable = 1;

            TitloviDiagnostics.Debug("Starting language search: providerLanguage='" + providerLanguage + "'.");
            do
            {
                var response = await _api.SearchAsync(
                    activeQuery,
                    type,
                    seasons,
                    episodes,
                    activeImdbId,
                    page,
                    providerLanguage,
                    token,
                    cancellationToken).ConfigureAwait(false);

                if (response?.SubtitleResults != null)
                    results.AddRange(response.SubtitleResults.Select(CloneSubtitle));

                TitloviDiagnostics.Debug(
                    "Search page " + page + " for '" + providerLanguage + "' completed: returned="
                    + (response?.SubtitleResults == null ? 0 : response.SubtitleResults.Count)
                    + ", pagesAvailable=" + (response == null ? 0 : response.PagesAvailable)
                    + ", accumulated=" + results.Count + ".");

                pagesAvailable = response == null ? page : Math.Max(page, response.PagesAvailable);
                page++;
            }
            while (page <= pagesAvailable && !cancellationToken.IsCancellationRequested);

            if (results.Count == 0
                && !string.IsNullOrWhiteSpace(titleFallbackQuery)
                && !string.IsNullOrWhiteSpace(activeImdbId))
            {
                var imdbUsed = activeImdbId;
                TitloviDiagnostics.Debug(
                    (type == TitloviSubtitleType.Episode ? "Series" : "Movie")
                    + " IMDb search returned no '" + providerLanguage + "' results for '" + imdbUsed
                    + "'; falling back to title '" + titleFallbackQuery + "'.");

                activeQuery = titleFallbackQuery;
                activeImdbId = null;
                page = 1;
                pagesAvailable = 1;
                do
                {
                    var response = await _api.SearchAsync(
                        activeQuery,
                        type,
                        seasons,
                        episodes,
                        activeImdbId,
                        page,
                        providerLanguage,
                        token,
                        cancellationToken).ConfigureAwait(false);

                    if (response?.SubtitleResults != null)
                        results.AddRange(response.SubtitleResults.Select(CloneSubtitle));

                    TitloviDiagnostics.Debug(
                        "Title fallback search page " + page + " for '" + providerLanguage + "' completed: returned="
                        + (response?.SubtitleResults == null ? 0 : response.SubtitleResults.Count)
                        + ", pagesAvailable=" + (response == null ? 0 : response.PagesAvailable)
                        + ", accumulated=" + results.Count + ".");

                    pagesAvailable = response == null ? page : Math.Max(page, response.PagesAvailable);
                    page++;
                }
                while (page <= pagesAvailable && !cancellationToken.IsCancellationRequested);
            }

            if (type == TitloviSubtitleType.Episode)
            {
                // Older whole-series records may only appear when querying Season=0
                // without an episode filter. Use the same active IMDb/title strategy
                // that succeeded for this language and merge genuine Season=0 packages.
                var wholeSeriesResults = new List<TitloviSubtitle>();
                var wholeSeriesPage = 1;
                var wholeSeriesPagesAvailable = 1;
                do
                {
                    var response = await _api.SearchAsync(
                        activeQuery,
                        type,
                        new[] { 0 },
                        null,
                        activeImdbId,
                        wholeSeriesPage,
                        providerLanguage,
                        token,
                        cancellationToken).ConfigureAwait(false);

                    if (response?.SubtitleResults != null)
                        wholeSeriesResults.AddRange(response.SubtitleResults.Select(CloneSubtitle));

                    TitloviDiagnostics.Debug(
                        "Whole-series Season=0 search page " + wholeSeriesPage + " for '" + providerLanguage + "' completed: returned="
                        + (response?.SubtitleResults == null ? 0 : response.SubtitleResults.Count)
                        + ", pagesAvailable=" + (response == null ? 0 : response.PagesAvailable)
                        + ", accumulated=" + wholeSeriesResults.Count + ".");

                    wholeSeriesPagesAvailable = response == null
                        ? wholeSeriesPage
                        : Math.Max(wholeSeriesPage, response.PagesAvailable);
                    wholeSeriesPage++;
                }
                while (wholeSeriesPage <= wholeSeriesPagesAvailable && !cancellationToken.IsCancellationRequested);

                var knownIds = new HashSet<int>(results.Select(x => x.Id));
                foreach (var candidate in wholeSeriesResults)
                {
                    TitloviDiagnostics.Debug(
                        "Whole-series candidate for '" + providerLanguage + "': mediaId=" + candidate.Id
                        + ", providerSeason=" + candidate.Season
                        + ", providerEpisode=" + candidate.Episode
                        + ", downloads=" + candidate.DownloadCount
                        + ", release='" + (candidate.Release ?? string.Empty) + "'.");

                    if (candidate.Season == 0 && knownIds.Add(candidate.Id))
                        results.Add(candidate);
                }
            }

            TitloviDiagnostics.Debug(
                "Language search completed: providerLanguage='" + providerLanguage
                + "', results=" + results.Count + ".");
            return results;
        }

        private static TitloviSubtitle CloneSubtitle(TitloviSubtitle source)
        {
            if (source == null)
                return null;

            return new TitloviSubtitle
            {
                Id = source.Id,
                Title = source.Title,
                Year = source.Year,
                Type = source.Type,
                Link = source.Link,
                Season = source.Season,
                Episode = source.Episode,
                Special = source.Special,
                Lang = source.Lang,
                LanguageCode = source.LanguageCode,
                Date = source.Date,
                DownloadCount = source.DownloadCount,
                Rating = source.Rating,
                Release = source.Release
            };
        }

        private static int CalculateReleaseMatchScore(string mediaPath, string release)
        {
            if (string.IsNullOrWhiteSpace(mediaPath) || string.IsNullOrWhiteSpace(release))
                return 0;

            var mediaName = Path.GetFileNameWithoutExtension(mediaPath) ?? mediaPath;
            var media = BuildReleaseFingerprint(mediaName);
            if (string.IsNullOrWhiteSpace(media.Normalized))
                return 0;

            var candidates = SplitReleaseCandidates(release);
            if (candidates.Count == 0)
                return 0;

            var bestScore = int.MinValue;
            foreach (var candidate in candidates)
            {
                var score = CalculateSingleReleaseMatchScore(media, candidate);
                if (score > bestScore)
                    bestScore = score;
            }

            return bestScore == int.MinValue ? 0 : bestScore;
        }

        private static int CalculateSingleReleaseMatchScore(ReleaseFingerprint media, string release)
        {
            var candidate = BuildReleaseFingerprint(release);
            if (string.IsNullOrWhiteSpace(candidate.Normalized))
                return 0;

            var score = 0;

            if (candidate.Tokens.Count >= 2
                && media.Normalized.IndexOf(candidate.Normalized, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 100;

            if (!string.IsNullOrWhiteSpace(media.ReleaseGroup)
                && !string.IsNullOrWhiteSpace(candidate.ReleaseGroup))
            {
                score += media.ReleaseGroup.Equals(candidate.ReleaseGroup, StringComparison.OrdinalIgnoreCase) ? 80 : -15;
            }

            if (!string.IsNullOrWhiteSpace(media.Source) && !string.IsNullOrWhiteSpace(candidate.Source))
            {
                if (media.Source.Equals(candidate.Source, StringComparison.OrdinalIgnoreCase))
                    score += 70;
                else if (!string.IsNullOrWhiteSpace(media.SourceFamily)
                    && media.SourceFamily.Equals(candidate.SourceFamily, StringComparison.OrdinalIgnoreCase))
                    score += 45;
                else
                    score -= 70;
            }

            if (!string.IsNullOrWhiteSpace(media.FrameRate) && !string.IsNullOrWhiteSpace(candidate.FrameRate))
                score += media.FrameRate.Equals(candidate.FrameRate, StringComparison.OrdinalIgnoreCase) ? 60 : -60;

            if (!string.IsNullOrWhiteSpace(media.Cut) && !string.IsNullOrWhiteSpace(candidate.Cut))
                score += media.Cut.Equals(candidate.Cut, StringComparison.OrdinalIgnoreCase) ? 50 : -60;

            score += CountIntersection(media.VariantFlags, candidate.VariantFlags) * 20;

            if (!string.IsNullOrWhiteSpace(media.Resolution) && !string.IsNullOrWhiteSpace(candidate.Resolution))
                score += media.Resolution.Equals(candidate.Resolution, StringComparison.OrdinalIgnoreCase) ? 15 : -2;

            if (!string.IsNullOrWhiteSpace(media.Codec) && !string.IsNullOrWhiteSpace(candidate.Codec))
                score += media.Codec.Equals(candidate.Codec, StringComparison.OrdinalIgnoreCase) ? 10 : -2;

            if (CountIntersection(media.HdrFlags, candidate.HdrFlags) > 0) score += 8;
            if (CountIntersection(media.AudioFlags, candidate.AudioFlags) > 0) score += 4;

            var genericMatches = media.Tokens
                .Where(x => candidate.Tokens.Contains(x) && IsGenericReleaseToken(x))
                .Take(12)
                .Count();
            score += genericMatches;

            return score;
        }

        private static List<string> SplitReleaseCandidates(string release)
        {
            if (string.IsNullOrWhiteSpace(release))
                return new List<string>();

            var prepared = Regex.Replace(release, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            return Regex.Split(prepared, @"(?:\r?\n|[;|]+|\s+/\s+)")
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static ReleaseFingerprint BuildReleaseFingerprint(string value)
        {
            var normalized = NormalizeReleaseText(value);
            var tokens = new HashSet<string>(
                normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(CanonicalizeReleaseToken),
                StringComparer.OrdinalIgnoreCase);

            var canonicalNormalized = string.Join(" ",
                normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(CanonicalizeReleaseToken));

            return new ReleaseFingerprint
            {
                Normalized = canonicalNormalized,
                Tokens = tokens,
                Resolution = ExtractResolution(tokens),
                Source = ExtractSource(tokens),
                SourceFamily = ExtractSourceFamily(tokens),
                Codec = ExtractCodec(tokens),
                FrameRate = ExtractFrameRate(value),
                ReleaseGroup = ExtractReleaseGroup(value),
                Cut = ExtractCut(tokens),
                VariantFlags = ExtractVariantFlags(tokens),
                HdrFlags = ExtractFlags(tokens, new[] { "hdr", "hdr10", "hdr10plus", "dolbyvision", "dv" }),
                AudioFlags = ExtractFlags(tokens, new[] { "aac", "ac3", "ddp", "dts", "truehd", "atmos", "flac" })
            };
        }

        private static string NormalizeReleaseText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.ToLowerInvariant()
                .Replace("director's cut", "directorscut")
                .Replace("directors cut", "directorscut")
                .Replace("director cut", "directorscut")
                .Replace("special edition", "specialedition")
                .Replace("blu-ray", "bluray")
                .Replace("blu ray", "bluray")
                .Replace("web-dl", "webdl")
                .Replace("web dl", "webdl")
                .Replace("h.264", "h264")
                .Replace("h.265", "h265")
                .Replace("dolby vision", "dolbyvision");

            return Regex.Replace(normalized, @"[^a-z0-9]+", " ").Trim();
        }

        private static string CanonicalizeReleaseToken(string token)
        {
            switch ((token ?? string.Empty).ToLowerInvariant())
            {
                case "h264": case "avc": return "x264";
                case "h265": case "hevc": return "x265";
                case "eac3": return "ddp";
                case "4k": case "uhd": return "2160p";
                case "8k": return "4320p";
                default: return token ?? string.Empty;
            }
        }

        private static string ExtractResolution(HashSet<string> tokens)
        {
            return tokens.FirstOrDefault(x => Regex.IsMatch(x, @"^(?:4320|2160|1080|720|576|540|480|360)p$"));
        }

        private static string ExtractSource(HashSet<string> tokens)
        {
            var known = new[] { "webdl", "webrip", "bluray", "bdrip", "brrip", "remux", "hdtv", "dvdrip", "dvd", "hdrip", "web" };
            return known.FirstOrDefault(tokens.Contains);
        }

        private static string ExtractSourceFamily(HashSet<string> tokens)
        {
            var source = ExtractSource(tokens);
            switch (source)
            {
                case "webdl": case "webrip": case "web": return "web";
                case "bluray": case "bdrip": case "brrip": case "remux": return "bluray";
                case "dvdrip": case "dvd": return "dvd";
                default: return source;
            }
        }

        private static string ExtractCodec(HashSet<string> tokens)
        {
            var known = new[] { "x265", "x264", "av1", "xvid", "mpeg2" };
            return known.FirstOrDefault(tokens.Contains);
        }

        private static string ExtractFrameRate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var decimalMatch = Regex.Match(value, @"(?<!\d)(?<fps>23[\.,](?:976|98)|29[\.,]97|59[\.,]94)(?:\s*fps)?(?!\d)", RegexOptions.IgnoreCase);
            if (decimalMatch.Success)
            {
                var fps = decimalMatch.Groups["fps"].Value.Replace(',', '.');
                if (fps.StartsWith("23.98", StringComparison.Ordinal)) return "23.976";
                return fps;
            }
            var integerMatch = Regex.Match(value, @"(?<!\d)(?<fps>24|25|30|50|60)(?:[\.,]0+)?\s*fps(?!\d)", RegexOptions.IgnoreCase);
            return integerMatch.Success ? integerMatch.Groups["fps"].Value : null;
        }

        private static string ExtractReleaseGroup(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var match = Regex.Match(value.Trim(), @"-(?<group>[A-Za-z0-9][A-Za-z0-9._]{1,30})$");
            if (!match.Success) return null;
            var prefix = value.Substring(0, match.Index);
            if (!ContainsTechnicalReleaseMarker(prefix)) return null;
            var groupTokens = NormalizeReleaseText(match.Groups["group"].Value)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(CanonicalizeReleaseToken).ToList();
            if (groupTokens.Count == 0 || groupTokens.All(IsTechnicalOrNoiseToken)) return null;
            return string.Join(" ", groupTokens);
        }

        private static bool ContainsTechnicalReleaseMarker(string value)
        {
            var normalized = NormalizeReleaseText(value);
            var tokens = new HashSet<string>(normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(CanonicalizeReleaseToken), StringComparer.OrdinalIgnoreCase);
            return ExtractResolution(tokens) != null || ExtractSource(tokens) != null || ExtractCodec(tokens) != null;
        }

        private static string ExtractCut(HashSet<string> tokens)
        {
            var known = new[] { "theatrical", "extended", "directorscut", "unrated", "uncut", "imax" };
            return known.FirstOrDefault(tokens.Contains);
        }

        private static HashSet<string> ExtractVariantFlags(HashSet<string> tokens) => ExtractFlags(tokens, new[] { "remastered", "specialedition", "proper", "rerip" });
        private static HashSet<string> ExtractFlags(HashSet<string> tokens, IEnumerable<string> known) => new HashSet<string>(known.Where(tokens.Contains), StringComparer.OrdinalIgnoreCase);
        private static int CountIntersection(HashSet<string> left, HashSet<string> right) => left == null || right == null || left.Count == 0 || right.Count == 0 ? 0 : left.Count(x => right.Contains(x));
        private static bool IsGenericReleaseToken(string token) => !string.IsNullOrWhiteSpace(token) && token.Length >= 4 && !token.All(char.IsDigit) && !IsTechnicalOrNoiseToken(token);

        private static bool IsTechnicalOrNoiseToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return true;
            if (Regex.IsMatch(token, @"^(?:4320|2160|1080|720|576|540|480|360)p$")) return true;
            switch (token.ToLowerInvariant())
            {
                case "dl": case "rip": case "webdl": case "webrip": case "web": case "bluray": case "bdrip": case "brrip": case "remux": case "hdtv": case "dvdrip": case "dvd": case "hdrip":
                case "x264": case "x265": case "av1": case "xvid": case "mpeg2": case "hdr": case "hdr10": case "hdr10plus": case "dolbyvision": case "dv": case "aac": case "ac3": case "ddp": case "dts": case "truehd": case "atmos": case "flac":
                case "theatrical": case "extended": case "directorscut": case "unrated": case "uncut": case "imax": case "remastered": case "specialedition": case "proper": case "rerip": case "complete": case "sub": case "subs": case "en": return true;
            }
            return false;
        }

        private sealed class ReleaseFingerprint
        {
            public string Normalized { get; set; }
            public HashSet<string> Tokens { get; set; }
            public string Resolution { get; set; }
            public string Source { get; set; }
            public string SourceFamily { get; set; }
            public string Codec { get; set; }
            public string FrameRate { get; set; }
            public string ReleaseGroup { get; set; }
            public string Cut { get; set; }
            public HashSet<string> VariantFlags { get; set; }
            public HashSet<string> HdrFlags { get; set; }
            public HashSet<string> AudioFlags { get; set; }
        }

        public async Task<CoreSubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            var metadata = DecodeMetadata(id);
            TitloviDiagnostics.Debug("Subtitle download requested: mediaId=" + metadata.Id + ", type=" + metadata.Type + ", providerLanguage='" + (metadata.Language ?? string.Empty) + "', languageCode='" + (metadata.LanguageCode ?? string.Empty) + "', season=" + metadata.Season + ", episode=" + metadata.Episode + ", episodeEnd=" + metadata.EpisodeEnd + ", episodeName='" + (metadata.EpisodeName ?? string.Empty) + "'.");
            var bytes = await _api.DownloadAsync(metadata.Id, metadata.Type, cancellationToken).ConfigureAwait(false);
            TitloviDiagnostics.Debug("Downloaded payload size=" + (bytes == null ? 0 : bytes.Length) + " bytes.");
            var files = ExtractSubtitles(bytes).Where(IsAcceptedDownloadedSubtitle).ToList();
            TitloviDiagnostics.Debug("Accepted extracted subtitle candidates=" + files.Count + ": " + string.Join(" | ", files.Select(x => (x.Path ?? "<raw>") + " [" + (x.Bytes == null ? 0 : x.Bytes.Length) + " bytes]")));
            if (files.Count == 0) { TitloviDiagnostics.Log("Downloaded payload contained no usable subtitle file."); throw new InvalidOperationException("Titlovi.com returned no usable subtitle file."); }
            DownloadedSubtitleFile selected = metadata.Type == TitloviSubtitleType.Episode ? SelectEpisodeSubtitle(files, metadata) : files[0];
            if (selected == null) throw new InvalidOperationException("Could not identify the requested episode subtitle inside the downloaded archive.");
            var outputBytes = selected.Bytes;
            var config = _configuration;
            if (config != null && config.ConvertDownloadedSubtitlesToUtf8)
            {
                outputBytes = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(selected.Bytes, metadata.Language, out var detectedEncoding, out var converted);
                TitloviDiagnostics.Debug("Encoding analysis: detected='" + detectedEncoding + "', converted=" + converted + ".");
                if (converted) TitloviDiagnostics.Log("Subtitle converted to UTF-8 from " + detectedEncoding + ".");
            }
            var outputFormat = GetSubtitleFormat(selected);
            var outputLanguage = !string.IsNullOrWhiteSpace(metadata.LanguageCode) ? metadata.LanguageCode : LanguageMap.FromProviderLanguage(metadata.Language);
            TitloviDiagnostics.Debug("Returning selected subtitle: path='" + (selected.Path ?? string.Empty) + "', format='" + outputFormat + "', providerLanguage='" + (metadata.Language ?? string.Empty) + "', storedLanguageCode='" + (metadata.LanguageCode ?? string.Empty) + "', hostLanguage='" + outputLanguage + "', bytes=" + outputBytes.Length + ".");
            return new CoreSubtitleResponse { Format = outputFormat, IsForced = false, IsHearingImpaired = false, LanguageCode = outputLanguage, Data = outputBytes };
        }

        private async Task<TitloviToken> GetTokenAsync(CancellationToken cancellationToken)
        {
            var config = _configuration;
            if (config.Token != null && !config.Token.IsExpired) { TitloviDiagnostics.Debug("Using cached Titlovi.com API token."); return config.Token; }
            TitloviDiagnostics.Debug("Cached Titlovi.com API token is missing or expired; authentication will be refreshed.");
            await TokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                config = _configuration;
                if (config.Token != null && !config.Token.IsExpired) { TitloviDiagnostics.Debug("Another request refreshed the API token while waiting for the token lock."); return config.Token; }
                var token = await _api.GetTokenAsync(config.Username, config.Password, cancellationToken).ConfigureAwait(false);
                config.Token = token; config.Save();
                TitloviDiagnostics.Debug("Titlovi.com API token refreshed and cached successfully.");
                return token;
            }
            finally { TokenLock.Release(); }
        }

        private CoreRemoteSubtitleInfo ToRemoteInfo(TitloviSubtitle subtitle, string requestedEpisodeName, int requestedEpisodeEnd, bool showLanguageInResultName)
        {
            var metadata = new SubtitleMetadata { Id = subtitle.Id, Type = subtitle.Type, Language = subtitle.Lang ?? string.Empty, LanguageCode = !string.IsNullOrWhiteSpace(subtitle.LanguageCode) ? subtitle.LanguageCode : LanguageMap.FromProviderLanguage(subtitle.Lang), Season = subtitle.Season, Episode = subtitle.Episode, EpisodeEnd = subtitle.Type == TitloviSubtitleType.Episode && requestedEpisodeEnd > subtitle.Episode ? requestedEpisodeEnd : 0, EpisodeName = subtitle.Type == TitloviSubtitleType.Episode ? (requestedEpisodeName ?? string.Empty) : string.Empty };
            var hostLanguage = metadata.LanguageCode;
            var legacyThreeLetterLanguage = LanguageMap.ToThreeLetterIso(metadata.Language);
            var resultName = subtitle.Title ?? string.Empty;
            if (showLanguageInResultName && !string.IsNullOrWhiteSpace(subtitle.Lang)) resultName += " — " + subtitle.Lang;
            TitloviDiagnostics.Debug("Preparing host result: mediaId=" + subtitle.Id + ", providerLanguage='" + (metadata.Language ?? string.Empty) + "', hostLanguage='" + (hostLanguage ?? string.Empty) + "', legacyThreeLetterLanguage='" + (legacyThreeLetterLanguage ?? string.Empty) + "'.");
            return new CoreRemoteSubtitleInfo { Id = EncodeMetadata(metadata), DateCreated = subtitle.Date, DownloadCount = subtitle.DownloadCount, Name = resultName, ProviderName = ProviderName, CommunityRating = subtitle.Rating, Comment = subtitle.Release ?? string.Empty, Format = "srt", LanguageCode = hostLanguage, ThreeLetterIsoLanguageName = legacyThreeLetterLanguage, IsForced = false, IsHearingImpaired = false };
        }

        private static string EncodeMetadata(SubtitleMetadata m)
        {
            var raw = string.Join("\t", new[] { m.Id.ToString(), ((int)m.Type).ToString(), m.Language ?? string.Empty, m.Season.ToString(), m.Episode.ToString(), (m.EpisodeName ?? string.Empty).Replace("\t", " "), m.EpisodeEnd.ToString(), m.LanguageCode ?? string.Empty });
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        private static SubtitleMetadata DecodeMetadata(string encoded)
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var parts = raw.Split('\t');
            if (parts.Length != 5 && parts.Length != 6 && parts.Length != 7 && parts.Length != 8) throw new FormatException("Invalid internal Titlovi subtitle id.");
            return new SubtitleMetadata { Id = int.Parse(parts[0]), Type = (TitloviSubtitleType)int.Parse(parts[1]), Language = parts[2], Season = int.Parse(parts[3]), Episode = int.Parse(parts[4]), EpisodeName = parts.Length >= 6 ? parts[5] : string.Empty, EpisodeEnd = parts.Length >= 7 ? int.Parse(parts[6]) : 0, LanguageCode = parts.Length >= 8 ? parts[7] : string.Empty };
        }

        private static DownloadedSubtitleFile SelectEpisodeSubtitle(List<DownloadedSubtitleFile> files, SubtitleMetadata metadata)
        {
            var isMultiEpisodeRequest = metadata.EpisodeEnd > metadata.Episode;
            if (isMultiEpisodeRequest)
            {
                foreach (var file in files)
                {
                    if (FilenameGroupMatchesEpisodeRange(file.Path, metadata.Season, metadata.Episode, metadata.EpisodeEnd, out var groupFormat))
                    { TitloviDiagnostics.Debug("Episode archive matched by exact multi-episode range (" + groupFormat + "): " + file.Path); return file; }
                }
                TitloviDiagnostics.Log("Episode archive match FAILED for multi-episode S" + metadata.Season + "E" + metadata.Episode + "-E" + metadata.EpisodeEnd + ". No subtitle file covers the exact requested range.");
                return null;
            }
            foreach (var file in files)
            {
                if (IsGroupedEpisodeFilename(file.Path)) continue;
                if (!TryParseEpisodeFromFilename(file.Path, out var season, out var episode, out var format)) continue;
                if (season.HasValue && season.Value == metadata.Season && episode == metadata.Episode) { TitloviDiagnostics.Debug("Episode archive matched by parsed season/episode (" + format + "): " + file.Path); return file; }
            }
            foreach (var file in files)
            {
                if (IsGroupedEpisodeFilename(file.Path)) continue;
                if (!TryParseEpisodeFromFilename(file.Path, out var season, out var episode, out var format)) continue;
                if (!season.HasValue && episode == metadata.Episode) { TitloviDiagnostics.Debug("Episode archive matched by parsed episode-only filename (" + format + "): " + file.Path); return file; }
            }
            var normalizedEpisodeName = NormalizeMatchText(metadata.EpisodeName);
            if (normalizedEpisodeName.Length >= 4)
            {
                foreach (var file in files)
                {
                    if (IsGroupedEpisodeFilename(file.Path)) continue;
                    var normalizedPath = NormalizeMatchText(Path.GetFileNameWithoutExtension(file.Path ?? string.Empty));
                    if (normalizedPath.Contains(normalizedEpisodeName)) { TitloviDiagnostics.Debug("Episode archive matched by episode title: " + file.Path); return file; }
                }
            }
            if (files.Count == 1 && !IsGroupedEpisodeFilename(files[0].Path)) return files[0];
            TitloviDiagnostics.Log("Episode archive match FAILED for S" + metadata.Season + "E" + metadata.Episode + ". Enable detailed debug logging to inspect candidate filenames.");
            return null;
        }

        private static bool FilenameGroupMatchesEpisodeRange(string path, int targetSeason, int targetEpisodeStart, int targetEpisodeEnd, out string format)
        {
            format = string.Empty;
            var fullName = (path ?? string.Empty).Replace('\\', '/');
            var fileName = Path.GetFileNameWithoutExtension(path ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileName) || targetEpisodeStart <= 0 || targetEpisodeEnd <= targetEpisodeStart) return false;
            foreach (Match listMatch in EpisodeListRegex.Matches(fileName))
            {
                var episodes = ParseEpisodeList(listMatch.Groups["list"].Value);
                if (!EpisodesExactlyMatchRange(episodes, targetEpisodeStart, targetEpisodeEnd)) continue;
                var seasonHint = FindArabicSeasonHint(fullName, fullName.Length);
                if (seasonHint.HasValue && seasonHint.Value != targetSeason) continue;
                format = seasonHint.HasValue ? "episode-list with season" : "episode-list"; return true;
            }
            foreach (Match romanMatch in RomanSeasonNumberRegex.Matches(fileName))
            {
                var romanSeason = RomanToInt(romanMatch.Groups["roman"].Value);
                if (romanSeason <= 0 || romanSeason != targetSeason) continue;
                var token = romanMatch.Groups["number"].Value.TrimStart('0');
                if (IsSequentialEpisodeBundle(token, out var bundledEpisodes) && EpisodesExactlyMatchRange(bundledEpisodes, targetEpisodeStart, targetEpisodeEnd)) { format = "roman-season episode-bundle"; return true; }
            }
            var explicitSeason = FindArabicSeasonHint(fullName, fullName.Length);
            if (!explicitSeason.HasValue || explicitSeason.Value != targetSeason) return false;
            foreach (Match bundleMatch in CompactEpisodeBundleRegex.Matches(fileName))
            {
                if (IsCompactBundleInsideExplicitEpisodeToken(fileName, bundleMatch)) continue;
                if (!IsSequentialEpisodeBundle(bundleMatch.Groups["bundle"].Value, out var bundledEpisodes)) continue;
                if (!EpisodesExactlyMatchRange(bundledEpisodes, targetEpisodeStart, targetEpisodeEnd)) continue;
                format = "season + compact episode-bundle"; return true;
            }
            return false;
        }

        private static bool IsGroupedEpisodeFilename(string path)
        {
            var fullName = (path ?? string.Empty).Replace('\\', '/');
            var fileName = Path.GetFileNameWithoutExtension(path ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            if (EpisodeListRegex.IsMatch(fileName)) return true;
            foreach (Match romanMatch in RomanSeasonNumberRegex.Matches(fileName))
            {
                var token = romanMatch.Groups["number"].Value.TrimStart('0');
                if (IsSequentialEpisodeBundle(token, out _)) return true;
            }
            if (FindArabicSeasonHint(fullName, fullName.Length).HasValue)
            {
                foreach (Match bundleMatch in CompactEpisodeBundleRegex.Matches(fileName))
                {
                    if (IsCompactBundleInsideExplicitEpisodeToken(fileName, bundleMatch)) continue;
                    if (IsSequentialEpisodeBundle(bundleMatch.Groups["bundle"].Value, out _)) return true;
                }
            }
            return false;
        }

        private static bool IsCompactBundleInsideExplicitEpisodeToken(string fileName, Match bundleMatch)
        {
            var bundle = bundleMatch == null ? null : bundleMatch.Groups["bundle"];
            if (bundle == null || !bundle.Success) return false;
            var bundleStart = bundle.Index;
            var bundleEnd = bundle.Index + bundle.Length;
            foreach (Match episodeToken in EpisodeTokenRegex.Matches(fileName ?? string.Empty))
            {
                if (bundleStart >= episodeToken.Index && bundleEnd <= episodeToken.Index + episodeToken.Length)
                    return true;
            }
            return false;
        }

        private static bool EpisodesExactlyMatchRange(IEnumerable<int> episodes, int startEpisode, int endEpisode)
        {
            var ordered = (episodes ?? Enumerable.Empty<int>()).Distinct().OrderBy(x => x).ToList();
            if (ordered.Count != endEpisode - startEpisode + 1) return false;
            for (var i = 0; i < ordered.Count; i++) if (ordered[i] != startEpisode + i) return false;
            return true;
        }

        private static int? FindArabicSeasonHint(string fileName, int beforeIndex)
        {
            int? season = null; var bestIndex = -1;
            foreach (Match match in SeasonTokenRegex.Matches(fileName ?? string.Empty))
            {
                if (!match.Success || match.Index >= beforeIndex) continue;
                if (int.TryParse(match.Groups["season"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 && match.Index > bestIndex) { bestIndex = match.Index; season = parsed; }
            }
            return season;
        }

        private static List<int> ParseEpisodeList(string value)
        {
            var result = new List<int>();
            foreach (var part in Regex.Split(value ?? string.Empty, @"\s*[,;+&/]\s*"))
                if (int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var episode) && episode > 0 && !result.Contains(episode)) result.Add(episode);
            return result;
        }

        private static bool IsSequentialEpisodeBundle(string digits, out List<int> episodes)
        {
            episodes = new List<int>();
            if (string.IsNullOrWhiteSpace(digits) || digits.Length < 2 || digits.Length > 4) return false;
            var previous = -1;
            foreach (var ch in digits)
            {
                if (ch < '1' || ch > '9') return false;
                var current = ch - '0';
                if (previous >= 0 && current != previous + 1) return false;
                episodes.Add(current); previous = current;
            }
            return true;
        }

        private static int RomanToInt(string roman)
        {
            switch ((roman ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "I": return 1; case "II": return 2; case "III": return 3; case "IV": return 4; case "V": return 5; case "VI": return 6; case "VII": return 7; case "VIII": return 8; case "IX": return 9; case "X": return 10; case "XI": return 11; case "XII": return 12; default: return 0;
            }
        }

        private static bool TryParseEpisodeFromFilename(string path, out int? season, out int episode, out string format)
        {
            season = null; episode = 0; format = string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(path ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            foreach (Match episodeToken in EpisodeTokenRegex.Matches(fileName))
            {
                var seasonHint = FindArabicSeasonHint(fileName, episodeToken.Index);
                if (!seasonHint.HasValue) continue;
                if (int.TryParse(episodeToken.Groups["episode"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var separatedEpisode) && separatedEpisode > 0) { season = seasonHint.Value; episode = separatedEpisode; format = "separated season/episode tokens"; return true; }
            }
            foreach (Match romanMatch in RomanSeasonNumberRegex.Matches(fileName))
            {
                var romanSeason = RomanToInt(romanMatch.Groups["roman"].Value); var token = romanMatch.Groups["number"].Value.TrimStart('0');
                if (romanSeason <= 0 || token.Length == 0 || IsSequentialEpisodeBundle(token, out _)) continue;
                if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var romanEpisode) && romanEpisode > 0) { season = romanSeason; episode = romanEpisode; format = "roman-season episode-number"; return true; }
            }
            var matches = EpisodeCandidateRegex.Matches(fileName).Cast<Match>().ToList();
            var best = matches.FirstOrDefault(m => m.Groups["season"].Success && m.Groups["episode"].Success) ?? matches.OrderByDescending(m => m.Value.Length).FirstOrDefault();
            if (best != null && best.Success && int.TryParse(best.Groups["episode"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedEpisode) && parsedEpisode > 0)
            {
                episode = parsedEpisode;
                if (best.Groups["season"].Success && int.TryParse(best.Groups["season"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedSeason) && parsedSeason > 0) { season = parsedSeason; format = "Titlovi-regex season/episode"; }
                else format = "Titlovi-regex episode-only";
                return true;
            }
            var leading = LeadingNumberRegex.Match(fileName);
            if (!leading.Success) return false;
            var digits = leading.Groups["number"].Value;
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0) return false;
            if (digits.Length >= 3 && digits[0] == '0') { episode = number; format = "leading-ordinal"; return true; }
            if (digits.Length >= 3)
            {
                var seasonText = digits.Substring(0, digits.Length - 2); var episodeText = digits.Substring(digits.Length - 2);
                if (int.TryParse(seasonText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedSeason) && int.TryParse(episodeText, NumberStyles.None, CultureInfo.InvariantCulture, out parsedEpisode) && parsedSeason > 0 && parsedEpisode > 0) { season = parsedSeason; episode = parsedEpisode; format = "compact-TV-number"; return true; }
            }
            episode = number; format = "leading-ordinal"; return true;
        }

        private static string NormalizeMatchText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var decomposed = value.Normalize(NormalizationForm.FormD); var sb = new StringBuilder(decomposed.Length);
            foreach (var ch in decomposed) { if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue; if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch)); }
            return sb.ToString();
        }

        private List<DownloadedSubtitleFile> ExtractSubtitles(byte[] buffer) { var result = new List<DownloadedSubtitleFile>(); ExtractSubtitlesRecursive(buffer, string.Empty, 0, result); return result; }

        private void ExtractSubtitlesRecursive(byte[] buffer, string pathPrefix, int depth, List<DownloadedSubtitleFile> result)
        {
            if (buffer == null || buffer.Length == 0 || depth > 3) return;
            try
            {
                using (var ms = new MemoryStream(buffer, writable: false))
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrWhiteSpace(entry.Name)) continue;
                        var fullPath = string.IsNullOrWhiteSpace(pathPrefix) ? entry.FullName : pathPrefix + "!/" + entry.FullName;
                        if (IsSupportedSubtitleFile(entry.Name))
                        {
                            using (var source = entry.Open()) using (var target = new MemoryStream()) { source.CopyTo(target); result.Add(new DownloadedSubtitleFile(fullPath, target.ToArray())); }
                            continue;
                        }
                        if (entry.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var source = entry.Open()) using (var target = new MemoryStream()) { source.CopyTo(target); ExtractSubtitlesRecursive(target.ToArray(), fullPath, depth + 1, result); }
                        }
                    }
                    return;
                }
            }
            catch (InvalidDataException)
            {
                if (depth == 0) result.Add(new DownloadedSubtitleFile(string.Empty, buffer));
            }
        }

        private bool IsSupportedSubtitleFile(string name) => !string.IsNullOrWhiteSpace(name) && IsAcceptedExtension(Path.GetExtension(name).TrimStart('.').ToLowerInvariant());
        private bool IsAcceptedDownloadedSubtitle(DownloadedSubtitleFile file) => file != null && (!string.IsNullOrWhiteSpace(file.Path) ? IsAcceptedExtension(Path.GetExtension(file.Path).TrimStart('.').ToLowerInvariant()) : IsAcceptedExtension(GetSubtitleFormat(file)));

        private bool IsAcceptedExtension(string extension)
        {
            var config = _configuration;
            if (config == null) return extension == "srt" || extension == "sub" || extension == "txt" || extension == "ssa" || extension == "ass";
            switch ((extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant()) { case "srt": return config.AcceptSrt; case "sub": return config.AcceptSub; case "txt": return config.AcceptTxt; case "ssa": return config.AcceptSsa; case "ass": return config.AcceptAss; default: return false; }
        }

        private static string GetSubtitleFormat(DownloadedSubtitleFile file)
        {
            if (file == null) return "srt";
            var extension = string.IsNullOrWhiteSpace(file.Path) ? string.Empty : Path.GetExtension(file.Path).TrimStart('.').ToLowerInvariant();
            if (extension == "sub" || extension == "ssa" || extension == "ass" || extension == "srt") return extension;
            var detected = DetectTextSubtitleFormat(file.Bytes);
            return string.IsNullOrWhiteSpace(detected) ? "srt" : detected;
        }

        private static string DetectTextSubtitleFormat(byte[] bytes)
        {
            var probeLength = Math.Min(bytes == null ? 0 : bytes.Length, 8192);
            if (probeLength <= 0) return null;
            var probe = Encoding.ASCII.GetString(bytes, 0, probeLength);
            if (Regex.IsMatch(probe, @"(?m)^\s*\{\d+\}\{\d+\}")) return "sub";
            if (probe.Contains("-->")) return "srt";
            if (probe.IndexOf("[Script Info]", StringComparison.OrdinalIgnoreCase) >= 0 && probe.IndexOf("[Events]", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (probe.IndexOf("[V4+ Styles]", StringComparison.OrdinalIgnoreCase) >= 0) return "ass";
                if (probe.IndexOf("[V4 Styles]", StringComparison.OrdinalIgnoreCase) >= 0) return "ssa";
            }
            return null;
        }
    }
}
