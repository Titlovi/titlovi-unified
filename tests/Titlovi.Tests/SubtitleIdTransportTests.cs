using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class SubtitleIdTransportTests
    {
        [Theory]
        [InlineData(3841)]
        [InlineData(3842)]
        public void WhitherCanada_ProblematicMetadataIds_AreMadeRouteSafeAndRoundTrip(int mediaId)
        {
            var internalId = TestData.MetadataId(
                mediaId,
                TitloviSubtitleType.Episode,
                "Srpski",
                1,
                1,
                "Whither Canada?",
                0,
                "sr");

            Assert.Contains("/", internalId);

            var routeSafeId = SubtitleIdTransport.ToUrlSafe(internalId);

            Assert.DoesNotContain("/", routeSafeId);
            Assert.DoesNotContain("+", routeSafeId);
            Assert.DoesNotContain("=", routeSafeId);
            Assert.Equal(internalId, SubtitleIdTransport.FromUrlSafe(routeSafeId));
        }

        [Fact]
        public void RouteSafeMetadataId_StillRoundTrips()
        {
            var internalId = TestData.MetadataId(
                54462,
                TitloviSubtitleType.Episode,
                "Srpski",
                1,
                1,
                "Whither Canada?",
                0,
                "sr");

            var routeSafeId = SubtitleIdTransport.ToUrlSafe(internalId);

            Assert.DoesNotContain("/", routeSafeId);
            Assert.DoesNotContain("+", routeSafeId);
            Assert.DoesNotContain("=", routeSafeId);
            Assert.Equal(internalId, SubtitleIdTransport.FromUrlSafe(routeSafeId));
        }

        [Fact]
        public void LegacyStandardBase64Id_IsAcceptedUnchangedOnDecode()
        {
            var internalId = TestData.MetadataId(
                32882,
                TitloviSubtitleType.Episode,
                "Srpski",
                1,
                1,
                "The Seinfeld Chronicles",
                0,
                "sr");

            Assert.Equal(internalId, SubtitleIdTransport.FromUrlSafe(internalId));
        }
    }
}
