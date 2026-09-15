using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class CrossPlatformPathTests
    {
        [Theory]
        [InlineData("/")]
        [InlineData("\\")]
        public async Task ArchiveEpisodeOrdinal_IsIndependentOfPathSeparator(string separator)
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[2001] = TestData.Zip(
                ("Season 01" + separator + "001 - Pilot.srt", TestData.Srt("E1")),
                ("Season 01" + separator + "002 - The Stakeout.srt", TestData.Srt("E2")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(2001, TitloviSubtitleType.Episode, "Srpski", 1, 2, "The Stakeout", 0, "sr"),
                CancellationToken.None);

            var text = Encoding.UTF8.GetString(result.Data);
            Assert.Contains("E2", text);
            Assert.DoesNotContain("E1", text);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("\\")]
        public async Task ArchiveEpisodeTitleFallback_IsIndependentOfPathSeparator(string separator)
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[2002] = TestData.Zip(
                ("Season 01" + separator + "The Seinfeld Chronicles.srt", TestData.Srt("TITLE1")),
                ("Season 01" + separator + "The Stakeout.srt", TestData.Srt("TITLE2")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(2002, TitloviSubtitleType.Episode, "Srpski", 1, 2, "The Stakeout", 0, "sr"),
                CancellationToken.None);

            var text = Encoding.UTF8.GetString(result.Data);
            Assert.Contains("TITLE2", text);
            Assert.DoesNotContain("TITLE1", text);
        }
    }
}
