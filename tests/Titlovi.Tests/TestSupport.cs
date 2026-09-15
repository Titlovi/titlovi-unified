using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titlovi.Common;

namespace Titlovi.Tests
{
    internal sealed class TestConfiguration : ITitloviConfigurationProvider
    {
        public string Username { get; set; } = "test-user";
        public string Password { get; set; } = "test-password";
        public TitloviToken Token { get; set; } = new TitloviToken
        {
            Token = "test-token",
            UserId = 1,
            UserName = "test-user",
            ExpirationDate = DateTime.Now.AddHours(1)
        };

        public bool ConvertDownloadedSubtitlesToUtf8 { get; set; }
        public bool EnableDebugLogging { get; set; }
        public bool IncludeBshAlternativeLanguages { get; set; }
        public bool AcceptSrt { get; set; } = true;
        public bool AcceptSub { get; set; } = true;
        public bool AcceptTxt { get; set; } = true;
        public bool AcceptSsa { get; set; } = true;
        public bool AcceptAss { get; set; } = true;
        public int SaveCalls { get; private set; }

        public void Save() => SaveCalls++;
    }

    internal sealed class SearchCall
    {
        public string Query { get; set; }
        public TitloviSubtitleType Type { get; set; }
        public List<int> Seasons { get; set; }
        public List<int> Episodes { get; set; }
        public string ImdbId { get; set; }
        public int Page { get; set; }
        public string Language { get; set; }
    }

    internal sealed class FakeTitloviApiClient : ITitloviApiClient
    {
        public Func<SearchCall, TitloviSearchPage> SearchHandler { get; set; }
        public Dictionary<int, byte[]> Downloads { get; } = new Dictionary<int, byte[]>();
        public List<SearchCall> SearchCalls { get; } = new List<SearchCall>();
        public int TokenCalls { get; private set; }
        public List<int> DownloadCalls { get; } = new List<int>();
        public TitloviToken TokenToReturn { get; set; } = new TitloviToken
        {
            Token = "fresh-token",
            UserId = 2,
            UserName = "test-user",
            ExpirationDate = DateTime.Now.AddHours(2)
        };

        public Task<TitloviToken> GetTokenAsync(string username, string password, CancellationToken cancellationToken)
        {
            TokenCalls++;
            return Task.FromResult(TokenToReturn);
        }

        public Task<TitloviSearchPage> SearchAsync(
            string query,
            TitloviSubtitleType type,
            IEnumerable<int> seasons,
            IEnumerable<int> episodes,
            string imdbId,
            int page,
            string language,
            TitloviToken token,
            CancellationToken cancellationToken)
        {
            var call = new SearchCall
            {
                Query = query,
                Type = type,
                Seasons = seasons == null ? null : seasons.ToList(),
                Episodes = episodes == null ? null : episodes.ToList(),
                ImdbId = imdbId,
                Page = page,
                Language = language
            };
            SearchCalls.Add(call);
            return Task.FromResult(SearchHandler == null ? EmptyPage() : SearchHandler(call));
        }

        public Task<byte[]> DownloadAsync(int mediaId, TitloviSubtitleType type, CancellationToken cancellationToken)
        {
            DownloadCalls.Add(mediaId);
            byte[] payload;
            if (!Downloads.TryGetValue(mediaId, out payload))
                throw new InvalidOperationException("No fake download payload registered for mediaId=" + mediaId + ".");
            return Task.FromResult(payload);
        }

        public static TitloviSearchPage EmptyPage()
        {
            return new TitloviSearchPage
            {
                ResultsFound = 0,
                PagesAvailable = 0,
                CurrentPage = 1,
                SubtitleResults = new List<TitloviSubtitle>()
            };
        }

        public static TitloviSearchPage Page(params TitloviSubtitle[] subtitles)
        {
            var list = subtitles == null ? new List<TitloviSubtitle>() : subtitles.ToList();
            return new TitloviSearchPage
            {
                ResultsFound = list.Count,
                PagesAvailable = 1,
                CurrentPage = 1,
                SubtitleResults = list
            };
        }
    }

    internal static class TestData
    {
        public static TitloviSubtitle Subtitle(
            int id,
            string title,
            string language,
            string release = "",
            int season = 0,
            int episode = 0,
            int downloads = 0,
            float rating = 0,
            TitloviSubtitleType type = TitloviSubtitleType.Movie)
        {
            return new TitloviSubtitle
            {
                Id = id,
                Title = title,
                Lang = language,
                Release = release,
                Season = season,
                Episode = episode,
                DownloadCount = downloads,
                Rating = rating,
                Type = type,
                Date = new DateTime(2020, 1, 1)
            };
        }

        public static string MetadataId(
            int id,
            TitloviSubtitleType type,
            string providerLanguage,
            int season,
            int episode,
            string episodeName = "",
            int episodeEnd = 0,
            string languageCode = "")
        {
            var raw = string.Join("\t", new[]
            {
                id.ToString(),
                ((int)type).ToString(),
                providerLanguage ?? string.Empty,
                season.ToString(),
                episode.ToString(),
                (episodeName ?? string.Empty).Replace("\t", " "),
                episodeEnd.ToString(),
                languageCode ?? string.Empty
            });
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        public static string[] DecodeMetadataId(string encoded)
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            return raw.Split('\t');
        }

        public static byte[] Srt(string marker)
        {
            return Encoding.UTF8.GetBytes("1\r\n00:00:00,000 --> 00:00:01,000\r\n" + marker + "\r\n");
        }

        public static byte[] Zip(params (string Path, byte[] Data)[] entries)
        {
            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var item in entries)
                    {
                        var entry = archive.CreateEntry(item.Path, CompressionLevel.Fastest);
                        using (var stream = entry.Open())
                            stream.Write(item.Data, 0, item.Data.Length);
                    }
                }
                return ms.ToArray();
            }
        }

        public static byte[] Windows1250SerbianSrt()
        {
            // "Čovek kaže: Šta ćeš? Đorđe ide kući." encoded as Windows-1250.
            var prefix = Encoding.ASCII.GetBytes("1\r\n00:00:00,000 --> 00:00:01,000\r\n");
            var text = new byte[]
            {
                0xC8, (byte)'o', (byte)'v', (byte)'e', (byte)'k', (byte)' ',
                (byte)'k', (byte)'a', 0x9E, (byte)'e', (byte)':', (byte)' ',
                0x8A, (byte)'t', (byte)'a', (byte)' ', 0xE6, (byte)'e', 0x9A, (byte)'?', (byte)' ',
                0xD0, (byte)'o', (byte)'r', 0xF0, (byte)'e', (byte)' ',
                (byte)'i', (byte)'d', (byte)'e', (byte)' ', (byte)'k', (byte)'u', 0xE6, (byte)'i', (byte)'.',
                (byte)'\r', (byte)'\n'
            };
            return prefix.Concat(text).ToArray();
        }

        public static TitloviEngine Engine(FakeTitloviApiClient api, TestConfiguration config)
        {
            return new TitloviEngine(api, config);
        }
    }
}
