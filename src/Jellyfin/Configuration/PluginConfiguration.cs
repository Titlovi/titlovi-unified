using MediaBrowser.Model.Plugins;
using Titlovi.Common;

namespace Jellyfin.Titlovi.Configuration
{
    public sealed class PluginConfiguration : BasePluginConfiguration
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public TitloviToken Token { get; set; }
        // Records that original-plugin settings were considered, including when newer
        // credentials took precedence. Prevents re-import after a user clears credentials.
        public bool LegacyCredentialsImportHandled { get; set; } = false;
        public bool ConvertDownloadedSubtitlesToUtf8 { get; set; } = false;
        public bool EnableDebugLogging { get; set; } = false;
        public bool IncludeBshAlternativeLanguages { get; set; } = false;
        public bool AcceptSrt { get; set; } = true;
        public bool AcceptSub { get; set; } = true;
        public bool AcceptTxt { get; set; } = true;
        public bool AcceptSsa { get; set; } = true;
        public bool AcceptAss { get; set; } = true;
    }
}
