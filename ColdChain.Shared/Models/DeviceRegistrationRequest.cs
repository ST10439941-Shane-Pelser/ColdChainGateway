namespace ColdChain.Shared.Models;

/// <summary>
/// What the frontend posts to POST /api/devices.
/// </summary>
public class DeviceRegistrationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
