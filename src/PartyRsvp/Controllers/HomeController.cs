using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PartyRsvp.Models;
using PartyRsvp.Services;

namespace PartyRsvp.Controllers;

public class HomeController(IRsvpService rsvps, IOptions<EventOptions> eventOptions, TimeProvider clock) : Controller
{
    private EventOptions Event => eventOptions.Value;

    private bool RsvpOpen => Event.IsRsvpOpen(clock.GetUtcNow());

    [HttpGet("/")]
    public IActionResult Index() => View(Event);

    [HttpGet("/rsvp")]
    public IActionResult RsvpForm() => RsvpOpen ? View(new RsvpInput()) : View("RsvpClosed", Event);

    [HttpPost("/rsvp")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("rsvp")]
    public async Task<IActionResult> RsvpForm(RsvpInput input, CancellationToken ct)
    {
        if (!RsvpOpen)
        {
            return View("RsvpClosed", Event);
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var result = await rsvps.SubmitAsync(input, ct);
        return View("Thanks", new ThanksViewModel(result.Rsvp, result.IsUpdate, Event));
    }

    [HttpGet("/guests")]
    public async Task<IActionResult> ListResponses(CancellationToken ct)
    {
        var guests = await rsvps.GetAttendeesAsync(ct);
        var stats = await rsvps.GetStatsAsync(ct);
        return View(new GuestListViewModel(guests, stats, Event));
    }

    [Route("/error")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("Status", StatusCodes.Status500InternalServerError);
    }

    [Route("/status/{code:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Status(int code) => View("Status", code);
}
