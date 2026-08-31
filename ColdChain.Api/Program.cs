using ColdChain.Api.Services;
using Microsoft.AspNetCore.Http.Features;

namespace ColdChain.Api;

/// <summary>
/// The FreshRoute Cold-Chain Gateway API.
/// </summary>
public class Program
{
    // Co-authored by Claude
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        // Gateway state and the location hierarchy live for the lifetime of the process.
        builder.Services.AddSingleton<GatewayStore>();
        builder.Services.AddSingleton<LocationTreeService>();
        builder.Services.AddSingleton<TelemetrySimulator>();
        builder.Services.AddScoped<DeviceValidator>();

        // Stands in for the remote devices, emitting telemetry every five seconds.
        builder.Services.AddHostedService<TelemetryBackgroundService>();

        // Keep multipart uploads inside the evidence-file limit.
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = AttachmentRules.MaxBytes;
        });

        // The WinForms client is not a browser, but CORS keeps the door open for a
        // web frontend without any further changes.
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        WebApplication app = builder.Build();

        app.UseCors();
        app.MapControllers();

        // A plain landing page so you can see at a glance that the gateway is up.
        app.MapGet("/", () => Results.Json(new
        {
            Service = "FreshRoute Cold-Chain Monitoring Gateway",
            Status = "Running",
            TimeUtc = DateTime.UtcNow,
            Endpoints = new[]
            {
                "GET  /api/devices",
                "GET  /api/devices/{id}",
                "POST /api/devices",
                "POST /api/devices/{id}/attachments   (multipart/form-data)",
                "GET  /api/devices/{id}/attachments",
                "GET  /api/telemetry",
                "GET  /api/telemetry/zones",
                "GET  /api/telemetry/zones/{zoneIndex}/average-temperature",
                "GET  /api/anomalies",
                "POST /api/anomalies/{id}/acknowledge",
                "GET  /api/locations",
                "GET  /api/locations/options",
                "GET  /api/locations/validate/{code}"
            }
        }));

        app.Run();
    }
}
