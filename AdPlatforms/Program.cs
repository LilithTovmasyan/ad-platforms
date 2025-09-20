using AdPlatforms.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AdPlatformsStore>();
builder.Services.AddSingleton<AdPlatformsService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// 1) Load/Reload from text file content (plain text body)
app.MapPost("/api/ad-platforms/load", async (HttpRequest req, AdPlatformsService svc) =>
{
    using var reader = new StreamReader(req.Body, Encoding.UTF8);
    string text = await reader.ReadToEndAsync();
    var result = svc.LoadFromText(text);
    return result.IsSuccess
        ? Results.Ok(new { message = "Loaded", platformsCount = result.PlatformsCount, locationKeys = result.LocationKeyCount })
        : Results.BadRequest(new { message = "Invalid input", errors = result.Errors });
})
.WithOpenApi(o => { o.Summary = "Load advertising platforms from plaintext"; return o; });

// 2) Query platforms by location
// Example: GET /api/ad-platforms?location=/ru/svrd/revda
app.MapGet("/api/ad-platforms", (string location, AdPlatformsService svc) =>
{
    var list = svc.FindByLocation(location);
    return Results.Ok(list);
})
.WithOpenApi(o => { o.Summary = "Get platforms available for a given location"; return o; });

app.Run();
