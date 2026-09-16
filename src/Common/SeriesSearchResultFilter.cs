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

            var requestedAliases = GetComparableTitleAliases(requestedTitle);
            var candidateAliases = GetComparableTitleAliases(candidateTitle);
            if (requestedAliases.Count == 0 || candidateAliases.Count == 0)
                return false;

            // Titlovi.com commonly joins an original title and an English/international
            // title with "AKA". Treat every AKA segment as an equal title identity.
            // If any requested/candidate alias matches exactly, this is not a cross-series
            // collision even when the full provider display title contains extra aliases.
            if (requestedAliases.Any(requested =>
                candidateAliases.Any(candidate => requested.SetEquals(candidate))))
            {
                return false;
            }

            // Titlovi.com's fuzzy title search can return another series whose title
            // extends the requested one, e.g. "The Wire" -> "Wire in the Blood".
            // After checking exact AKA aliases above, treat strict token-set containment
            // between any alias pair as a definite cross-series collision. Completely
            // different/localized titles remain untouched because there is insufficient
            // evidence to reject them safely.
            foreach (var requested in requestedAliases)
            {
                foreach (var candidate in candidateAliases)
                {
                    if (IsStrictSubset(requested, candidate)
                        || IsStrictSubset(candidate, requested))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsStrictSubset(HashSet<string> subset, HashSet<string> superset)
        {
            return subset.Count < superset.Count && subset.All(superset.Contains);
        }

        private static List<HashSet<string>> GetComparableTitleAliases(string value)
        {
            return Regex.Split(value ?? string.Empty, @"\s+\bAKA\b\s+", RegexOptions.IgnoreCase)
                .Select(GetComparableTitleTokens)
                .Where(tokens => tokens.Count > 0)
                .ToList();
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
