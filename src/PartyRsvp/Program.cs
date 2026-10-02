using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PartyRsvp.Data;
using PartyRsvp.Endpoints;
using PartyRsvp.Hubs;
using PartyRsvp.Models;
using PartyRsvp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();

builder.Services.Configure<EventOptions>(builder.Configuration.GetSection(EventOptions.SectionName));
builder.Services.AddDbContext<PartyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Party")));
builder.Services.AddScoped<IRsvpService, RsvpService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHealthChecks().AddDbContextCheck<PartyDbContext>();

// Stop anyone from spamming the RSVP form: N submissions per minute per IP.
var rsvpPermitLimit = builder.Configuration.GetValue("RateLimiting:RsvpPerMinute", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("rsvp", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = rsvpPermitLimit, Window = TimeSpan.FromMinutes(1) }));
});

// Behind a hosting proxy (Render), read the visitor's real IP so rate limiting is per guest, not per proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PartyDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/status/{0}");
app.UseRequestLocalization("en-US");

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();

app.MapControllers();
app.MapPartyApi();
app.MapHub<RsvpHub>(RsvpHub.Path);
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
