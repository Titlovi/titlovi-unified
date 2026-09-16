using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Providers;
using Titlovi.Common;

namespace Jellyfin.Titlovi
{
    public sealed class TitloviSubtitleProvider : ISubtitleProvider
    {
        private readonly TitloviEngine _engine;
        private readonly ILibraryManager _libraryManager;

        public TitloviSubtitleProvider(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
            _engine = new TitloviEngine(
                new JellyfinJsonDeserializer(),
                new JellyfinConfigurationProvider());
        }

        public string Name => "Titlovi.com (Unofficial)";

        public IEnumerable<VideoContentType> SupportedMediaTypes =>
            new[] { VideoContentType.Movie, VideoContentType.Episode };

        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(
            SubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var seriesProviderIds = TryGetSeriesProviderIds(request);

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
                SeriesProviderIds = seriesProviderIds
            };

            var results = await _engine.Search(coreRequest, cancellationToken).ConfigureAwait(false);
            var requestedTitle = coreRequest.ContentType == TitloviContentType.Episode
                ? coreRequest.SeriesName
                : coreRequest.Name;
            var filteredResults = TitleSearchResultFilter.Apply(
                results,
                coreRequest.ContentType,
                requestedTitle);
            return filteredResults.Select(ToJellyfinResult).ToList();
        }

        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            var internalId = SubtitleIdTransport.FromUrlSafe(id);
            var response = await _engine.GetSubtitles(internalId, cancellationToken).ConfigureAwait(false);
            return new SubtitleResponse
            {
                Format = response.Format,
                IsForced = response.IsForced,
                IsHearingImpaired = response.IsHearingImpaired,
                Language = response.LanguageCode,
                Stream = new MemoryStream(response.Data ?? Array.Empty<byte>(), writable: false)
            };
        }


        private IDictionary<string, string> TryGetSeriesProviderIds(SubtitleSearchRequest request)
        {
            if (request.ContentType != VideoContentType.Episode || string.IsNullOrWhiteSpace(request.MediaPath))
                return null;

            try
            {
                var episode = _libraryManager.FindByPath(request.MediaPath, false) as Episode;
                var series = episode?.Series;
                return series == null ? null : CopyIds(series.ProviderIds);
            }
            catch (Exception ex)
            {
                TitloviDiagnostics.Debug(
                    "Jellyfin series provider-id lookup failed for mediaPath='"
                    + (request.MediaPath ?? string.Empty) + "': " + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        private RemoteSubtitleInfo ToJellyfinResult(CoreRemoteSubtitleInfo result)
        {
            return new RemoteSubtitleInfo
            {
                Id = SubtitleIdTransport.ToUrlSafe(result.Id),
                DateCreated = result.DateCreated,
                DownloadCount = result.DownloadCount,
                Name = result.Name,
                ProviderName = Name,
                CommunityRating = result.CommunityRating,
                Comment = result.Comment,
                Format = result.Format,
                ThreeLetterISOLanguageName = result.ThreeLetterIsoLanguageName,
                Forced = result.IsForced,
                HearingImpaired = result.IsHearingImpaired
            };
        }

        private static IDictionary<string, string> CopyIds(IDictionary<string, string> source)
        {
            return source == null
                ? null
                : new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);
        }
    }
}
