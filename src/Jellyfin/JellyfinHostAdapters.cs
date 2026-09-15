using System;
using System.Text.Json;
using Titlovi.Common;

namespace Jellyfin.Titlovi
{
    internal sealed class JellyfinJsonDeserializer : ITitloviJsonDeserializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
    }

    internal sealed class JellyfinConfigurationProvider : ITitloviConfigurationProvider
    {
        private Configuration.PluginConfiguration Configuration
        {
            get
            {
                var plugin = Plugin.Instance ?? throw new InvalidOperationException("Titlovi plugin is not initialized.");
                return plugin.Configuration;
            }
        }

        public string Username => Configuration.Username;
        public string Password => Configuration.Password;
        public TitloviToken Token
        {
            get => Configuration.Token;
            set => Configuration.Token = value;
        }
        public bool ConvertDownloadedSubtitlesToUtf8 => Configuration.ConvertDownloadedSubtitlesToUtf8;
        public bool EnableDebugLogging => Configuration.EnableDebugLogging;
        public bool IncludeBshAlternativeLanguages => Configuration.IncludeBshAlternativeLanguages;
        public bool AcceptSrt => Configuration.AcceptSrt;
        public bool AcceptSub => Configuration.AcceptSub;
        public bool AcceptTxt => Configuration.AcceptTxt;
        public bool AcceptSsa => Configuration.AcceptSsa;
        public bool AcceptAss => Configuration.AcceptAss;

        public void Save()
        {
            var plugin = Plugin.Instance ?? throw new InvalidOperationException("Titlovi plugin is not initialized.");
            plugin.SaveConfiguration();
        }
    }
}
