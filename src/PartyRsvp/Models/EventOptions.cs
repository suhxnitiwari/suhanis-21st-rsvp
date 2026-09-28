namespace PartyRsvp.Models;

/// <summary>Party details, bound from the "Event" section of appsettings.json.</summary>
public class EventOptions
{
    public const string SectionName = "Event";

    /// <summary>Whose party it is. Shown in script on the invitation.</summary>
    public string Honoree { get; set; } = "Suhani";

    /// <summary>The line under the honoree's name, e.g. "turns twenty-one".</summary>
    public string Occasion { get; set; } = "turns twenty-one";

    public string Title { get; set; } = "Suhani's 21st Birthday";

    public string Tagline { get; set; } = "An Evening in the Garden";

    public string Description { get; set; } = "";

    public DateTimeOffset StartsAt { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.FromHours(4);

    /// <summary>Last moment guests can reply. The form closes after this.</summary>
    public DateTimeOffset RsvpBy { get; set; }

    public string Location { get; set; } = "";

    public string Address { get; set; } = "";

    public string DressCode { get; set; } = "";

    public DateTimeOffset EndsAt => StartsAt + Duration;

    public bool IsRsvpOpen(DateTimeOffset now) => now <= RsvpBy;
}
