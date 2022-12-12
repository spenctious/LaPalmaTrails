using FluentAssertions.Execution;
using FluentAssertions;
using NSubstitute;
using LaPalmaTrailsAPI.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace LaPalmaTrailsAPI.Tests
{
    public class ControllerTests
    {
        [Fact]
        public async Task Successful_scrape_returns_trails_anomalies_success_200()
        {
            // Arrange
            string pageContent = TestHelper.SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={TestHelper.LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{TestHelper.IgnoredContent}</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var sut = new TrailStatusesController(mockHttpClient, new StatusScraper());

            // Act
            var actionResult = await sut.Get();

            // Assert
            var okResult = actionResult as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var returnValue = okResult.Value as ScraperResult;
            returnValue.Should().NotBeNull();
            using (new AssertionScope())
            {
                returnValue.Result.Type.Should().Be("Success");
                returnValue.Result.Message.Should().Be("1 additional page lookups");
                returnValue.Result.Detail.Should().Be("0 anomalies found");
            }

            returnValue.Anomalies.Should().BeEmpty();
            returnValue.Trails.Count.Should().Be(1);

            using (new AssertionScope())
            {
                TrailStatus trail = returnValue.Trails[0];
                trail.Name.Should().Be("GR 130 Etapa 1");
                trail.Status.Should().Be("Open");
                trail.Url.Should().Be("Link_to_English_version.html");
            }
        }


        [Fact]
        public async Task Successful_scrape_with_parameters_returns_trails_anomalies_success_200()
        {
            // Arrange
            string pageContent = TestHelper.SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={TestHelper.LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{TestHelper.IgnoredContent}</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var mockStatusScraper = Substitute.For<IStatusScraper>();
            mockStatusScraper.GetTrailStatuses(mockHttpClient)
                .Returns(new ScraperResult());

            var sut = new TrailStatusesController(mockHttpClient, mockStatusScraper);

            // Act
            var actionResult = await sut.Get("foo", 999, 111, false, false);

            // Assert
            mockStatusScraper.StatusPage.Should().Be("foo");
            mockStatusScraper.StatusPageTimeout.Should().Be(999);
            mockStatusScraper.DetailPageTimeout.Should().Be(111);
            mockStatusScraper.UseCache.Should().Be(false);
            mockStatusScraper.ClearLookups.Should().Be(false);

            var okResult = actionResult as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
        }


        [Fact]
        public async Task Failed_scrape_returns_error_message_server_error_500()
        {
            // Arrange
            string pageContent = TestHelper.SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={TestHelper.LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{TestHelper.IgnoredContent}</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var mockStatusScraper = Substitute.For<IStatusScraper>();
            mockStatusScraper.GetTrailStatuses(mockHttpClient)
                .Returns(Task.FromException<ScraperResult>(new Exception("error message thrown in GetTrailStatuses")));

            var sut = new TrailStatusesController(mockHttpClient, mockStatusScraper);

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
