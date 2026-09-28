using Microsoft.AspNetCore.SignalR;

namespace PartyRsvp.Hubs;

/// <summary>
/// Server-to-client only: the guest list page listens here and refreshes
/// itself whenever someone RSVPs, so nobody has to hit reload.
/// </summary>
public sealed class RsvpHub : Hub
{
    public const string Path = "/hubs/rsvp";
    public const string RsvpChanged = "rsvpChanged";
}
