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
        public static readonly Guid PluginId = Guid.Parse("8d8e1f97-ae88-4b46-acac-a16f6a2d88dd");

        public static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
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
