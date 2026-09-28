using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PartyRsvp.Tests;

/// <summary>Boots the real app against a throwaway SQLite file, with a clock the test controls.</summary>
public sealed partial class PartyAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"party-test-{Guid.NewGuid():N}.db");

    /// <summary>Well before the RSVP deadline in appsettings.json.</summary>
    public FixedClock Clock { get; } = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));

    public int RsvpsPerMinute { get; init; } = 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Party", $"Data Source={_dbPath};Pooling=False");
        builder.UseSetting("RateLimiting:RsvpPerMinute", RsvpsPerMinute.ToString());
        builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock)));
    }

    /// <summary>Loads the form to pick up the antiforgery cookie + token, then posts it like a browser would.</summary>
    public static async Task<HttpResponseMessage> PostRsvpAsync(HttpClient client, Dictionary<string, string> fields)
    {
        var form = await client.GetStringAsync("/rsvp");
        var token = AntiforgeryToken().Match(form).Groups[1].Value;
        return await client.PostAsync("/rsvp", new FormUrlEncodedContent(
            new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token }));
    }

    public static Dictionary<string, string> ValidRsvp(string email = "maya@example.com", bool attending = true) => new()
    {
        ["Name"] = "Maya Patel",
        ["Email"] = email,
        ["Phone"] = "(512) 555-0100",
        ["WillAttend"] = attending ? "true" : "false",
        ["PartySize"] = "2",
        ["DietaryNotes"] = "Vegetarian",
        ["Message"] = "Happy 21st!",
    };

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        File.Delete(_dbPath);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();
}

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
