using System;
using System.Reflection;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class EpisodeFilenameParserTests
    {
        private static readonly MethodInfo Parser = typeof(TitloviEngine).GetMethod(
            "TryParseEpisodeFromFilename", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo IsGrouped = typeof(TitloviEngine).GetMethod(
            "IsGroupedEpisodeFilename", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo GroupRange = typeof(TitloviEngine).GetMethod(
            "FilenameGroupMatchesEpisodeRange", BindingFlags.NonPublic | BindingFlags.Static);

        [Theory]
        [InlineData("Friends.S01E01.1080p.WEB-DL.srt", 1, 1)]
        [InlineData("Seinfeld - 01x01 - PILOT - Good News, Bad News.srt", 1, 1)]
        [InlineData("Seinfeld - 01x02 - Stakeout .srt", 1, 2)]
        [InlineData("01x02_-_sex_and_violence.srt", 1, 2)]
        [InlineData("season_01_blackadder_season_01_episode_01.srt", 1, 1)]
        [InlineData("black_adder_i_01.srt", 1, 1)]
        [InlineData("black_adder_iv_06.srt", 4, 6)]
        [InlineData("101 - Pilot.srt", 1, 1)]
        [InlineData("110 - Finale.srt", 1, 10)]
        [InlineData("1001 - Episode.srt", 10, 1)]
        public void Parser_RecognizesSeasonAndEpisode(string fileName, int expectedSeason, int expectedEpisode)
        {
            var parsed = Parse(fileName);
            Assert.True(parsed.Success);
            Assert.Equal(expectedSeason, parsed.Season);
            Assert.Equal(expectedEpisode, parsed.Episode);
        }

        [Theory]
        [InlineData("001 - The Seinfeld Chronicles - [DVD].avi.srt", 1)]
        [InlineData("002 - The Stakeout - [DVD].srt", 2)]
        [InlineData("12 - Finale.srt", 12)]
        [InlineData("friends_01_01.srt", 1)]
        [InlineData("Season 01/002 - The Stakeout.srt", 2)]
        [InlineData("Season 01\\002 - The Stakeout.srt", 2)]
        public void Parser_RecognizesEpisodeOnlyPackNames(string fileName, int expectedEpisode)
        {
            var parsed = Parse(fileName);
            Assert.True(parsed.Success);
            Assert.Null(parsed.Season);
            Assert.Equal(expectedEpisode, parsed.Episode);
        }

        [Theory]
        [InlineData("Ep 1,2,3.srt")]
        [InlineData("Episode 4+5+6.srt")]
        [InlineData("BlackAdder_I_123_ser.srt")]
        [InlineData("s2_123black.adder123.srt")]
        public void GroupedEpisodeNames_AreDetected(string fileName)
        {
            Assert.True((bool)IsGrouped.Invoke(null, new object[] { fileName }));
        }

        [Theory]
        [InlineData("Seinfeld.S01E01.srt")]
        [InlineData("001 - The Seinfeld Chronicles.srt")]
        [InlineData("black_adder_i_01.srt")]
        [InlineData("Ep 1,2,3/001 - Pilot.srt")]
        [InlineData("Ep 1,2,3\\001 - Pilot.srt")]
        public void OrdinaryEpisodeNames_AreNotGrouped(string fileName)
        {
            Assert.False((bool)IsGrouped.Invoke(null, new object[] { fileName }));
        }

        [Theory]
        [InlineData("Ep 1,2,3.srt", 1, 1, 3, true)]
        [InlineData("Ep 1,2,3.srt", 1, 1, 2, false)]
        [InlineData("Episode 4+5+6.srt", 1, 4, 6, true)]
        [InlineData("Episode 4+5+6.srt", 1, 4, 5, false)]
        [InlineData("BlackAdder_I_123_ser.srt", 1, 1, 3, true)]
        [InlineData("BlackAdder_II_456_ser.srt", 2, 4, 6, true)]
        [InlineData("BlackAdder_II_456_ser.srt", 1, 4, 6, false)]
        [InlineData("season_02_blackadder_123.srt", 2, 1, 3, true)]
        [InlineData("Season 02/Ep 1,2,3.srt", 2, 1, 3, true)]
        [InlineData("Season 02\\Ep 1,2,3.srt", 2, 1, 3, true)]
        public void GroupRange_MustExactlyMatchRequestedRange(
            string fileName, int season, int start, int end, bool expected)
        {
            var args = new object[] { fileName, season, start, end, string.Empty };
            var actual = (bool)GroupRange.Invoke(null, args);
            Assert.Equal(expected, actual);
        }

        private static (bool Success, int? Season, int Episode, string Format) Parse(string path)
        {
            Assert.NotNull(Parser);
            var args = new object[] { path, null, 0, string.Empty };
            var success = (bool)Parser.Invoke(null, args);
            return (success, (int?)args[1], (int)args[2], (string)args[3]);
        }
    }
}
