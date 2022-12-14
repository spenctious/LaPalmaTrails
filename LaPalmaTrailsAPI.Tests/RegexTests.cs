using FluentAssertions;
using System.Diagnostics.CodeAnalysis;

namespace LaPalmaTrailsAPI.Tests
{
    [ExcludeFromCodeCoverage]
    public class RegexTests
    {
        [InlineData("GR 130 Etapa 1")]
        [InlineData("GR 131 Etapa 1")]
        [InlineData("PR LP 01")]
        [InlineData("PR LP 200")]
        [InlineData("PR LP 02.1")]
        [InlineData("PR LP 03.01")]
        [InlineData("SL BV 01")]
        [InlineData("SL BV 200")]
        [Theory]
        public void Valid_trails_Ids_recognised(string trailId)
        {
            var match = TrailScraperRegex.MatchValidTrailFormats(trailId);

            match.Success.Should().BeTrue();
            trailId.Should().Be(match.ToString());
        }


        [InlineData("PR 130 Etapa 1")]
        [InlineData("GR 120 Etapa 1")]
        [InlineData("GR 130.1")]
        [InlineData("PL LP 12")]
        [InlineData("PR LP 1")]
        [Theory]
        public void Invalid_trails_Ids_rejected(string trailId)
        {
            var match = TrailScraperRegex.MatchValidTrailFormats(trailId);

            match.Success.Should().BeFalse();
        }


        [Fact]
        public void Open_status_is_recognised()
        {
            bool isOpen = TrailScraperRegex.TrailIsOpen("Abierto / Open / Geöffnet");

            isOpen.Should().BeTrue();
        }


        [InlineData("Open")]
        [InlineData("Abierto")]
        [InlineData("Random text")]
        [Theory]
        public void Uncertain_open_status_is_rejected(string status)
        {
            bool isOpen = TrailScraperRegex.TrailIsOpen(status);

            isOpen.Should().BeFalse();
        }


        [InlineData("Abierto / Open / Geöffnet")]
        [InlineData("Abierto / Open / Geöffnet\n")]
        [Theory]
        public void Completely_open_status_is_recognised(string status)
        {
            bool isCompletelyOpen = TrailScraperRegex.TrailIsCompletelyOpen(status);

            isCompletelyOpen.Should().BeTrue();
        }


        [InlineData("something Abierto / Open / Geöffnet")]
        [InlineData("Abierto / Open / Geöffnet\nsomething")]
        [InlineData("Random text")]
        [Theory]
        public void Uncertain_completely_open_status_is_rejected(string status)
        {
            bool isOpen = TrailScraperRegex.TrailIsCompletelyOpen(status);

            isOpen.Should().BeFalse();
        }


        [Fact]
        public void Closed_status_is_recognised()
        {
            bool isClosed = TrailScraperRegex.TrailIsClosed("Cerrado / Closed / Gesperrt");

            isClosed.Should().BeTrue();
        }


        [InlineData("Cerrado")]
        [InlineData("Random text")]
        [Theory]
        public void Uncertain_closed_status_is_rejected(string status)
        {
            bool isClosed = TrailScraperRegex.TrailIsClosed(status);

            isClosed.Should().BeFalse();
        }

        [Fact]
        public void Two_digits_after_decimal_is_recognised()
        {
            bool hasTwoDigitsAfterDecimal = TrailScraperRegex.TrailIdHasTwoDigitsAfterDecimal("PR LP 01.01");

            hasTwoDigitsAfterDecimal.Should().BeTrue();
        }


        [InlineData("PR LP 01.1")]
        [InlineData("PR LP 01.001")]
        [Theory]
        public void Not_two_digits_after_decimal_is_regected(string trailId)
        {
            bool hasTwoDigitsAfterDecimal = TrailScraperRegex.TrailIdHasTwoDigitsAfterDecimal(trailId);

            hasTwoDigitsAfterDecimal.Should().BeFalse();
        }
    }
}
