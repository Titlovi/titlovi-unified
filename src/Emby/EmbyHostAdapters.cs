using System;
using MediaBrowser.Model.Serialization;
using Titlovi.Common;

namespace Emby.Titlovi
{
    internal sealed class EmbyJsonDeserializer : ITitloviJsonDeserializer
    {
        private readonly IJsonSerializer _json;

        public EmbyJsonDeserializer(IJsonSerializer json)
        {
            _json = json ?? throw new ArgumentNullException(nameof(json));
        }

        public T Deserialize<T>(string json)
        {
            return (T)_json.DeserializeFromString(json, typeof(T));
        }
    }

    internal sealed class EmbyConfigurationProvider : ITitloviConfigurationProvider
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
