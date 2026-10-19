using System.Linq;
using System.Net.Http;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class SearchRequestHeaderTests
    {
        [Theory]
        [InlineData("titlovi-jellyfin")]
        [InlineData("emby-titlovi")]
        public void SearchRequest_UsesHostSpecificXAppHeader(string appIdentifier)
        {
            using (var request = TitloviApiClient.CreateSearchRequest(
                "https://kodi.titlovi.com/api/subtitles/search?query=Movie",
                appIdentifier))
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/subtitles/search", request.RequestUri.AbsolutePath);
                Assert.True(request.Headers.TryGetValues("X-App", out var values));
                Assert.Equal(new[] { appIdentifier }, values.ToArray());
            }
        }
    }
}
