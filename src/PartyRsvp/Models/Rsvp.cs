namespace PartyRsvp.Models;

/// <summary>A guest's stored reply. One row per email address; re-submitting updates it.</summary>
public class Rsvp
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Trimmed and lower-cased so the same person can't RSVP twice.</summary>
    public required string Email { get; set; }

    public required string Phone { get; set; }

    public bool WillAttend { get; set; }

    /// <summary>Number of people coming, including the guest. Zero when declining.</summary>
    public int PartySize { get; set; }

    public string? DietaryNotes { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
