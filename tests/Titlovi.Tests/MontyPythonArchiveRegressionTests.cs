using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class MontyPythonArchiveRegressionTests
    {
        private static readonly string[] SeasonOneEpisodeNames =
        {
            "Whither Canada?",
            "Sex and Violence",
            "How to Recognise Different Types of Trees From Quite a Long Way Away",
            "Owl-Stretching Time",
            "Man's Crisis of Identity",
            "The BBC Entry For the Zinc Stoat of Budapest",
            "Oh, You're No Fun Anymore",
            "Full Frontal Nudity",
            "The Ant, an Introduction",
            "Untitled",
            "The Royal Philharmonic Orchestra Goes to the Bathroom",
            "The Naked Ant",
            "Intermission"
        };

        [Fact]
        public async Task Media3842_MixedLegacySeasonPack_SelectsAllThirteenEpisodes()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[3842] = TestData.Zip(
                ("Monty Python's Flying Circus - 1x13 - Intermission.sub", TestData.Srt("MP13")),
                ("m.python s1 ep12 The Naked Ant.sub", TestData.Srt("MP12")),
                ("m.python s1 ep11The Royal Philharmonic Orchestra.sub", TestData.Srt("MP11")),
                ("monty python s1 ep10 NO TITLE.sub", TestData.Srt("MP10")),
                ("monthy python s1 ep04Owl-Stretching Time.sub", TestData.Srt("MP04")),
                ("monthy python s1 ep5  Man's Crisis of Identity.sub", TestData.Srt("MP05")),
                ("monthy python s1 ep 07 Oh, You're No Fun Anymore.sub", TestData.Srt("MP07")),
                ("monthy python s1 ep 08Full Frontal Nudity.sub", TestData.Srt("MP08")),
                ("Monty Python - 1x03 - How to Recognise Different Types of Trees From Quite a Long Way Away.txt", TestData.Srt("MP03")),
                ("Monty Python - 1x02- Sex and Violence.txt", TestData.Srt("MP02")),
                ("Monty Python - 1x01 - Whither Canada.txt", TestData.Srt("MP01")),
                ("monthy python s1 ep06.  The BBC Entry For the Zinc Stoat of Budapest.sub", TestData.Srt("MP06")),
                ("09.  The Ant, an Introduction.sub", TestData.Srt("MP09")));

            await AssertSeasonOneEpisodes(api, 3842, 1, 13, "MP");
        }

        [Fact]
        public async Task Media54462_ExplicitSeasonPack_SelectsAllThirteenEpisodes()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[54462] = TestData.Zip(
                ("monty_python_-_s01e01_-_whither_canada.sub", TestData.Srt("M54462-01")),
                ("monty_python_-_s01e02_-_sex_and_violence.sub", TestData.Srt("M54462-02")),
                ("monty_python_-_s01e03_-_how_to_recognise_different_types_of_trees_from_quite_a_long_way_away.txt", TestData.Srt("M54462-03")),
                ("monty_python_-_s01e04_-_owl-stretching_time.sub", TestData.Srt("M54462-04")),
                ("monty_python_-_s01e05_-_mans_crisis_of_identity_in_the_latter_half_of_the_twentieth_century.sub", TestData.Srt("M54462-05")),
                ("monty_python_-_s01e06_-_the_bbc_entry_to_the_zinc_stoat_of_budapest.sub", TestData.Srt("M54462-06")),
                ("monty_python_-_s01e07_-_youre_no_fun_any_more.sub", TestData.Srt("M54462-07")),
                ("monty_python_-_s01e08_-_full_frontal_nudity.sub", TestData.Srt("M54462-08")),
                ("monty_python_-_s01e09_-_the_ant_an_introduction.sub", TestData.Srt("M54462-09")),
                ("monty_python_-_s01e10_-_untitled.sub", TestData.Srt("M54462-10")),
                ("monty_python_-_s01e11_-_the_royal_philharmonic_orchestra_goes_to_the_bathroom.sub", TestData.Srt("M54462-11")),
                ("monty_python_-_s01e12_-_the_naked_ant.sub", TestData.Srt("M54462-12")),
                ("monty_python_-_s01e13_-_intermission.sub", TestData.Srt("M54462-13")));

            await AssertSeasonOneEpisodes(api, 54462, 1, 13, "M54462-");
        }

        [Fact]
        public async Task Media3841_LeadingSeasonEpisodeFiles_SelectFirstFourEpisodes()
        {
            var api = new FakeTitloviApiClient();
            api.Downloads[3841] = TestData.Zip(
                ("Monty Python - 104 - Owl-Stretching Time.sub", TestData.Srt("M3841-04")),
                ("Monty Python - 102 - Sex and Violence.sub", TestData.Srt("M3841-02")),
                ("Monty Python - 103 - How to Recognise Different Types of Trees From Quite a Long Way Away.txt", TestData.Srt("M3841-03")),
                ("Monty Python - 101 - Whither Canada.sub", TestData.Srt("M3841-01")),
                ("MONTY PYTHON 10-12.SUB", TestData.Srt("GROUP-10-12")),
                ("MontyPython'sFlyingCircus14-16.SR.sub", TestData.Srt("GROUP-14-16")),
                ("MontyPython'sFlyingCircus07-09.SR.sub", TestData.Srt("GROUP-07-09")),
                ("MontyPython'sFlyingCircus17-19.SR.sub", TestData.Srt("GROUP-17-19")),
                ("MontyPython'sFlyingCircus04-06_SR.sub", TestData.Srt("GROUP-04-06")));

            await AssertSeasonOneEpisodes(api, 3841, 1, 4, "M3841-");
        }

        private static async Task AssertSeasonOneEpisodes(
            FakeTitloviApiClient api,
            int mediaId,
            int firstEpisode,
            int lastEpisode,
            string markerPrefix)
        {
            var engine = TestData.Engine(api, new TestConfiguration());
            for (var episode = firstEpisode; episode <= lastEpisode; episode++)
            {
                var result = await engine.GetSubtitles(
                    TestData.MetadataId(
                        mediaId,
                        TitloviSubtitleType.Episode,
                        "Srpski",
                        1,
                        episode,
                        SeasonOneEpisodeNames[episode - 1],
                        0,
                        "sr"),
                    CancellationToken.None);

                Assert.Contains(markerPrefix + episode.ToString("00"), Encoding.UTF8.GetString(result.Data));
            }
        }
    }
}
