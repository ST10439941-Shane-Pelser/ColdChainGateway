using ColdChain.Api.Services;
using ColdChain.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ColdChain.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly GatewayStore _store;
    private readonly TelemetrySimulator _simulator;

    public TelemetryController(GatewayStore store, TelemetrySimulator simulator)
    {
        _store = store;
        _simulator = simulator;
    }

    // Co-authored by Claude
    /// <summary>
    /// GET /api/telemetry - the mixed telemetry stream, newest first.
    /// The ValueType column shows which TelemetryPacket&lt;T&gt; produced each row.
    /// </summary>
    [HttpGet]
    public ActionResult<List<TelemetryDto>> GetTelemetry(
        [FromQuery] string? deviceId,
        [FromQuery] string? metricName,
        [FromQuery] bool? onlyAnomalies,
        [FromQuery] int take = 100)
    {
        if (take is < 1 or > 600)
            return BadRequest(new ApiError("The 'take' parameter must be between 1 and 600."));

        return Ok(_store.GetTelemetry(deviceId, metricName, onlyAnomalies, take));
    }

    // Co-authored by Claude
    /// <summary>
    /// GET /api/telemetry/zones - the jagged array of monitoring zones and their devices.
    /// </summary>
    [HttpGet("zones")]
    public ActionResult<List<ZoneInfo>> GetZones()
    {
        var zones = new List<ZoneInfo>();

        // Row by row through the jagged array. Each row has its own length.
        for (int i = 0; i < MonitoringZones.DeviceIdsByZone.Length; i++)
        {
            zones.Add(new ZoneInfo
            {
                ZoneIndex = i,
                ZoneName = MonitoringZones.ZoneName(i),
                DeviceIds = MonitoringZones.DeviceIdsByZone[i],
                DeviceCount = MonitoringZones.DeviceIdsByZone[i].Length
            });
        }

        return Ok(zones);
    }

    // Co-authored by Claude
    /// <summary>
    /// GET /api/telemetry/zones/{zoneIndex}/average-temperature
    /// Combines the latest reading from every temperature probe in the zone using
    /// the overloaded + operator on TemperatureReading.
    /// </summary>
    [HttpGet("zones/{zoneIndex:int}/average-temperature")]
    public ActionResult<ZoneTemperatureSummary> GetZoneAverage(int zoneIndex)
    {
        ZoneTemperatureSummary? summary = _simulator.CombineZoneTemperatures(zoneIndex);

        if (summary is null)
            return NotFound(new ApiError(
                $"Zone {zoneIndex} does not exist. Valid zones are 0 to {MonitoringZones.DeviceIdsByZone.Length - 1}."));

        return Ok(summary);
    }
}
