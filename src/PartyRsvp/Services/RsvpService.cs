using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PartyRsvp.Data;
using PartyRsvp.Hubs;
using PartyRsvp.Models;

namespace PartyRsvp.Services;

public record RsvpResult(Rsvp Rsvp, bool IsUpdate);

public interface IRsvpService
{
    Task<RsvpResult> SubmitAsync(RsvpInput input, CancellationToken ct = default);
    Task<IReadOnlyList<GuestSummary>> GetAttendeesAsync(CancellationToken ct = default);
    Task<RsvpStats> GetStatsAsync(CancellationToken ct = default);
}

public class RsvpService(
    PartyDbContext db,
    IHubContext<RsvpHub> hub,
    TimeProvider clock,
    ILogger<RsvpService> logger) : IRsvpService
{
    public async Task<RsvpResult> SubmitAsync(RsvpInput input, CancellationToken ct = default)
    {
        var email = input.Email!.Trim().ToLowerInvariant();
        var now = clock.GetUtcNow().UtcDateTime;
        var attending = input.WillAttend == true;

        var rsvp = await db.Rsvps.SingleOrDefaultAsync(r => r.Email == email, ct);
        var isUpdate = rsvp is not null;

        rsvp ??= new Rsvp { Name = "", Email = email, Phone = "", CreatedAtUtc = now };
        rsvp.Name = input.Name!.Trim();
        rsvp.Phone = input.Phone!.Trim();
        rsvp.WillAttend = attending;
        rsvp.PartySize = attending ? input.PartySize : 0;
        rsvp.DietaryNotes = attending ? Clean(input.DietaryNotes) : null;
        rsvp.Message = Clean(input.Message);
        rsvp.UpdatedAtUtc = now;

        if (!isUpdate)
        {
            db.Rsvps.Add(rsvp);
        }
        await db.SaveChangesAsync(ct);

        logger.LogInformation("RSVP {Action} for {RsvpId}: attending={Attending}, party of {PartySize}",
            isUpdate ? "updated" : "created", rsvp.Id, attending, rsvp.PartySize);

        await hub.Clients.All.SendAsync(RsvpHub.RsvpChanged, await GetStatsAsync(ct), ct);

        return new RsvpResult(rsvp, isUpdate);
    }

    public async Task<IReadOnlyList<GuestSummary>> GetAttendeesAsync(CancellationToken ct = default) =>
        await db.Rsvps
            .AsNoTracking()
            .Where(r => r.WillAttend)
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => new GuestSummary(r.Name, r.PartySize, r.Message))
            .ToListAsync(ct);

    public async Task<RsvpStats> GetStatsAsync(CancellationToken ct = default)
    {
        var replies = await db.Rsvps
            .AsNoTracking()
            .Select(r => new { r.WillAttend, r.PartySize })
            .ToListAsync(ct);

        var attending = replies.Count(r => r.WillAttend);
        return new RsvpStats(
            Responses: replies.Count,
            Attending: attending,
            Declined: replies.Count - attending,
            Headcount: replies.Sum(r => r.PartySize));
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
