using FluentAssertions.Execution;
using FluentAssertions;
using NSubstitute;
using LaPalmaTrailsAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;

namespace LaPalmaTrailsAPI.Tests
{
    [ExcludeFromCodeCoverage]
    public class ControllerTests
    {
        [Fact]
        public async Task Successful_scrape_returns_trails_anomalies_success_200()
        {
            // Arrange
            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var sut = new TrailStatusesController(stubHttpClient, new StatusScraper());

            // Act
            var actionResult = await sut.Get();

            // Assert
            var okResult = actionResult as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var returnValue = okResult.Value as ScraperResult;
            returnValue.Should().NotBeNull();
            returnValue.Result.ShouldMatch(TestHelper.SuccessResult_OneLookup_NoAnomalies);
            returnValue.Anomalies.Should().BeEmpty();
            returnValue.Trails.Should().ContainSingle();
            returnValue.Trails[0].ShouldMatch(TestHelper.Gr130_Open_EnglishLink);
        }


        [Fact]
        public async Task Successful_scrape_with_parameters_returns_trails_anomalies_success_200()
        {
            // Arrange
            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var mockStatusScraper = Substitute.For<IStatusScraper>();
            mockStatusScraper.GetTrailStatuses(stubHttpClient)
                .Returns(new ScraperResult());

            string nonDefaultUrl        = "https://different.co.uk";
            int nonDefaultStatusTimeout = 999;
            int nonDefaultDetailTimeout = 111;
            bool nonDefaultUseCache     = false;
            bool nonDefaultClearLookups = true;


            var sut = new TrailStatusesController(stubHttpClient, mockStatusScraper);

            // Act
            var actionResult = await sut.Get(
                nonDefaultUrl,
                nonDefaultStatusTimeout,
                nonDefaultDetailTimeout,
                nonDefaultUseCache,
                nonDefaultClearLookups);

            // Assert
            mockStatusScraper.StatusPage.Should().Be(nonDefaultUrl);
            mockStatusScraper.StatusPageTimeout.Should().Be(nonDefaultStatusTimeout);
            mockStatusScraper.DetailPageTimeout.Should().Be(nonDefaultDetailTimeout);
            mockStatusScraper.UseCache.Should().Be(nonDefaultUseCache);
            mockStatusScraper.ClearLookups.Should().Be(nonDefaultClearLookups);

            var okResult = actionResult as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
        }


        [Fact]
        public async Task Failed_scrape_returns_error_message_server_error_500()
        {
            // Arrange
            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var mockStatusScraper = Substitute.For<IStatusScraper>();
            mockStatusScraper.GetTrailStatuses(stubHttpClient)
                .Returns(Task.FromException<ScraperResult>(new Exception("error message thrown in GetTrailStatuses")));

            var sut = new TrailStatusesController(stubHttpClient, mockStatusScraper);

            // Act
            var actionResult = await sut.Get("trail status url");

            // Assert
            actionResult.Should().BeOfType<ObjectResult>();
            var statusCodeResult = actionResult as ObjectResult;
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be(500);
            statusCodeResult.Value.Should().Be("error message thrown in GetTrailStatuses");
        }
    }
}
