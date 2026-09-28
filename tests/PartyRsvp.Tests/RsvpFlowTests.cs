using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PartyRsvp.Data;
using PartyRsvp.Models;

namespace PartyRsvp.Tests;

public class RsvpFlowTests : IDisposable
{
    private readonly PartyAppFactory _app = new();

    public void Dispose()
    {
        _app.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<List<Rsvp>> StoredRsvpsAsync()
    {
        using var scope = _app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PartyDbContext>().Rsvps.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Invitation_page_shows_the_party_details()
    {
        var html = await _app.CreateClient().GetStringAsync("/");

        Assert.Contains("Suhani", html);
        Assert.Contains("turns twenty-one", html);
        Assert.Contains("The Garden Terrace", html);
        Assert.Contains("href=\"/rsvp\"", html);
    }

    [Fact]
    public async Task Accepting_saves_the_reply_and_thanks_the_guest()
    {
        var response = await PartyAppFactory.PostRsvpAsync(_app.CreateClient(), PartyAppFactory.ValidRsvp());
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Thank you, Maya", html);
        Assert.Contains("party of 2", html);

        var saved = Assert.Single(await StoredRsvpsAsync());
        Assert.True(saved.WillAttend);
        Assert.Equal(2, saved.PartySize);
        Assert.Equal("Vegetarian", saved.DietaryNotes);
    }

    [Fact]
    public async Task Declining_shows_the_sorry_message_and_clears_attending_only_fields()
    {
        var response = await PartyAppFactory.PostRsvpAsync(_app.CreateClient(), PartyAppFactory.ValidRsvp(attending: false));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("miss you", html);
        var saved = Assert.Single(await StoredRsvpsAsync());
        Assert.False(saved.WillAttend);
        Assert.Equal(0, saved.PartySize);
        Assert.Null(saved.DietaryNotes);
    }

    [Fact]
    public async Task Replying_again_with_the_same_email_updates_instead_of_duplicating()
    {
        var client = _app.CreateClient();
        await PartyAppFactory.PostRsvpAsync(client, PartyAppFactory.ValidRsvp("Maya@Example.com"));
        var second = await PartyAppFactory.PostRsvpAsync(client, PartyAppFactory.ValidRsvp("  maya@example.COM ", attending: false));

        Assert.Contains("Your reply has been updated", await second.Content.ReadAsStringAsync());
        var saved = Assert.Single(await StoredRsvpsAsync());
        Assert.Equal("maya@example.com", saved.Email);
        Assert.False(saved.WillAttend);
    }

    [Theory]
    [InlineData("Name", "", "Please enter your name")]
    [InlineData("Email", "not-an-email", "That doesn&#x27;t look like an email address")]
    [InlineData("Phone", "call me", "That doesn&#x27;t look like a phone number")]
    [InlineData("WillAttend", "", "Please let us know whether you&#x27;ll attend")]
    [InlineData("PartySize", "9", "Party size must be between 1 and 4")]
    public async Task Invalid_replies_are_rejected_with_a_helpful_message(string field, string value, string error)
    {
        var fields = PartyAppFactory.ValidRsvp();
        fields[field] = value;

        var response = await PartyAppFactory.PostRsvpAsync(_app.CreateClient(), fields);

        Assert.Contains(error, await response.Content.ReadAsStringAsync());
        Assert.Empty(await StoredRsvpsAsync());
    }

    [Fact]
    public async Task Posting_without_an_antiforgery_token_is_refused()
    {
        var response = await _app.CreateClient().PostAsync("/rsvp", new FormUrlEncodedContent(PartyAppFactory.ValidRsvp()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await StoredRsvpsAsync());
    }

    [Fact]
    public async Task Guest_list_never_exposes_emails_or_phone_numbers()
    {
        var client = _app.CreateClient();
        await PartyAppFactory.PostRsvpAsync(client, PartyAppFactory.ValidRsvp());

        var page = await client.GetStringAsync("/guests");
        var api = await client.GetStringAsync("/api/guests");

        foreach (var body in new[] { page, api })
        {
            Assert.Contains("Maya Patel", body);
            Assert.DoesNotContain("maya@example.com", body);
            Assert.DoesNotContain("555-0100", body);
            Assert.DoesNotContain("Vegetarian", body);
        }
    }

    [Fact]
    public async Task Guest_names_are_html_encoded()
    {
        var client = _app.CreateClient();
        var fields = PartyAppFactory.ValidRsvp();
        fields["Name"] = "<script>alert(1)</script>";
        await PartyAppFactory.PostRsvpAsync(client, fields);

        var page = await client.GetStringAsync("/guests");

        Assert.DoesNotContain("<script>alert(1)</script>", page);
        Assert.Contains("&lt;script&gt;", page);
    }

    [Fact]
    public async Task Stats_count_everyone_in_each_party()
    {
        var client = _app.CreateClient();
        await PartyAppFactory.PostRsvpAsync(client, PartyAppFactory.ValidRsvp("a@example.com"));            // 2 coming
        await PartyAppFactory.PostRsvpAsync(client, PartyAppFactory.ValidRsvp("b@example.com"));            // 2 coming
        await PartyAppFactory.PostRsvpAsync(client, PartyAppFactory.ValidRsvp("c@example.com", false));     // regrets

        var stats = await client.GetFromJsonAsync<RsvpStats>("/api/stats");

        Assert.Equal(new RsvpStats(Responses: 3, Attending: 2, Declined: 1, Headcount: 4), stats);
    }

    [Fact]
    public async Task Form_closes_after_the_rsvp_deadline()
    {
        var client = _app.CreateClient();
        var form = await client.GetStringAsync("/rsvp"); // grab a token while still open
        _app.Clock.Now = new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero);

        Assert.Contains("Replies are closed", await client.GetStringAsync("/rsvp"));

        var token = System.Text.RegularExpressions.Regex.Match(form, "__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var late = await client.PostAsync("/rsvp", new FormUrlEncodedContent(
            new Dictionary<string, string>(PartyAppFactory.ValidRsvp()) { ["__RequestVerificationToken"] = token }));

        Assert.Contains("Replies are closed", await late.Content.ReadAsStringAsync());
        Assert.Empty(await StoredRsvpsAsync());
    }

    [Fact]
    public async Task Unknown_pages_get_the_friendly_404()
    {
        var response = await _app.CreateClient().GetAsync("/no-such-page");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Lost in the garden", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var response = await _app.CreateClient().GetAsync("/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task Health_check_reports_healthy()
    {
        Assert.Equal("Healthy", await _app.CreateClient().GetStringAsync("/health"));
    }
}

public class RateLimitTests : IDisposable
{
    private readonly PartyAppFactory _app = new() { RsvpsPerMinute = 2 };

    public void Dispose()
    {
        _app.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Rsvp_spam_is_throttled()
    {
        var client = _app.CreateClient();
        HttpStatusCode last = default;
        for (var i = 0; i < 3; i++)
        {
            last = (await PartyAppFactory.PostRsvpAsync(client, PartyAppFactory.ValidRsvp($"guest{i}@example.com"))).StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }
}
