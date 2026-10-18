using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Titlovi.Configuration;
using Titlovi.Common;

namespace Jellyfin.Titlovi
{
    public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public const string PluginVersion = "1.1.9";
        public static readonly Guid PluginId = Guid.Parse("6e25df50-638e-4109-a50b-03c14fc93fdd");

        public static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            ImportLegacyConfigurationIfNeeded(applicationPaths.PluginConfigurationsPath);
            try
            {
                TitloviDiagnostics.Initialize(
                    applicationPaths.LogDirectoryPath,
                    "jellyfin",
                    PluginVersion,
                    () => Instance?.Configuration?.EnableDebugLogging == true);
            }
            catch
            {
                // Diagnostics are best-effort only.
            }
        }

        private void ImportLegacyConfigurationIfNeeded(string configurationDirectory)
        {
            // Existing Jellyfin.Titlovi.xml may contain an empty configuration from an
            // earlier preview installation. Never use file existence as an import guard.
            var current = Configuration;
            if (current == null || current.LegacyCredentialsImportHandled)
                return;

            if (!LegacyConfigurationImport.TryReadForImport(configurationDirectory, out var legacy))
                return;

            try
            {
                if (LegacyConfigurationImport.ShouldImportCredentials(
                    current.Username,
                    current.Password,
                    current.Token != null,
                    current.LegacyCredentialsImportHandled))
                {
                    // Preserve all non-credential options already set in Jellyfin.
                    current.Username = legacy.Username;
                    current.Password = legacy.Password;
                    current.Token = null; // Original token uses an incompatible XML model.
                }

                // Record the decision even if user-supplied new credentials won.
                // Otherwise clearing them later could silently restore the old login.
                current.LegacyCredentialsImportHandled = true;
                SaveConfiguration(current);
            }
            catch
            {
                // Import must never block startup or log credential values.
            }
        }

        public override string Name => "Titlovi.com (Unofficial)";
        public override string Description => "Unofficial Titlovi.com subtitle provider for Jellyfin.";
        public override Guid Id => PluginId;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    DisplayName = "Titlovi.com (Unofficial)",
                    Name = "JellyfinTitloviConfigPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                },
                new PluginPageInfo
                {
                    Name = "JellyfinTitloviConfigPageJs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.js"
                }
            };
        }
    }
}
