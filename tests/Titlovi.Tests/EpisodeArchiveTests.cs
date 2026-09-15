using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class EpisodeArchiveTests
    {
        [Fact]
        public async Task Seinfeld_01xPack_SelectsRequestedEpisode()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[32882] = TestData.Zip(
                ("Seinfeld - 01x01 - PILOT - Good News, Bad News.srt", TestData.Srt("E1")),
                ("Seinfeld - 01x02 - Stakeout.srt", TestData.Srt("E2")),
                ("Seinfeld - 01x03 - Robbery.srt", TestData.Srt("E3")),
                ("Seinfeld - 01x04 - Male Unbonding.srt", TestData.Srt("E4")),
                ("Seinfeld - 01x05 - Stock Tip.srt", TestData.Srt("E5")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(32882, TitloviSubtitleType.Episode, "Srpski", 1, 1, "The Seinfeld Chronicles", 0, "sr"),
                CancellationToken.None);

            Assert.Contains("E1", Encoding.UTF8.GetString(result.Data));
            Assert.DoesNotContain("E2", Encoding.UTF8.GetString(result.Data));
            Assert.Equal("sr", result.LanguageCode);
        }

        [Fact]
        public async Task Seinfeld_TrailingSpaceFilename_SelectsStakeout()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[14796] = TestData.Zip(
                ("Seinfeld - 01x01 - PILOT - Good News, Bad News.srt", TestData.Srt("E1")),
                ("Seinfeld - 01x02 - Stakeout .srt", TestData.Srt("E2")),
                ("Seinfeld - 01x03 - Robbery.srt", TestData.Srt("E3")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(14796, TitloviSubtitleType.Episode, "Hrvatski", 1, 2, "The Stakeout", 0, "hr"),
                CancellationToken.None);

            Assert.Contains("E2", Encoding.UTF8.GetString(result.Data));
            Assert.Equal("hr", result.LanguageCode);
        }

        [Fact]
        public async Task Seinfeld_LeadingOrdinalDvdPack_SelectsRequestedEpisode()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1001] = TestData.Zip(
                ("001 - The Seinfeld Chronicles - [DVD].avi.srt", TestData.Srt("ORD1")),
                ("002 - The Stakeout - [DVD].srt", TestData.Srt("ORD2")),
                ("003 - The Robbery - [DVD].srt", TestData.Srt("ORD3")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1001, TitloviSubtitleType.Episode, "Srpski", 1, 2, "The Stakeout", 0, "sr"),
                CancellationToken.None);

            Assert.Contains("ORD2", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task FullSeriesPack_WithSeasonFolders_SelectsExactSeasonAndEpisode()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[382914] = TestData.Zip(
                ("Seinfeld.1080p.WEBRip.x265-RARBG/1/Seinfeld.S01E01.1080p.WEBRip.x265-RARBG.srt", TestData.Srt("S1E1")),
                ("Seinfeld.1080p.WEBRip.x265-RARBG/7/Seinfeld.S07E16.1080p.WEBRip.x265-RARBG.srt", TestData.Srt("S7E16")),
                ("Seinfeld.1080p.WEBRip.x265-RARBG/9/Seinfeld.S09E01.1080p.WEBRip.x265-RARBG.srt", TestData.Srt("S9E1")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(382914, TitloviSubtitleType.Episode, "Hrvatski", 7, 16, "The Doll", 0, "hr"),
                CancellationToken.None);

            Assert.Contains("S7E16", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task EpisodeTitleFallback_WorksWhenPackHasNoUsableNumbers()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1002] = TestData.Zip(
                ("The Seinfeld Chronicles.srt", TestData.Srt("TITLE1")),
                ("The Stakeout.srt", TestData.Srt("TITLE2")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1002, TitloviSubtitleType.Episode, "Srpski", 1, 2, "The Stakeout", 0, "sr"),
                CancellationToken.None);

            Assert.Contains("TITLE2", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task SingleOrdinarySubtitle_IsSafeFallback()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1003] = TestData.Zip(("odd-old-name.srt", TestData.Srt("ONLY")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1003, TitloviSubtitleType.Episode, "Srpski", 3, 7, "Whatever", 0, "sr"),
                CancellationToken.None);

            Assert.Contains("ONLY", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task SingleGroupedSubtitle_IsNeverFallbackForSingleEpisode()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1004] = TestData.Zip(("Ep 1,2,3.srt", TestData.Srt("GROUP")));

            await Assert.ThrowsAsync<InvalidOperationException>(() => TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1004, TitloviSubtitleType.Episode, "Srpski", 1, 1, "Pilot", 0, "sr"),
                CancellationToken.None));
        }

        [Fact]
        public async Task MultiEpisodeRequest_SelectsExactEpisodeList()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1005] = TestData.Zip(
                ("Ep 1,2.srt", TestData.Srt("PAIR")),
                ("Ep 1,2,3.srt", TestData.Srt("TRIPLE")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1005, TitloviSubtitleType.Episode, "Srpski", 1, 1, "Pilot", 3, "sr"),
                CancellationToken.None);

            Assert.Contains("TRIPLE", Encoding.UTF8.GetString(result.Data));
            Assert.DoesNotContain("PAIR", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task Blackadder_RomanSeasonBundle_SelectsExactRange()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1006] = TestData.Zip(
                ("BlackAdder_I_123_ser.srt", TestData.Srt("I123")),
                ("BlackAdder_II_123_ser.srt", TestData.Srt("II123")));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1006, TitloviSubtitleType.Episode, "Srpski", 1, 1, "", 3, "sr"),
                CancellationToken.None);

            Assert.Contains("I123", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task MultiEpisodeRequest_RejectsNonExactBundle()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1007] = TestData.Zip(("Ep 1,2,3.srt", TestData.Srt("TRIPLE")));

            await Assert.ThrowsAsync<InvalidOperationException>(() => TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1007, TitloviSubtitleType.Episode, "Srpski", 1, 1, "", 2, "sr"),
                CancellationToken.None));
        }

        [Fact]
        public async Task NestedZip_SelectsEpisodeFromInnerArchive()
        {
            var inner = TestData.Zip(
                ("Friends.S01E01.srt", TestData.Srt("INNER1")),
                ("Friends.S01E02.srt", TestData.Srt("INNER2")));
            var api = new FakeTitloviApiClient();
            api.Downloads[1008] = TestData.Zip(("Season 01.zip", inner));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1008, TitloviSubtitleType.Episode, "Srpski", 1, 2, "", 0, "sr"),
                CancellationToken.None);

            Assert.Contains("INNER2", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task RawSrtPayload_IsAcceptedWithoutZip()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1009] = TestData.Srt("RAW");

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1009, TitloviSubtitleType.Movie, "Srpski", 0, 0, "", 0, "sr"),
                CancellationToken.None);

            Assert.Equal("srt", result.Format);
            Assert.Contains("RAW", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task Windows1250Payload_IsConvertedToUtf8ThroughFullDownloadFlow()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1010] = TestData.Windows1250SerbianSrt();
            var config = new TestConfiguration { ConvertDownloadedSubtitlesToUtf8 = true };

            var result = await TestData.Engine(api, config).GetSubtitles(
                TestData.MetadataId(1010, TitloviSubtitleType.Movie, "Srpski", 0, 0, "", 0, "sr"),
                CancellationToken.None);

            Assert.Contains("Čovek kaže: Šta ćeš? Đorđe ide kući.", Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public async Task StoredCroatianLanguageCode_SurvivesDownload()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1011] = TestData.Srt("HR");

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1011, TitloviSubtitleType.Movie, "Hrvatski", 0, 0, "", 0, "hr"),
                CancellationToken.None);

            Assert.Equal("hr", result.LanguageCode);
        }

        [Theory]
        [InlineData("subtitle.sub", "{1}{25}Hello", "sub")]
        [InlineData("subtitle.ssa", "[Script Info]\n[V4 Styles]\n[Events]\nDialogue: 0,0:00:00.00,0:00:01.00,Default,,0,0,0,,Hello", "ssa")]
        [InlineData("subtitle.ass", "[Script Info]\n[V4+ Styles]\n[Events]\nDialogue: 0,0:00:00.00,0:00:01.00,Default,,0,0,0,,Hello", "ass")]
        [InlineData("subtitle.txt", "1\n00:00:00,000 --> 00:00:01,000\nHello", "srt")]
        public async Task SupportedFormats_AreDetected(string fileName, string content, string expectedFormat)
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1012] = TestData.Zip((fileName, Encoding.UTF8.GetBytes(content)));

            var result = await TestData.Engine(api, new TestConfiguration()).GetSubtitles(
                TestData.MetadataId(1012, TitloviSubtitleType.Movie, "English", 0, 0, "", 0, "en"),
                CancellationToken.None);

            Assert.Equal(expectedFormat, result.Format);
        }

        [Fact]
        public async Task DisabledFormat_IsFilteredOut()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[1013] = TestData.Zip(("subtitle.srt", TestData.Srt("NOPE")));
            var config = new TestConfiguration { AcceptSrt = false };

            await Assert.ThrowsAsync<InvalidOperationException>(() => TestData.Engine(api, config).GetSubtitles(
                TestData.MetadataId(1013, TitloviSubtitleType.Movie, "Srpski", 0, 0, "", 0, "sr"),
                CancellationToken.None));
        }
    }
}
