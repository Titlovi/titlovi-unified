using System.Collections.Generic;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class TitleSearchResultFilterTests
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
        public void BshLanguageSuffix_IsIgnoredBeforeTitleComparison()
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
            Assert.False(TitleSearchResultFilter.IsClearlyDifferentTitle(requestedTitle, providerTitle));

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
        public void AkaTitle_DoesNotDisableCrossTitleProtection()
        {
            var results = ApplyEpisodeFilter(
                "The Wire",
                "Wire in the Blood AKA Sangre en la sangre");

            Assert.Empty(results);
        }

        [Fact]
        public void StandaloneAkaTitle_IsNotTreatedAsAliasSeparator()
        {
            Assert.False(TitleSearchResultFilter.IsClearlyDifferentTitle("AKA", "AKA"));

            var results = ApplyEpisodeFilter("AKA", "AKA");
            Assert.Single(results);
        }

        [Fact]
        public void MovieNamedAka_IsReturnedUnchanged()
        {
            var results = ApplyMovieFilter("AKA", "AKA");

            Assert.Single(results);
            Assert.Equal("AKA", results[0].Name);
        }

        [Fact]
        public void RebeloveAkaRebels_RejectsRebelRidgeForMovie()
        {
            var results = ApplyMovieFilter("Rebelové AKA Rebels", "Rebel Ridge");

            Assert.Empty(results);
        }

        [Theory]
        [InlineData("Rebelové")]
        [InlineData("Rebels")]
        public void RebeloveAkaRebels_PreservesEitherMovieAlias(string requestedTitle)
        {
            var results = ApplyMovieFilter(requestedTitle, "Rebelové AKA Rebels");

            Assert.Single(results);
        }

        [Fact]
        public void MovieLocalizedAlternativeTitle_IsPreservedWhenThereIsNoClearCollision()
        {
            var results = ApplyMovieFilter("La vita è bella", "Life Is Beautiful");

            Assert.Single(results);
        }

        [Fact]
        public void SimplePluralDifferenceAlone_IsNotEnoughToRejectSameLengthTitle()
        {
            Assert.False(TitleSearchResultFilter.IsClearlyDifferentTitle("Rebels", "Rebel"));
        }

        private static List<CoreRemoteSubtitleInfo> ApplyEpisodeFilter(
            string requestedSeries,
            string providerTitle)
        {
            return ApplyFilter(TitloviContentType.Episode, requestedSeries, providerTitle);
        }

        private static List<CoreRemoteSubtitleInfo> ApplyMovieFilter(
            string requestedMovie,
            string providerTitle)
        {
            return ApplyFilter(TitloviContentType.Movie, requestedMovie, providerTitle);
        }

        private static List<CoreRemoteSubtitleInfo> ApplyFilter(
            TitloviContentType contentType,
            string requestedTitle,
            string providerTitle)
        {
            var input = new[]
            {
                new CoreRemoteSubtitleInfo { Name = providerTitle }
            };

            return TitleSearchResultFilter.Apply(
                input,
                contentType,
                requestedTitle);
        }
    }
}
