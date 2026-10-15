using System;
using System.IO;
using System.Security;
using System.Xml;
using System.Xml.Linq;

namespace Jellyfin.Titlovi.Configuration
{
    // Reads only the credentials from Matko's existing Jellyfin plugin configuration.
    // Never deserialize its Token type: its XML model differs from Titlovi.Common.TitloviToken.
    internal static class LegacyConfigurationImport
    {
        internal const string LegacyFileName = "Titlovi.Plugin.xml";
        internal const string CurrentFileName = "Jellyfin.Titlovi.xml";

        internal static bool TryReadForImport(string configurationDirectory, out LegacyCredentials credentials)
        {
            credentials = null;
            if (string.IsNullOrWhiteSpace(configurationDirectory))
                return false;

            try
            {
                // The new configuration always takes precedence, including an intentionally
                // unconfigured or unreadable file. Never overwrite it with old credentials.
                if (File.Exists(System.IO.Path.Combine(configurationDirectory, CurrentFileName)))
                    return false;

                var legacyPath = System.IO.Path.Combine(configurationDirectory, LegacyFileName);
                if (!File.Exists(legacyPath))
                    return false;

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = 65536
                };

                using (var reader = XmlReader.Create(legacyPath, settings))
                {
                    var document = XDocument.Load(reader);
                    var root = document.Root;
                    if (root == null || root.Name.LocalName != "TitloviConfiguration")
                        return false;

                    string username = null;
                    string password = null;
                    foreach (var element in root.Elements())
                    {
                        if (element.Name.LocalName == "Username")
                            username = element.Value;
                        else if (element.Name.LocalName == "Password")
                            password = element.Value;
                    }

                    if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password))
                        return false;

                    // Preserve the original values verbatim (including whitespace in a password).
                    credentials = new LegacyCredentials(username ?? string.Empty, password ?? string.Empty);
                    return true;
                }
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is XmlException ||
                ex is SecurityException ||
                ex is ArgumentException ||
                ex is NotSupportedException)
            {
                // Optional migration must not prevent the plugin from starting.
                // Do not log paths, XML content, usernames, passwords or token values.
                return false;
            }
        }
    }

    internal sealed class LegacyCredentials
    {
        internal LegacyCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }

        internal string Username { get; }
        internal string Password { get; }
    }
}
