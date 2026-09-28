using System.Globalization;
using System.Text;
using PartyRsvp.Models;

namespace PartyRsvp.Services;

/// <summary>Builds an RFC 5545 .ics file so guests can drop the party straight into their calendar.</summary>
public static class CalendarInvite
{
    public static string Create(EventOptions evt, DateTimeOffset now)
    {
        var ics = new StringBuilder();
        void Line(string text) => ics.Append(Fold(text)).Append("\r\n");

        Line("BEGIN:VCALENDAR");
        Line("VERSION:2.0");
        Line("PRODID:-//PartyRsvp//EN");
        Line("CALSCALE:GREGORIAN");
        Line("METHOD:PUBLISH");
        Line("BEGIN:VEVENT");
        Line($"UID:party-{evt.StartsAt.ToUnixTimeSeconds()}@partyrsvp");
        Line($"DTSTAMP:{Utc(now)}");
        Line($"DTSTART:{Utc(evt.StartsAt)}");
        Line($"DTEND:{Utc(evt.EndsAt)}");
        Line($"SUMMARY:{Escape(evt.Title)}");
        Line($"DESCRIPTION:{Escape(Details(evt))}");
        Line($"LOCATION:{Escape(Place(evt))}");
        Line("END:VEVENT");
        Line("END:VCALENDAR");

        return ics.ToString();
    }

    /// <summary>A link that opens Google Calendar's "add event" page pre-filled with the party.</summary>
    public static string GoogleCalendarUrl(EventOptions evt) =>
        "https://calendar.google.com/calendar/render?action=TEMPLATE" +
        $"&text={Uri.EscapeDataString(evt.Title)}" +
        $"&dates={Utc(evt.StartsAt)}/{Utc(evt.EndsAt)}" +
        $"&details={Uri.EscapeDataString(Details(evt))}" +
        $"&location={Uri.EscapeDataString(Place(evt))}";

    private static string Place(EventOptions evt) =>
        string.Join(", ", new[] { evt.Location, evt.Address }.Where(s => s.Length > 0));

    private static string Details(EventOptions evt) =>
        string.Join("\n\n", new[] { evt.Tagline, evt.Description, evt.DressCode.Length > 0 ? $"Dress code: {evt.DressCode}" : "" }
            .Where(s => s.Length > 0));

    /// <summary>RFC 5545 §3.1: lines longer than 75 octets continue on the next line after a CRLF + space.</summary>
    internal static string Fold(string line)
    {
        var folded = new StringBuilder();
        var octets = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (octets + size > 75)
            {
                folded.Append("\r\n ");
                octets = 1;
            }
            folded.Append(rune.ToString());
            octets += size;
        }
        return folded.ToString();
    }

    internal static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n");

    private static string Utc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
}
