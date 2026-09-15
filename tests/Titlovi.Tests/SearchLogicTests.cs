using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Titlovi.Common;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class SearchLogicTests
    {
        [Fact]
        public async Task BshAlternatives_AreOrderedByRequestedLanguageBeforePopularity()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call =>
                {
                    if (call.Language == "Srpski")
                        return FakeTitloviApiClient.Page(TestData.Subtitle(1, "Movie", "Srpski", downloads: 10));
                    if (call.Language == "Bosanski")
                        return FakeTitloviApiClient.Page(TestData.Subtitle(2, "Movie", "Bosanski", downloads: 10000));
                    if (call.Language == "Hrvatski")
                        return FakeTitloviApiClient.Page(TestData.Subtitle(3, "Movie", "Hrvatski", downloads: 50000));
                    return FakeTitloviApiClient.EmptyPage();
                }
            };
            var config = new TestConfiguration { IncludeBshAlternativeLanguages = true };
            var request = new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "sr",
                MediaPath = "Movie.1080p.WEB-DL.mkv"
            };

            var results = (await TestData.Engine(api, config).Search(request, CancellationToken.None)).ToList();

            Assert.Equal(new[] { "sr", "bs", "hr" }, results.Select(x => x.LanguageCode).ToArray());
            Assert.Contains("Srpski", results[0].Name);
            Assert.Contains("Bosanski", results[1].Name);
            Assert.Contains("Hrvatski", results[2].Name);
        }

        [Fact]
        public async Task ApiReportedActualLanguage_IsPreservedEvenWhenSearchLanguageDiffers()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => call.Language == "Srpski"
                    ? FakeTitloviApiClient.Page(TestData.Subtitle(10, "Movie", "Hrvatski"))
                    : FakeTitloviApiClient.EmptyPage()
            };
            var config = new TestConfiguration { IncludeBshAlternativeLanguages = true };

            var result = (await TestData.Engine(api, config).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "sr"
            }, CancellationToken.None)).Single();

            Assert.Equal("hr", result.LanguageCode);
            Assert.Equal("hrv", result.ThreeLetterIsoLanguageName);
            Assert.Contains("Hrvatski", result.Name);
        }

        [Fact]
        public async Task DuplicateMediaId_IsReplacedByBetterActualLanguagePriority()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call =>
                {
                    if (call.Language == "Srpski")
                        return FakeTitloviApiClient.Page(TestData.Subtitle(20, "Movie", "Hrvatski", release: "OLD"));
                    if (call.Language == "Bosanski")
                        return FakeTitloviApiClient.Page(TestData.Subtitle(20, "Movie", "Bosanski", release: "BETTER"));
                    return FakeTitloviApiClient.EmptyPage();
                }
            };
            var config = new TestConfiguration { IncludeBshAlternativeLanguages = true };

            var result = (await TestData.Engine(api, config).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "sr"
            }, CancellationToken.None)).Single();

            Assert.Equal("bs", result.LanguageCode);
            Assert.Equal("BETTER", result.Comment);
        }

        [Fact]
        public async Task WholeSeriesSeasonZeroResult_IsRewrittenToRequestedEpisode()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call =>
                {
                    if (call.Seasons != null && call.Seasons.SequenceEqual(new[] { 0 }) && call.Episodes == null)
                    {
                        return FakeTitloviApiClient.Page(TestData.Subtitle(
                            382914, "Seinfeld", "Hrvatski", "1080p.BluRay.x265-RARBG",
                            season: 0, episode: 0, type: TitloviSubtitleType.Episode));
                    }
                    return FakeTitloviApiClient.EmptyPage();
                }
            };
            var config = new TestConfiguration { IncludeBshAlternativeLanguages = false };

            var result = (await TestData.Engine(api, config).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Episode,
                SeriesName = "Seinfeld",
                Name = "The Doll",
                ParentIndexNumber = 7,
                IndexNumber = 16,
                Language = "hr"
            }, CancellationToken.None)).Single();

            var metadata = TestData.DecodeMetadataId(result.Id);
            Assert.Equal("7", metadata[3]);
            Assert.Equal("16", metadata[4]);
            Assert.Equal("hr", metadata[7]);
        }

        [Fact]
        public async Task SeriesImdbSearch_FallsBackToTitleAndKeepsTitleForWholeSeriesProbe()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call =>
                {
                    if (call.Query == "Seinfeld" && call.Seasons != null && call.Seasons.Count == 2)
                        return FakeTitloviApiClient.Page(TestData.Subtitle(30, "Seinfeld", "Srpski", season: 1, episode: 1, type: TitloviSubtitleType.Episode));
                    return FakeTitloviApiClient.EmptyPage();
                }
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Episode,
                SeriesName = "Seinfeld",
                Name = "The Seinfeld Chronicles",
                ParentIndexNumber = 1,
                IndexNumber = 1,
                Language = "sr",
                SeriesProviderIds = new System.Collections.Generic.Dictionary<string, string> { ["Imdb"] = "tt0098904" }
            }, CancellationToken.None)).ToList();

            Assert.Single(results);
            Assert.True(api.SearchCalls.Count >= 3);
            Assert.Equal("tt0098904", api.SearchCalls[0].Query);
            Assert.Equal("tt0098904", api.SearchCalls[0].ImdbId);
            Assert.Equal("Seinfeld", api.SearchCalls[1].Query);
            Assert.Null(api.SearchCalls[1].ImdbId);
            Assert.Equal("Seinfeld", api.SearchCalls[2].Query);
            Assert.Equal(new[] { 0 }, api.SearchCalls[2].Seasons);
            Assert.Null(api.SearchCalls[2].Episodes);
        }

        [Fact]
        public async Task DisneyAnimatedShorts_LargeSeasonNumber_IsPassedToApiUnchanged()
        {
            var api = new FakeTitloviApiClient { SearchHandler = _ => FakeTitloviApiClient.EmptyPage() };

            var results = await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Episode,
                SeriesName = "Disney Animated Shorts",
                Name = "Short",
                ParentIndexNumber = 1947,
                IndexNumber = 1,
                Language = "en"
            }, CancellationToken.None);

            Assert.Empty(results);
            Assert.Equal(new[] { 0, 1947 }, api.SearchCalls[0].Seasons);
            Assert.Equal(new[] { 0, 1 }, api.SearchCalls[0].Episodes);
        }

        [Fact]
        public async Task Berserk_SeasonZeroRequest_IsNotSilentlyChangedBeforeApiSearch()
        {
            var api = new FakeTitloviApiClient { SearchHandler = _ => FakeTitloviApiClient.EmptyPage() };

            await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Episode,
                SeriesName = "Berserk",
                Name = "Special",
                ParentIndexNumber = 0,
                IndexNumber = 1,
                Language = "en"
            }, CancellationToken.None);

            Assert.Equal(new[] { 0, 0 }, api.SearchCalls[0].Seasons);
            Assert.Equal(new[] { 0, 1 }, api.SearchCalls[0].Episodes);
        }

        [Fact]
        public async Task MissingCredentials_ReturnsNoResultsWithoutCallingApi()
        {
            var api = new FakeTitloviApiClient();
            var config = new TestConfiguration { Username = "", Password = "" };

            var result = await TestData.Engine(api, config).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "sr"
            }, CancellationToken.None);

            Assert.Empty(result);
            Assert.Empty(api.SearchCalls);
            Assert.Equal(0, api.TokenCalls);
        }

        [Fact]
        public async Task ExpiredToken_IsRefreshedOnceAndSaved()
        {
            var api = new FakeTitloviApiClient { SearchHandler = _ => FakeTitloviApiClient.EmptyPage() };
            var config = new TestConfiguration
            {
                Token = new TitloviToken { Token = "expired", ExpirationDate = DateTime.Now.AddMinutes(-1) }
            };

            await TestData.Engine(api, config).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "en"
            }, CancellationToken.None);

            Assert.Equal(1, api.TokenCalls);
            Assert.Equal(1, config.SaveCalls);
            Assert.Equal("fresh-token", config.Token.Token);
        }

        [Fact]
        public async Task ValidCachedToken_DoesNotAuthenticateAgain()
        {
            var api = new FakeTitloviApiClient { SearchHandler = _ => FakeTitloviApiClient.EmptyPage() };
            var config = new TestConfiguration();

            await TestData.Engine(api, config).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "en"
            }, CancellationToken.None);

            Assert.Equal(0, api.TokenCalls);
            Assert.Equal(0, config.SaveCalls);
        }

        [Fact]
        public async Task ReleaseMatch_OutranksDownloadCount()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => FakeTitloviApiClient.Page(
                    TestData.Subtitle(41, "The Matrix", "English", "1080p.BluRay.x265-GROUP", downloads: 10),
                    TestData.Subtitle(42, "The Matrix", "English", "1080p.WEB-DL.x264", downloads: 50000),
                    TestData.Subtitle(43, "The Matrix", "English", "720p.BluRay", downloads: 90000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "The Matrix",
                Language = "en",
                MediaPath = "The.Matrix.1999.1080p.BluRay.x265-GROUP.mkv"
            }, CancellationToken.None)).ToList();

            Assert.Equal("1080p.BluRay.x265-GROUP", results[0].Comment);
        }

        [Fact]
        public async Task ReleaseMatching_PrefersSourceAndGroupOverResolutionAndCodec()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => FakeTitloviApiClient.Page(
                    TestData.Subtitle(61, "Show", "English", "1080p.WEB-DL.x265-GROUP", downloads: 1),
                    TestData.Subtitle(62, "Show", "English", "720p.WEB-DL.x264-GROUP", downloads: 1000),
                    TestData.Subtitle(63, "Show", "English", "1080p.WEB-DL.x265-OTHER", downloads: 2000),
                    TestData.Subtitle(64, "Show", "English", "1080p.BluRay.x265-OTHER", downloads: 5000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Show",
                Language = "en",
                MediaPath = "Show.1080p.WEB-DL.x265-GROUP.mkv"
            }, CancellationToken.None)).ToList();

            Assert.Equal(new[] { 61, 62, 63, 64 }, results.Select(x => int.Parse(TestData.DecodeMetadataId(x.Id)[0])).ToArray());
        }

        [Fact]
        public async Task ReleaseMatching_ResolutionIsWeakerThanSource()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => FakeTitloviApiClient.Page(
                    TestData.Subtitle(71, "Show", "English", "720p.WEB-DL.x264", downloads: 1),
                    TestData.Subtitle(72, "Show", "English", "1080p.BluRay.x265", downloads: 10000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Show",
                Language = "en",
                MediaPath = "Show.1080p.WEB-DL.x265.mkv"
            }, CancellationToken.None)).ToList();

            Assert.Equal("720p.WEB-DL.x264", results[0].Comment);
        }

        [Fact]
        public async Task ReleaseMatching_WebDlAndWebRipGetPartialFamilyCredit()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => FakeTitloviApiClient.Page(
                    TestData.Subtitle(81, "Show", "English", "1080p.WEBRip.x265-GROUP", downloads: 1),
                    TestData.Subtitle(82, "Show", "English", "1080p.BluRay.x265-GROUP", downloads: 10000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Show",
                Language = "en",
                MediaPath = "Show.1080p.WEB-DL.x265-GROUP.mkv"
            }, CancellationToken.None)).ToList();

            Assert.Equal("1080p.WEBRip.x265-GROUP", results[0].Comment);
        }

        [Fact]
        public async Task ReleaseMatching_ExplicitFpsConflictIsPenalized()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => FakeTitloviApiClient.Page(
                    TestData.Subtitle(91, "Movie", "English", "1080p.BluRay.x265.23.976fps-GROUP", downloads: 1),
                    TestData.Subtitle(92, "Movie", "English", "1080p.BluRay.x265.25fps-GROUP", downloads: 10000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "en",
                MediaPath = "Movie.1080p.BluRay.x265.23.976fps-GROUP.mkv"
            }, CancellationToken.None)).ToList();

            Assert.Equal("1080p.BluRay.x265.23.976fps-GROUP", results[0].Comment);
        }

        [Fact]
        public async Task ReleaseMatching_ExplicitCutConflictIsPenalized()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => FakeTitloviApiClient.Page(
                    TestData.Subtitle(101, "Movie", "English", "1080p.BluRay.Extended.x265-GROUP", downloads: 1),
                    TestData.Subtitle(102, "Movie", "English", "1080p.BluRay.Theatrical.x265-GROUP", downloads: 10000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Movie",
                Language = "en",
                MediaPath = "Movie.1080p.BluRay.Extended.x265-GROUP.mkv"
            }, CancellationToken.None)).ToList();

            Assert.Equal("1080p.BluRay.Extended.x265-GROUP", results[0].Comment);
        }

        [Fact]
        public async Task ReleaseMatching_UsesBestVariantWithoutCombiningDifferentReleaseDescriptions()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => FakeTitloviApiClient.Page(
                    TestData.Subtitle(111, "Show", "English", "1080p.WEB-DL.x265-GROUP", downloads: 1),
                    TestData.Subtitle(112, "Show", "English", "1080p.BluRay.x265-OTHER / 720p.WEB-DL.x264-GROUP", downloads: 1000),
                    TestData.Subtitle(113, "Show", "English", "1080p.WEB-DL.x265-OTHER", downloads: 2000))
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "Show",
                Language = "en",
                MediaPath = "Show.1080p.WEB-DL.x265-GROUP.mkv"
            }, CancellationToken.None)).ToList();

            Assert.Equal(new[] { 111, 112, 113 }, results.Select(x => int.Parse(TestData.DecodeMetadataId(x.Id)[0])).ToArray());
        }

        [Fact]
        public async Task MovieImdbSearch_FallsBackToTitleWhenNeeded()
        {
            var api = new FakeTitloviApiClient
            {
                SearchHandler = call => call.Query == "The Matrix"
                    ? FakeTitloviApiClient.Page(TestData.Subtitle(50, "The Matrix", "English"))
                    : FakeTitloviApiClient.EmptyPage()
            };

            var results = (await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "The Matrix",
                Language = "en",
                ProviderIds = new System.Collections.Generic.Dictionary<string, string> { ["Imdb"] = "tt0133093" }
            }, CancellationToken.None)).ToList();

            Assert.Single(results);
            Assert.Equal("tt0133093", api.SearchCalls[0].Query);
            Assert.Equal("The Matrix", api.SearchCalls[1].Query);
            Assert.Null(api.SearchCalls[1].ImdbId);
        }

        [Fact]
        public async Task ZeroResultSearch_ReturnsEmptyList()
        {
            var api = new FakeTitloviApiClient { SearchHandler = _ => FakeTitloviApiClient.EmptyPage() };
            var results = await TestData.Engine(api, new TestConfiguration()).Search(new CoreSubtitleSearchRequest
            {
                ContentType = TitloviContentType.Movie,
                Name = "No Such Title",
                Language = "en"
            }, CancellationToken.None);

            Assert.Empty(results);
        }
    }
}
