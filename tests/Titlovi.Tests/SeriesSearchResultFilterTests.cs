using System.Collections.Generic;
using System.Linq;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class SeriesSearchResultFilterTests
    {
        [Fact]
        public void TheWire_RejectsWireInTheBlood()
        {
            var results = ApplyEpisodeFilter("The Wire", "Wire in the Blood");

            Assert.Empty(results);
        }

        [Fact]
        public void TheWire_PreservesExactSeriesTitle()
        {
            var results = ApplyEpisodeFilter("The Wire", "The Wire");

            Assert.Single(results);
        }

        [Fact]
        public void LocalizedAlternativeTitle_IsPreservedWhenThereIsNoClearCollision()
        {
            var results = ApplyEpisodeFilter("The Wire", "Žica");

            Assert.Single(results);
        }

        [Fact]
        public void ParentheticalQualifier_IsIgnored()
        {
            var results = ApplyEpisodeFilter("The Office", "The Office (US)");

            Assert.Single(results);
        }

        [Fact]
        public void BshLanguageSuffix_IsIgnoredBeforeSeriesComparison()
        {
            var results = ApplyEpisodeFilter("The Office", "The Office (US) — Hrvatski");

            Assert.Single(results);
        }

        [Fact]
        public void ClearFranchiseExtension_IsRejected()
        {
            var results = ApplyEpisodeFilter("NCIS", "NCIS: Los Angeles");

            Assert.Empty(results);
        }

        [Theory]
        [InlineData(
            "Dedictví aneb Kurvahosigutntag",
            "Dedictví aneb Kurvahosigutntag AKA The Inheritance or Fuckoffguysgoodday")]
        [InlineData(
            "The Inheritance or Fuckoffguysgoodday",
            "Dedictví aneb Kurvahosigutntag AKA The Inheritance or Fuckoffguysgoodday")]
        [InlineData(
            "Bröderna Lejonhjärta",
            "Bröderna Lejonhjärta AKA The Brothers Lionheart")]
        [InlineData(
            "The Brothers Lionheart",
            "Bröderna Lejonhjärta AKA The Brothers Lionheart")]
        [InlineData(
            "Honig im Kopf",
            "Honig im Kopf AKA Head Full of Honey")]
        [InlineData(
            "Head Full of Honey",
            "Honig im Kopf AKA Head Full of Honey")]
        public void AkaTitles_PreserveEitherAlias(string requestedTitle, string providerTitle)
        {
            Assert.False(SeriesSearchResultFilter.IsClearlyDifferentSeriesTitle(requestedTitle, providerTitle));

            var results = ApplyEpisodeFilter(requestedTitle, providerTitle);
            Assert.Single(results);
        }

        [Fact]
        public void AkaTitle_WithLanguageSuffix_PreservesMatchingAlias()
        {
            var results = ApplyEpisodeFilter(
                "Head Full of Honey",
                "Honig im Kopf AKA Head Full of Honey — Hrvatski");

            Assert.Single(results);
        }

        [Fact]
        public void AkaTitle_DoesNotDisableCrossSeriesProtection()
        {
            var results = ApplyEpisodeFilter(
                "The Wire",
                "Wire in the Blood AKA Sangre en la sangre");

            Assert.Empty(results);
        }

        [Fact]
        public void Movies_AreNotFilteredByEpisodeSeriesRule()
        {
            var input = new[]
            {
                new CoreRemoteSubtitleInfo { Name = "Wire in the Blood" }
            };

            var results = SeriesSearchResultFilter.Apply(
                input,
                TitloviContentType.Movie,
                "The Wire");

            Assert.Single(results);
        }

        private static List<CoreRemoteSubtitleInfo> ApplyEpisodeFilter(
            string requestedSeries,
            string providerTitle)
        {
            var input = new[]
            {
                new CoreRemoteSubtitleInfo { Name = providerTitle }
            };

            return SeriesSearchResultFilter.Apply(
                input,
                TitloviContentType.Episode,
                requestedSeries);
        }
    }
}
