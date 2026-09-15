using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Titlovi.Common;

namespace Emby.Titlovi
{
    public sealed class TitloviSubtitleProvider : ISubtitleProvider, IHasOrder
    {
        private readonly TitloviEngine _engine;

        public TitloviSubtitleProvider(IJsonSerializer jsonSerializer)
        {
            _engine = new TitloviEngine(
                new EmbyJsonDeserializer(jsonSerializer),
                new EmbyConfigurationProvider());
        }

        public string Name => "Titlovi.com (Unofficial)";
        public int Order => 0;

        public IEnumerable<VideoContentType> SupportedMediaTypes =>
            new[] { VideoContentType.Movie, VideoContentType.Episode };

        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(
            SubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var coreRequest = new CoreSubtitleSearchRequest
            {
                ContentType = request.ContentType == VideoContentType.Episode
                    ? TitloviContentType.Episode
                    : TitloviContentType.Movie,
                Language = request.Language,
                MediaPath = request.MediaPath,
                SeriesName = request.SeriesName,
                Name = request.Name,
                IndexNumber = request.IndexNumber,
                IndexNumberEnd = request.IndexNumberEnd,
                ParentIndexNumber = request.ParentIndexNumber,
                ProductionYear = request.ProductionYear,
                RuntimeTicks = request.RuntimeTicks,
                IsPerfectMatch = request.IsPerfectMatch,
                ProviderIds = CopyIds(request.ProviderIds),
                SeriesProviderIds = CopyIds(request.SeriesProviderIds)
            };

            var results = await _engine.Search(coreRequest, cancellationToken).ConfigureAwait(false);
            return results.Select(ToEmbyResult).ToList();
        }

        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            var response = await _engine.GetSubtitles(id, cancellationToken).ConfigureAwait(false);
            return new SubtitleResponse
            {
                Format = response.Format,
                IsForced = response.IsForced,
                IsHearingImpaired = response.IsHearingImpaired,
                Language = response.LanguageCode,
                Stream = new MemoryStream(response.Data ?? Array.Empty<byte>(), writable: false)
            };
        }

        private RemoteSubtitleInfo ToEmbyResult(CoreRemoteSubtitleInfo result)
        {
#pragma warning disable 0618 // Kept for compatibility with Emby versions that still expose the legacy ISO field.
            return new RemoteSubtitleInfo
            {
                Id = result.Id,
                DateCreated = result.DateCreated,
                DownloadCount = result.DownloadCount,
                Name = result.Name,
                ProviderName = Name,
                CommunityRating = result.CommunityRating,
                Comment = result.Comment,
                Format = result.Format,
                Language = result.LanguageCode,
                ThreeLetterISOLanguageName = result.ThreeLetterIsoLanguageName,
                IsForced = result.IsForced,
                IsHearingImpaired = result.IsHearingImpaired
            };
#pragma warning restore 0618
        }

        private static IDictionary<string, string> CopyIds(IDictionary<string, string> source)
        {
            return source == null
                ? null
                : new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);
        }
    }
}
