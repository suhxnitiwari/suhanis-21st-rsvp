# An Evening in the Garden: Suhani's 21st

*A watercolor birthday invitation with a real backend: ASP.NET Core MVC, a live SignalR guest list, calendar invites and 23 integration tests.*

[![CI](https://github.com/suhxnitiwari/suhanis-21st-rsvp/actions/workflows/ci.yml/badge.svg)](https://github.com/suhxnitiwari/suhanis-21st-rsvp/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-8A4D57) ![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-8A4D57) ![EF Core + SQLite](https://img.shields.io/badge/EF_Core-SQLite-56634A) ![SignalR](https://img.shields.io/badge/SignalR-real--time-56634A)

## Ownership

© 2026 Suhani Tiwari. **All rights reserved.** This is my original work. The code is public so you can see how I build, not so you can reuse it: copying, reusing or republishing any part of it, including for a portfolio or a class assignment, is not permitted without my written permission. See [LICENSE](LICENSE).

## What it is

An RSVP site for my 21st birthday, built with **ASP.NET Core MVC** on .NET 10.

It started as Homework 1 in MIS 333K at UT Austin, a tutorial "party invites" app with a form and a list. I got 100 on it, then rebuilt it into something I'd actually send to friends: a watercolor invitation, a live guest list, calendar invites, a real database, a security pass, a test suite with CI, and a Docker deploy config.

![The invitation](docs/screenshots/invitation.png)

| RSVP form | Live guest list | On a phone |
| --- | --- | --- |
| ![RSVP form](docs/screenshots/rsvp.png) | ![Guest list](docs/screenshots/guests.png) | ![Mobile](docs/screenshots/mobile.png) |

## Design choices

- **Invitation card.** Real HTML text sits on the watercolor artwork and scales with CSS container units, so it reads like printed stationery at any screen size.
- **RSVP form.** Accept/decline cards, validation messages written for people, and party size and dietary fields that appear only when you say yes (pure CSS `:has()`, works without JavaScript).
- **One reply per guest.** Emails are normalized, so re-submitting with the same address *updates* your RSVP instead of duplicating it.
- **Live guest list.** SignalR pushes each new RSVP to every open guest-list page, with no refresh needed. It shows names, party sizes and birthday wishes, but never emails or phone numbers.
- **Add to calendar.** A standards-compliant `.ics` download (RFC 5545 escaping and line folding) and a pre-filled Google Calendar link.
- **Countdown** to the party, plus falling petals when you accept (turned off for `prefers-reduced-motion`).
- **RSVP deadline.** The form closes automatically after the reply-by date.
- **Nice link previews.** Open Graph tags and a preview image for when the invite is texted around.

## How it's built

| Concern | How |
| --- | --- |
| Persistence | EF Core + SQLite, unique index on email |
| Architecture | Thin MVC controller → `IRsvpService` → `DbContext`. Separate input model (`RsvpInput`) so the form can't over-post fields like `Id` |
| Real time | SignalR hub; the service broadcasts fresh stats (responses, attending, declined, headcount) to every open page after each save |
| Security | Antiforgery tokens, a fixed-window rate limiter partitioned per IP (10 RSVPs a minute by default, configurable), forwarded-header handling so limits apply per guest rather than per proxy, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, HSTS in production, HTML-encoded output, SRI on the CDN script |
| Config | All party details in `appsettings.json`, bound to `EventOptions` |
| Testability | `TimeProvider` is injected, so tests can move the clock past the RSVP deadline |
| Ops | `/health` check (includes the database), friendly 404/429/500 pages, JSON API at `/api/event`, `/api/stats`, `/api/guests` |
| Quality | 23 xUnit tests (integration tests through `WebApplicationFactory` plus unit tests for the calendar file), run on every push by GitHub Actions, warnings treated as errors |
| Deploy | Multi-stage Dockerfile (SDK build, slim ASP.NET runtime, non-root user, SQLite stored under `/data`) and a `render.yaml` blueprint with the `/health` check wired in |

## Tech stack

C#, .NET 10, ASP.NET Core MVC + minimal APIs, Razor, EF Core, SQLite, SignalR, xUnit, GitHub Actions, Docker, Render.

## Run it locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/PartyRsvp
# → http://localhost:5000
```

The SQLite database (`party.db`) is created automatically on first run.

```bash
dotnet test
```

## Make it your party

Everything about the event lives in [`src/PartyRsvp/appsettings.json`](src/PartyRsvp/appsettings.json):

```json
"Event": {
  "Honoree": "Suhani",
  "Occasion": "turns twenty-one",
  "Tagline": "An Evening in the Garden",
  "StartsAt": "2027-03-06T19:00:00-06:00",
  "RsvpBy": "2027-02-20T23:59:59-06:00",
  "Location": "The Garden Terrace",
  ...
}
```

## Project layout

```
src/PartyRsvp/
  Controllers/HomeController.cs   invitation, RSVP (GET/POST), guest list, error pages
  Services/RsvpService.cs         save/update replies, stats, SignalR broadcast
  Services/CalendarInvite.cs      .ics + Google Calendar link
  Endpoints/ApiEndpoints.cs       minimal-API JSON endpoints + /event.ics
  Hubs/RsvpHub.cs                 real-time guest list
  Models/                         Rsvp (entity), RsvpInput (form), EventOptions (config), view models
  Views/                          Razor views
  wwwroot/                        CSS, JS, artwork
tests/PartyRsvp.Tests/            integration + unit tests
design/                           source artwork the web images are cut from
```

---

*Originally built for MIS 333K, Homework 1: MVC Tutorial.*

Built by [Suhani Tiwari](https://suhanitiwari.com).
