using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class SubtitleIdTransportTests
    {
        [Fact]
        public void WhitherCanada_MetadataId_IsMadeRouteSafeAndRoundTrips()
        {
            var internalId = TestData.MetadataId(
                3842,
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
