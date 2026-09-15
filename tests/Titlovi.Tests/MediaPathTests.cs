using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class MediaPathTests
    {
        [Theory]
        [InlineData("/mnt/media/Theatrical/Movies/Movie/Movie.1080p.BluRay.Extended.x265-GROUP.mkv")]
        [InlineData("D:\\Media\\Theatrical\\Movies\\Movie\\Movie.1080p.BluRay.Extended.x265-GROUP.mkv")]
        public async Task ReleaseRanking_UsesMediaFilenameInsteadOfParentDirectoryTokens(string mediaPath)
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = _ => FakeTitloviApiClient.Page(
                    TestData.Subtitle(3001, "Movie", "English", "1080p.BluRay.Extended.x265-GROUP", downloads: 1),
                    TestData.Subtitle(3002, "Movie", "English", "1080p.BluRay.Theatrical.x265-GROUP", downloads: 10000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "en",
                MediaPath = mediaPath
            }, CancellationToken.None)).ToList();

            Assert.Equal("1080p.BluRay.Extended.x265-GROUP", results[0].Comment);
        }

        [Theory]
        [InlineData("/mnt/media/tv/Seinfeld/Season 07/Seinfeld.S07E16.1080p.WEBRip.x265-RARBG.mkv")]
        [InlineData("/media/tv/Seinfeld/Season 07/Seinfeld.S07E16.1080p.WEBRip.x265-RARBG.mkv")]
        public async Task LinuxServerMediaPath_PreservesReleaseRanking(string mediaPath)
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = _ => FakeTitloviApiClient.Page(
                    TestData.Subtitle(3011, "Seinfeld", "English", "1080p.WEBRip.x265-RARBG", downloads: 1),
                    TestData.Subtitle(3012, "Seinfeld", "English", "1080p.BluRay.x265-RARBG", downloads: 10000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Episode,
                SeriesName = "Seinfeld",
                Name = "The Doll",
                ParentIndexNumber = 7,
                IndexNumber = 16,
                Language = "en",
                MediaPath = mediaPath
            }, CancellationToken.None)).ToList();

            Assert.Equal("1080p.WEBRip.x265-RARBG", results[0].Comment);
        }
    }
}
