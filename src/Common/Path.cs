namespace Titlovi.Common
{
    /// <summary>
    /// Path facade used by shared code.
    ///
    /// Filename parsing treats both Windows and Unix directory separators as '/'
    /// so archive/media names behave identically on every host OS. Operations that
    /// access the real filesystem still delegate to System.IO.Path and therefore
    /// retain the native platform behavior.
    /// </summary>
    internal static class Path
    {
        public static string GetFileNameWithoutExtension(string path)
        {
            var fileName = GetPortableFileName(path);
            return System.IO.Path.GetFileNameWithoutExtension(fileName);
        }

        public static string GetExtension(string path)
        {
            var fileName = GetPortableFileName(path);
            return System.IO.Path.GetExtension(fileName);
        }

        public static string GetTempPath()
        {
            return System.IO.Path.GetTempPath();
        }

        public static string Combine(string path1, string path2)
        {
            return System.IO.Path.Combine(path1, path2);
        }

        private static string GetPortableFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path ?? string.Empty;

            var normalized = path.Replace('\\', '/');
            var separatorIndex = normalized.LastIndexOf('/');
            return separatorIndex >= 0
                ? normalized.Substring(separatorIndex + 1)
                : normalized;
        }
    }
}
