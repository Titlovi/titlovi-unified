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
        public void EmptyPayload_IsReturnedAsEmpty()
        {
            var output = SubtitleEncodingConverter.ConvertToUtf8IfNeeded(new byte[0], "Srpski", out var detected, out var converted);
            Assert.Equal("Empty", detected);
            Assert.False(converted);
            Assert.Empty(output);
        }
    }
}
