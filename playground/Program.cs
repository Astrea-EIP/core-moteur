using System.Text.Json.Serialization;
using AstreaEngine;

var builder = WebApplication.CreateBuilder(args);

// Configure port
builder.WebHost.UseUrls("http://localhost:5050");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/route", async (RouteApiRequest req) =>
{
    if (req.Points == null || req.Points.Count < 2)
    {
        return Results.BadRequest(new { success = false, error = "At least 2 points are required." });
    }

    var host = string.IsNullOrWhiteSpace(req.Host) ? "http://localhost:8989" : req.Host.TrimEnd('/');
    var userJson = string.IsNullOrWhiteSpace(req.UserJson) ? "{}" : req.UserJson;

    try
    {
        var enginePoints = req.Points.Select(p => new PointResponse { Lat = p.Lat, Lng = p.Lng }).ToList();
        var route = await AstreaEngine.AstreaEngine.AstreaRouteAsync(host, enginePoints, userJson);
        return Results.Ok(new
        {
            success = true,
            points = route,
            count = route.Count
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            success = false,
            error = ex.Message
        });
    }
});

app.MapGet("/api/health", async (string? host) =>
{
    var targetHost = string.IsNullOrWhiteSpace(host) ? "http://localhost:8989" : host.TrimEnd('/');
    try
    {
        var health = await AstreaEngine.AstreaEngine.AstreaHealthAsync(targetHost);
        return Results.Ok(new { success = true, health });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
});

Console.WriteLine("=================================================");
Console.WriteLine("  AstreaEngine GUI Playground running at:");
Console.WriteLine("  http://localhost:5050");
Console.WriteLine("=================================================");

app.Run();

public record RoutePoint(
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lng")] double Lng
);

public record RouteApiRequest(
    [property: JsonPropertyName("host")] string? Host,
    [property: JsonPropertyName("points")] List<RoutePoint>? Points,
    [property: JsonPropertyName("userJson")] string? UserJson
);

