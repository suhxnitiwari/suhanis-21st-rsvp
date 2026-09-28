using Microsoft.Extensions.Options;
using PartyRsvp.Models;
using PartyRsvp.Services;

namespace PartyRsvp.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapPartyApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Party");

        api.MapGet("/event", (IOptions<EventOptions> evt) => TypedResults.Ok(evt.Value));

        api.MapGet("/stats", async (IRsvpService rsvps, CancellationToken ct) =>
            TypedResults.Ok(await rsvps.GetStatsAsync(ct)));

        api.MapGet("/guests", async (IRsvpService rsvps, CancellationToken ct) =>
            TypedResults.Ok(await rsvps.GetAttendeesAsync(ct)));

        app.MapGet("/event.ics", (IOptions<EventOptions> evt, TimeProvider clock) =>
            TypedResults.Text(CalendarInvite.Create(evt.Value, clock.GetUtcNow()), "text/calendar; charset=utf-8"))
            .WithName("CalendarInvite");

        return app;
    }
}
