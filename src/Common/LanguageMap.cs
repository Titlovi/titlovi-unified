using System;
using System.Collections.Generic;

namespace Titlovi.Common
{
    internal static class LanguageMap
    {
        public static string ToProviderLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            switch (code.Trim().ToLowerInvariant())
            {
                case "hr": case "hrv": case "croatian": case "hrvatski": return "Hrvatski";
                case "bs": case "bos": case "bosnian": case "bosanski": return "Bosanski";
                case "en": case "eng": case "english": return "English";
                case "mk": case "mkd": case "macedonian": case "makedonski": return "Makedonski";
                case "sr": case "srp": case "serbian": case "srpski": return "Srpski";
                case "sl": case "slv": case "slovenian": case "slovenski": return "Slovenski";
                case "cyr": case "cir": case "cirilica": case "cyrillic": return "Cirilica";
                default: return string.Empty;
            }
        }

        // Keep the canonical two-letter language code (sr, hr, bs...) in the shared
        // model. Host adapters translate it to whichever metadata fields their API exposes.
        public static string FromProviderLanguage(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            switch (name.Trim().ToLowerInvariant())
            {
                case "hrvatski": return "hr";
                case "bosanski": return "bs";
                case "english": return "en";
                case "makedonski": return "mk";
                case "srpski": return "sr";
                case "slovenski": return "sl";
                case "cirilica": return "sr";
                default: return string.Empty;
            }
        }

        public static string ToThreeLetterIso(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            switch (name.Trim().ToLowerInvariant())
            {
                case "hrvatski": return "hrv";
                case "bosanski": return "bos";
                case "english": return "eng";
                case "makedonski": return "mkd";
                case "srpski": return "srp";
                case "slovenski": return "slv";
                case "cirilica": return "srp";
                default: return string.Empty;
            }
        }

        public static IReadOnlyList<string> GetSearchLanguages(string requestedCode, bool includeBshAlternatives)
        {
            var primary = ToProviderLanguage(requestedCode);
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(primary))
                return result;

            result.Add(primary);
            if (!includeBshAlternatives || !IsBshLanguage(primary))
                return result;

            // The host-selected language is always first. The remaining B/S/H
            // languages follow in a stable order so the result grouping is predictable.
            foreach (var language in new[] { "Bosanski", "Hrvatski", "Srpski" })
            {
                if (!language.Equals(primary, StringComparison.OrdinalIgnoreCase))
                    result.Add(language);
            }

            return result;
        }

        private static bool IsBshLanguage(string providerLanguage)
        {
            return providerLanguage.Equals("Srpski", StringComparison.OrdinalIgnoreCase)
                || providerLanguage.Equals("Hrvatski", StringComparison.OrdinalIgnoreCase)
                || providerLanguage.Equals("Bosanski", StringComparison.OrdinalIgnoreCase);
        }
    }
}
