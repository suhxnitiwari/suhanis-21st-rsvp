namespace PartyRsvp.Models;

public record RsvpStats(int Responses, int Attending, int Declined, int Headcount);

/// <summary>The public view of an attending guest. Deliberately leaves out email and phone.</summary>
public record GuestSummary(string Name, int PartySize, string? Message);

public record GuestListViewModel(IReadOnlyList<GuestSummary> Guests, RsvpStats Stats, EventOptions Event);

public record ThanksViewModel(Rsvp Rsvp, bool IsUpdate, EventOptions Event);
