using System;
using System.IO;
using System.Text;

namespace Titlovi.Common
{
    internal static class TitloviDiagnostics
    {
        private const long BasicLogMaxBytes = 2L * 1024L * 1024L;
        private const long DebugLogMaxBytes = 8L * 1024L * 1024L;
        private static readonly object Sync = new object();
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
        private static string _basicPath;
        private static string _debugPath;
        private static Func<bool> _isDebugEnabled;
        private static string _hostName = "Titlovi";
        private static string _version = string.Empty;

        public static string BasicPath => _basicPath;
        public static string DebugPath => _debugPath;

        public static bool IsDebugEnabled
        {
            get
            {
                try
                {
                    return _isDebugEnabled != null && _isDebugEnabled();
                }
                catch
                {
                    return false;
                }
            }
        }

        // Diagnostics are deliberately best-effort. Failure to create or write a
        // log file must never prevent the host from loading or using the plugin.
        public static void Initialize(
            string logDirectoryPath,
            string hostName,
            string version,
            Func<bool> isDebugEnabled)
        {
            lock (Sync)
            {
                _basicPath = null;
                _debugPath = null;
                _hostName = string.IsNullOrWhiteSpace(hostName) ? "Titlovi" : hostName.Trim();
                _version = version ?? string.Empty;
                _isDebugEnabled = isDebugEnabled;

                var directory = TryPrepareDirectory(logDirectoryPath);
                if (string.IsNullOrWhiteSpace(directory))
                    directory = TryPrepareDirectory(Path.GetTempPath());

                if (string.IsNullOrWhiteSpace(directory))
                    return;

                var prefix = _hostName.ToLowerInvariant().Replace(" ", "-");
                _basicPath = Path.Combine(directory, prefix + "-titlovi.log");
                _debugPath = Path.Combine(directory, prefix + "-titlovi-debug.log");
            }

            Log(_hostName + ".Titlovi " + _version + " initialized. Detailed debug logging is "
                + (IsDebugEnabled ? "enabled" : "disabled") + ".");
            Debug("Debug session started. Basic log: " + (_basicPath ?? "<unavailable>")
                + "; debug log: " + (_debugPath ?? "<unavailable>") + ".");
        }

        public static void Log(string message)
        {
            Append(_basicPath, "INFO", message, BasicLogMaxBytes);
            if (IsDebugEnabled)
                Append(_debugPath, "INFO", message, DebugLogMaxBytes);
        }

        public static void Debug(string message)
        {
            if (!IsDebugEnabled)
                return;

            Append(_debugPath, "DEBUG", message, DebugLogMaxBytes);
        }

        private static string TryPrepareDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                Directory.CreateDirectory(path);
                return path;
            }
            catch
            {
                return null;
            }
        }

        private static void Append(string path, string level, string message, long maxBytes)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                lock (Sync)
                {
                    RotateIfNeeded(path, maxBytes);
                    var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        + " | " + level + " | " + (message ?? string.Empty)
                        + Environment.NewLine;
                    File.AppendAllText(path, line, Utf8NoBom);
                }
            }
            catch
            {
                // Logging must never break plugin loading, search or download.
            }
        }

        private static void RotateIfNeeded(string path, long maxBytes)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length < maxBytes)
                    return;

                var previous = path + ".1";
                if (File.Exists(previous))
                    File.Delete(previous);
                File.Move(path, previous);
            }
            catch
            {
                // Best-effort rotation only.
            }
        }
    }
}
