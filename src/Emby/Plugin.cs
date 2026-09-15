using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Emby.Titlovi.Configuration;
using Titlovi.Common;

namespace Emby.Titlovi
{
    public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public const string PluginVersion = "1.1.9";
        public static readonly Guid PluginId = Guid.Parse("1dbf2e1e-3187-4f63-9a60-4ca7ace4232e");

        public static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            try
            {
                TitloviDiagnostics.Initialize(
                    applicationPaths.LogDirectoryPath,
                    "emby",
                    PluginVersion,
                    () => Instance?.Configuration?.EnableDebugLogging == true);
            }
            catch
            {
                // Diagnostics are best-effort only.
            }
        }

        public override string Name => "Titlovi.com (Unofficial)";
        public override string Description => "Unofficial Titlovi.com subtitle provider for Emby.";
        public override Guid Id => PluginId;

        public Stream GetThumbImage()
        {
            return GetType().Assembly.GetManifestResourceStream(GetType().Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    DisplayName = "Titlovi.com (Unofficial)",
                    Name = "EmbyTitloviConfigPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
                    EnableInMainMenu = true,
                    MenuSection = "server",
                    MenuIcon = "closed_caption"
                },
                new PluginPageInfo
                {
                    Name = "EmbyTitloviConfigPageJs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.js"
                }
            };
        }
    }
}
