using System.Linq;
using System.Text;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class EncodingTests
    {
        [Fact]
        public void Utf8WithoutBom_IsPreservedByteForByte()
        {
            var input = Encoding.UTF8.GetBytes("Čovek kaže: Šta ćeš?");
            var output = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(input, "Srpski", out var detected, out var converted);

            Assert.Equal("UTF-8", detected);
            Assert.False(converted);
            Assert.Equal(input, output);
        }

        [Fact]
        public void Utf8Bom_IsPreservedByteForByte()
        {
            var body = Encoding.UTF8.GetBytes("Đorđe");
            var input = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(body).ToArray();
            var output = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(input, "Srpski", out var detected, out var converted);

            Assert.Equal("UTF-8 BOM", detected);
            Assert.False(converted);
            Assert.Equal(input, output);
        }

        [Fact]
        public void Utf16LeBom_IsConvertedToUtf8WithoutBom()
        {
            var text = "Čovek kaže: Šta ćeš?";
            var body = Encoding.Unicode.GetBytes(text);
            var input = new byte[] { 0xFF, 0xFE }.Concat(body).ToArray();
            var output = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(input, "Srpski", out var detected, out var converted);

            Assert.Equal("UTF-16 LE", detected);
            Assert.True(converted);
            Assert.Equal(text, Encoding.UTF8.GetString(output));
            Assert.False(output.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }));
        }

        [Fact]
        public void Windows1250Serbian_IsDetectedAndConverted()
        {
            var input = TestData.Windows1250SerbianSrt();
            var output = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(input, "Srpski", out var detected, out var converted);
            var text = Encoding.UTF8.GetString(output);

            Assert.Equal("Windows-1250", detected);
            Assert.True(converted);
            Assert.Contains("Čovek kaže: Šta ćeš? Đorđe ide kući.", text);
        }

        [Fact]
        public void Windows1251SerbianCyrillic_IsDetectedWithScriptNeutralSerbianLabel()
        {
            // Excerpt from Heart of the Beast, Titlovi.com media ID 429245:
            // "НЕГДЕ НА АЉАСЦИ" / "СРЦЕ ЗВЕРИ" in CP1251.
            var input = new byte[]
            {
                205, 197, 195, 196, 197, 32, 205, 192, 32, 192, 138, 192, 209, 214, 200,
                13, 10,
                209, 208, 214, 197, 32, 199, 194, 197, 208, 200
            };

            var output = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(input, "Srpski", out var detected, out var converted);

            Assert.Equal("Windows-1251", detected);
            Assert.True(converted);
            Assert.Equal("НЕГДЕ НА АЉАСЦИ\r\nСРЦЕ ЗВЕРИ", Encoding.UTF8.GetString(output));
        }

        [Fact]
        public void Windows1251Macedonian_IsDetectedAndConverted()
        {
            // "СЕГА СМЕ ТУКА И ЌЕ ОДИМЕ ДОМА" in CP1251.
            // Includes the Macedonian letter Ќ (0x8D).
            var input = new byte[]
            {
                209, 197, 195, 192, 32, 209, 204, 197, 32, 210, 211, 202, 192, 32,
                200, 32, 141, 197, 32, 206, 196, 200, 204, 197, 32, 196, 206, 204, 192
            };

            var output = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(input, "Makedonski", out var detected, out var converted);

            Assert.Equal("Windows-1251", detected);
            Assert.True(converted);
            Assert.Equal("СЕГА СМЕ ТУКА И ЌЕ ОДИМЕ ДОМА", Encoding.UTF8.GetString(output));
        }

        [Fact]
        public void EmptyPayload_IsReturnedAsEmpty()
        {
            var output = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(new byte[0], "Srpski", out var detected, out var converted);
            Assert.Equal("Empty", detected);
            Assert.False(converted);
            Assert.Empty(output);
        }
    }
}
