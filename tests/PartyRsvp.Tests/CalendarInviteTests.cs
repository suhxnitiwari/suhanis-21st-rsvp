using System.Net;
using System.Text;
using PartyRsvp.Models;
using PartyRsvp.Services;

namespace PartyRsvp.Tests;

public class CalendarInviteTests
{
    private static readonly EventOptions Party = new()
    {
        Title = "Suhani's 21st Birthday",
        Tagline = "An Evening in the Garden",
        StartsAt = new DateTimeOffset(2026, 11, 14, 19, 0, 0, TimeSpan.FromHours(-6)),
        Duration = TimeSpan.FromHours(5),
        Location = "The Garden Terrace",
        Address = "Austin, Texas",
    };

    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Times_are_converted_to_utc()
    {
        var ics = CalendarInvite.Create(Party, Now);

        Assert.Contains("DTSTART:20261115T010000Z\r\n", ics);
        Assert.Contains("DTEND:20261115T060000Z\r\n", ics);
    }

    [Fact]
    public void Commas_and_semicolons_are_escaped()
    {
        var ics = CalendarInvite.Create(Party, Now);

        Assert.Contains(@"LOCATION:The Garden Terrace\, Austin\, Texas", ics);
        Assert.Equal(@"a\;b\,c\\d\ne", CalendarInvite.Escape("a;b,c\\d\ne"));
    }

    [Fact]
    public void Long_lines_are_folded_at_75_octets()
    {
        var folded = CalendarInvite.Fold("DESCRIPTION:" + new string('x', 100) + "🎂🎂🎂");

        var lines = folded.Split("\r\n");
        Assert.True(lines.Length > 1);
        Assert.All(lines, line => Assert.True(Encoding.UTF8.GetByteCount(line) <= 75));
        Assert.All(lines.Skip(1), line => Assert.StartsWith(" ", line));
        // Unfolding gives back the original, and multi-byte characters are never split.
        Assert.Equal("DESCRIPTION:" + new string('x', 100) + "🎂🎂🎂", folded.Replace("\r\n ", ""));
    }

    [Fact]
    public void Google_calendar_link_is_prefilled()
    {
        var url = CalendarInvite.GoogleCalendarUrl(Party);

        Assert.StartsWith("https://calendar.google.com/calendar/render?action=TEMPLATE", url);
        Assert.Contains("dates=20261115T010000Z/20261115T060000Z", url);
        Assert.Contains("text=Suhani%27s%2021st%20Birthday", url);
    }

    [Fact]
    public async Task Ics_endpoint_serves_a_calendar_file()
    {
        using var app = new PartyAppFactory();
        var response = await app.CreateClient().GetAsync("/event.ics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/calendar", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("BEGIN:VCALENDAR\r\n", body);
        Assert.Contains("SUMMARY:Suhani's 21st Birthday", body);
    }
}
