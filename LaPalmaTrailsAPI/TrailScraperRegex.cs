using System.Text.RegularExpressions;

namespace LaPalmaTrailsAPI
{
    /// <summary>
    /// Content matching methods
    /// </summary>
    public class TrailScraperRegex
    {
        public static Match MatchValidTrailFormats(string trail)
        {
            // GR 130 Etapa <digit>
            // GR 131 Etapa <digit>
            // <PR or SL> <2 upper case letters> <2 or 3 digits>
            // <PR or SL> <2 upper case letters> <2 or 3 digits>.<digit with or without leading zero>
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
