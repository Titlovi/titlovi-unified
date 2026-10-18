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

        // The current file's presence alone is not evidence of user configuration:
        // Jellyfin can create an empty XML file before the old credentials are imported.
        // Only the host plugin decides whether existing settings must take precedence.
        internal static bool ShouldImportCredentials(
            string currentUsername,
            string currentPassword,
            bool hasCurrentToken,
            bool migrationAlreadyHandled)
        {
            return !migrationAlreadyHandled &&
                string.IsNullOrWhiteSpace(currentUsername) &&
                string.IsNullOrWhiteSpace(currentPassword) &&
                !hasCurrentToken;
        }

        internal static bool TryReadForImport(string configurationDirectory, out LegacyCredentials credentials)
        {
            credentials = null;
            if (string.IsNullOrWhiteSpace(configurationDirectory))
                return false;

            try
            {
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
