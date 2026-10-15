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
            if (!LegacyConfigurationImport.TryReadForImport(configurationDirectory, out var legacy))
                return;

            try
            {
                // Save through Jellyfin's own XML serializer under Jellyfin.Titlovi.xml.
                // The source Titlovi.Plugin.xml remains intact for rollback.
                // A legacy token is intentionally not reused because its model differs.
                SaveConfiguration(new PluginConfiguration
                {
                    Username = legacy.Username,
                    Password = legacy.Password,
                    Token = null
                });
            }
            catch
            {
                // Credential import is best-effort. Never block normal plugin startup
                // or include potentially sensitive configuration details in logs.
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
