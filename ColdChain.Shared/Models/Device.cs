namespace ColdChain.Shared.Models;

/// <summary>
/// A monitoring device installed in a cold room, shelf or vehicle bay.
/// </summary>
public class Device
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Human readable path through the location tree, e.g. Johannesburg Depot / Cold Room 1 / Shelf A.</summary>
    public string LocationPath { get; set; } = string.Empty;

    public int AttachmentCount { get; set; }

    public override string ToString() => $"{DeviceId} ({DeviceType}) @ {LocationCode}";
}
