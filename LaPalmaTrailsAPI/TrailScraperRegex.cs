using System.Text.RegularExpressions;

namespace LaPalmaTrailsAPI
{
    public class TrailScraperRegex
    {
        public static Match MatchValidTrailFormats(string trail)
        {
            // trail ID examples:
            // - GR 130 Etapa 1, GR 131 Etapa 2
            // - PR LP 01, SL BV 09
            // - PR LP 01.1
            // - PR LP 02.01 (the leading 0 after the decimal is an error on their part that must be corrected)
            return Regex.Match(trail, @"(GR 13(0|1) Etapa \d)|((PR|SL) [A-Z][A-Z] \d{2,3}(\.0\d|\.\d|)?)");
        }

        public static bool TrailIdHasTwoDigitsAfterDecimal(string trailId)
        {
            return Regex.IsMatch(trailId, @"\.\d\d$");
        }

        public static bool TrailIsCompletelyOpen(string status)
        {
            return Regex.IsMatch(status, @"^Abierto / Open / Geöffnet(<br />)?$");
        }

        public static bool TrailIsOpen(string status)
        {
            return Regex.IsMatch(status, "Abierto / Open / Geöffnet");
        }

        public static bool TrailIsClosed(string status)
        {
            return Regex.IsMatch(status, "Cerrado / Closed / Gesperrt");
        }
    }
}
