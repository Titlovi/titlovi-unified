using System.Linq;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class LanguageMapTests
    {
        [Theory]
        [InlineData("sr", "Srpski")]
        [InlineData("srp", "Srpski")]
        [InlineData("serbian", "Srpski")]
        [InlineData("hr", "Hrvatski")]
        [InlineData("hrv", "Hrvatski")]
        [InlineData("bs", "Bosanski")]
        [InlineData("bos", "Bosanski")]
        [InlineData("en", "English")]
        [InlineData("mk", "Makedonski")]
        [InlineData("sl", "Slovenski")]
        [InlineData("cyr", "Cirilica")]
        public void ToProviderLanguage_MapsKnownCodes(string input, string expected)
        {
            Assert.Equal(expected, LanguageMap.ToProviderLanguage(input));
        }

        [Theory]
        [InlineData("Srpski", "sr", "srp")]
        [InlineData("Hrvatski", "hr", "hrv")]
        [InlineData("Bosanski", "bs", "bos")]
        [InlineData("English", "en", "eng")]
        [InlineData("Makedonski", "mk", "mkd")]
        [InlineData("Slovenski", "sl", "slv")]
        [InlineData("Cirilica", "sr", "srp")]
        public void ProviderLanguage_MapsToModernAndLegacyCodes(string input, string modern, string legacy)
        {
            Assert.Equal(modern, LanguageMap.FromProviderLanguage(input));
            Assert.Equal(legacy, LanguageMap.ToThreeLetterIso(input));
        }

        [Fact]
        public void SerbianAlternativeOrder_PrefersCroatian()
        {
            Assert.Equal(new[] { "Srpski", "Hrvatski", "Bosanski" },
                LanguageMap.GetSearchLanguages("sr", true).ToArray());
        }

        [Fact]
        public void CroatianAlternativeOrder_PrefersSerbian()
        {
            Assert.Equal(new[] { "Hrvatski", "Srpski", "Bosanski" },
                LanguageMap.GetSearchLanguages("hr", true).ToArray());
        }

        [Fact]
        public void BosnianAlternativeOrder_PrefersSerbian()
        {
            Assert.Equal(new[] { "Bosanski", "Srpski", "Hrvatski" },
                LanguageMap.GetSearchLanguages("bs", true).ToArray());
        }

        [Fact]
        public void AlternativesOff_SearchesOnlyPrimaryLanguage()
        {
            Assert.Equal(new[] { "Srpski" }, LanguageMap.GetSearchLanguages("sr", false).ToArray());
        }

        [Fact]
        public void Cyrillic_IsNotExpandedToBshAlternatives()
        {
            Assert.Equal(new[] { "Cirilica" }, LanguageMap.GetSearchLanguages("cyr", true).ToArray());
        }

        [Fact]
        public void UnknownLanguage_ReturnsNoProviderLanguages()
        {
            Assert.Empty(LanguageMap.GetSearchLanguages("zz", true));
        }
    }
}
