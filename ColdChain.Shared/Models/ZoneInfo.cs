namespace ColdChain.Shared.Models;

/// <summary>
/// One row of the jagged monitoring-zone array, described for the frontend.
/// </summary>
public class ZoneInfo
{
    public int ZoneIndex { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string[] DeviceIds { get; set; } = Array.Empty<string>();
    public int DeviceCount { get; set; }

    public override string ToString() => $"Zone {ZoneIndex}: {ZoneName} ({DeviceCount} devices)";
}
