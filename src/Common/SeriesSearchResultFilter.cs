using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Titlovi.Common
{
    internal static class SeriesSearchResultFilter
    {
        private static readonly HashSet<string> NoiseTokens = new HashSet<string>(
            new[] { "the", "a", "an", "of", "in", "and" },
            StringComparer.OrdinalIgnoreCase);

        public static List<CoreRemoteSubtitleInfo> Apply(
            IEnumerable<CoreRemoteSubtitleInfo> results,
            TitloviContentType contentType,
            string requestedSeriesTitle)
        {
            var materialized = (results ?? Enumerable.Empty<CoreRemoteSubtitleInfo>()).ToList();
            if (contentType != TitloviContentType.Episode || string.IsNullOrWhiteSpace(requestedSeriesTitle))
                return materialized;

            var filtered = new List<CoreRemoteSubtitleInfo>(materialized.Count);
            foreach (var result in materialized)
            {
                var providerTitle = StripLanguageSuffix(result?.Name);
                if (IsClearlyDifferentSeriesTitle(requestedSeriesTitle, providerTitle))
                {
                    TitloviDiagnostics.Debug(
                        "Discarding episode search result from a clearly different series: requestedSeries='"
                        + requestedSeriesTitle + "', providerTitle='" + (providerTitle ?? string.Empty) + "'.");
                    continue;
                }

                filtered.Add(result);
            }

            return filtered;
        }

        internal static bool IsClearlyDifferentSeriesTitle(string requestedTitle, string candidateTitle)
        {
            if (string.IsNullOrWhiteSpace(requestedTitle) || string.IsNullOrWhiteSpace(candidateTitle))
                return false;

            var requestedTokens = GetComparableTitleTokens(requestedTitle);
            var candidateTokens = GetComparableTitleTokens(candidateTitle);
            if (requestedTokens.Count == 0 || candidateTokens.Count == 0)
                return false;

            if (requestedTokens.SetEquals(candidateTokens))
                return false;

            // Titlovi.com's title search can return another series whose title simply
            // extends the requested one, e.g. "The Wire" -> "Wire in the Blood".
            // Treat only strict token-set containment as a definite cross-series collision.
            // Completely different/localized titles remain untouched because there is
            // insufficient evidence to reject them safely.
            return IsStrictSubset(requestedTokens, candidateTokens)
                || IsStrictSubset(candidateTokens, requestedTokens);
        }

        private static bool IsStrictSubset(HashSet<string> subset, HashSet<string> superset)
        {
            return subset.Count < superset.Count && subset.All(superset.Contains);
        }

        private static HashSet<string> GetComparableTitleTokens(string value)
        {
            // Parenthetical year/country qualifiers are commonly metadata rather than
            // part of the series identity: "The Office (US)" should match "The Office".
            var prepared = Regex.Replace(value ?? string.Empty, @"\([^)]*\)", " ");
            var decomposed = prepared.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToLowerInvariant(ch));
                else
                    sb.Append(' ');
            }

            return new HashSet<string>(
                sb.ToString()
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(token => !NoiseTokens.Contains(token))
                    .Where(token => !IsStandaloneYear(token))
                    .Where(token => token.Length > 1 || !char.IsLetter(token[0])),
                StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsStandaloneYear(string token)
        {
            if (token == null || token.Length != 4 || !token.All(char.IsDigit))
                return false;

            int year;
            return int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out year)
                && year >= 1900
                && year <= 2099;
        }

        private static string StripLanguageSuffix(string resultName)
        {
            if (string.IsNullOrWhiteSpace(resultName))
                return resultName ?? string.Empty;

            var separatorIndex = resultName.LastIndexOf(" — ", StringComparison.Ordinal);
            if (separatorIndex <= 0)
                return resultName;

            var suffix = resultName.Substring(separatorIndex + 3).Trim();
            return string.IsNullOrWhiteSpace(LanguageMap.ToProviderLanguage(suffix))
                ? resultName
                : resultName.Substring(0, separatorIndex).TrimEnd();
        }
    }
}
