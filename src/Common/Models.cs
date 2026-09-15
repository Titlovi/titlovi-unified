using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Titlovi.Common
{
    public enum TitloviSubtitleType
    {
        None = 0,
        Movie = 1,
        Episode = 2
    }

    public enum TitloviContentType
    {
        Movie = 1,
        Episode = 2
    }

    public sealed class TitloviToken
    {
        public string Token { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public DateTime ExpirationDate { get; set; }

        public bool IsExpired => string.IsNullOrWhiteSpace(Token) || DateTime.Now >= ExpirationDate;
    }

    public sealed class TitloviSubtitle
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public TitloviSubtitleType Type { get; set; }
        public string Link { get; set; }
        public int Season { get; set; }
        public int Episode { get; set; }
        public int Special { get; set; }
        public string Lang { get; set; }
        public string LanguageCode { get; set; }
        public DateTime Date { get; set; }
        public int DownloadCount { get; set; }
        public float Rating { get; set; }
        public string Release { get; set; }

        // Search-only metadata used for deterministic result ordering. These fields
        // are not part of the Titlovi.com JSON contract and are populated locally.
        public int LanguagePriority { get; set; }
        public int MatchScore { get; set; }
    }

    public sealed class TitloviSearchPage
    {
        public int ResultsFound { get; set; }
        public int PagesAvailable { get; set; }
        public int CurrentPage { get; set; }
        public List<TitloviSubtitle> SubtitleResults { get; set; } = new List<TitloviSubtitle>();
    }

    public sealed class CoreSubtitleSearchRequest
    {
        public TitloviContentType ContentType { get; set; }
        public string Language { get; set; }
        public string MediaPath { get; set; }
        public string SeriesName { get; set; }
        public string Name { get; set; }
        public int? IndexNumber { get; set; }
        public int? IndexNumberEnd { get; set; }
        public int? ParentIndexNumber { get; set; }
        public int? ProductionYear { get; set; }
        public long? RuntimeTicks { get; set; }
        public bool IsPerfectMatch { get; set; }
        public IDictionary<string, string> ProviderIds { get; set; }
        public IDictionary<string, string> SeriesProviderIds { get; set; }
    }

    public sealed class CoreRemoteSubtitleInfo
    {
        public string Id { get; set; }
        public DateTime? DateCreated { get; set; }
        public int? DownloadCount { get; set; }
        public string Name { get; set; }
        public string ProviderName { get; set; }
        public float? CommunityRating { get; set; }
        public string Comment { get; set; }
        public string Format { get; set; }
        public string LanguageCode { get; set; }
        public string ThreeLetterIsoLanguageName { get; set; }
        public bool IsForced { get; set; }
        public bool IsHearingImpaired { get; set; }
    }

    public sealed class CoreSubtitleResponse
    {
        public string Format { get; set; }
        public bool IsForced { get; set; }
        public bool IsHearingImpaired { get; set; }
        public string LanguageCode { get; set; }
        public byte[] Data { get; set; }
    }

    internal interface ITitloviApiClient
    {
        Task<TitloviToken> GetTokenAsync(string username, string password, CancellationToken cancellationToken);

        Task<TitloviSearchPage> SearchAsync(
            string query,
            TitloviSubtitleType type,
            IEnumerable<int> seasons,
            IEnumerable<int> episodes,
            string imdbId,
            int page,
            string language,
            TitloviToken token,
            CancellationToken cancellationToken);

        Task<byte[]> DownloadAsync(int mediaId, TitloviSubtitleType type, CancellationToken cancellationToken);
    }

    public interface ITitloviJsonDeserializer
    {
        T Deserialize<T>(string json);
    }

    public interface ITitloviConfigurationProvider
    {
        string Username { get; }
        string Password { get; }
        TitloviToken Token { get; set; }
        bool ConvertDownloadedSubtitlesToUtf8 { get; }
        bool EnableDebugLogging { get; }
        bool IncludeBshAlternativeLanguages { get; }
        bool AcceptSrt { get; }
        bool AcceptSub { get; }
        bool AcceptTxt { get; }
        bool AcceptSsa { get; }
        bool AcceptAss { get; }
        void Save();
    }

    internal sealed class DownloadedSubtitleFile
    {
        public DownloadedSubtitleFile(string path, byte[] bytes)
        {
            Path = path ?? string.Empty;
            Bytes = bytes ?? Array.Empty<byte>();
        }

        public string Path { get; }
        public byte[] Bytes { get; }
    }

    internal sealed class SubtitleMetadata
    {
        public int Id { get; set; }
        public TitloviSubtitleType Type { get; set; }
        public string Language { get; set; }
        public string LanguageCode { get; set; }
        public int Season { get; set; }
        public int Episode { get; set; }
        public int EpisodeEnd { get; set; }
        public string EpisodeName { get; set; }
    }
}
