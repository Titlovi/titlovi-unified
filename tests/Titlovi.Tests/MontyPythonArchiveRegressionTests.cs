using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class MontyPythonArchiveRegressionTests
    {
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

            var episodeNames = new[]
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

            var engine = TestData.Engine(api, new TestConfiguration());
            for (var episode = 1; episode <= 13; episode++)
            {
                var result = await engine.GetSubtitles(
                    TestData.MetadataId(
                        3842,
                        TitloviSubtitleType.Episode,
                        "Srpski",
                        1,
                        episode,
                        episodeNames[episode - 1],
                        0,
                        "sr"),
                    CancellationToken.None);

                Assert.Contains("MP" + episode.ToString("00"), Encoding.UTF8.GetString(result.Data));
            }
        }
    }
}
