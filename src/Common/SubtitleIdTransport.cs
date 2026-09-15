using System;

namespace Titlovi.Common
{
    /// <summary>
    /// Converts internal subtitle ids to and from the URL-safe Base64 alphabet used at host boundaries.
    /// The engine keeps its existing standard Base64 metadata format; host adapters only make that
    /// opaque id safe for route segments and restore it before calling back into the engine.
    /// </summary>
    internal static class SubtitleIdTransport
    {
        public static string ToUrlSafe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string FromUrlSafe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var normalized = value
                .Replace('-', '+')
                .Replace('_', '/');

            switch (normalized.Length % 4)
            {
                case 0:
                    return normalized;
                case 2:
                    return normalized + "==";
                case 3:
                    return normalized + "=";
                default:
                    throw new FormatException("Invalid URL-safe Base64 subtitle id.");
            }
        }
    }
}
