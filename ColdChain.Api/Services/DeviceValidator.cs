using System.Text.RegularExpressions;
using ColdChain.Shared.Models;

namespace ColdChain.Api.Services;

/// <summary>
/// Registration rules for a monitoring device. Everything the API rejects is
/// decided here so the controller stays thin and the messages stay consistent.
/// </summary>
public class DeviceValidator
{
    private readonly GatewayStore _store;
    private readonly LocationTreeService _locations;

    /// <summary>Three letters, a hyphen, three digits. For example TMP-006.</summary>
    private static readonly Regex DeviceIdPattern = new(@"^[A-Za-z]{3}-\d{3}$", RegexOptions.Compiled);

    public DeviceValidator(GatewayStore store, LocationTreeService locations)
    {
        _store = store;
        _locations = locations;
    }

    // Co-authored by Claude
    /// <summary>
    /// Validates a registration request. Returns every problem found rather than
    /// stopping at the first, so the operator can fix the form in one pass.
    /// </summary>
    public List<string> Validate(DeviceRegistrationRequest request, out string locationPath)
    {
        var errors = new List<string>();
        locationPath = string.Empty;

        if (request is null)
        {
            errors.Add("No registration payload was supplied.");
            return errors;
        }

        // --- Device ID ---
        string deviceId = request.DeviceId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            errors.Add("Device ID is required.");
        }
        else
        {
            if (!DeviceIdPattern.IsMatch(deviceId))
                errors.Add($"Device ID '{deviceId}' is invalid. Use three letters, a hyphen and three digits, for example TMP-006.");

            if (_store.DeviceExists(deviceId))
                errors.Add($"Device ID '{deviceId}' is already registered.");
        }

        // --- Name ---
        if (string.IsNullOrWhiteSpace(request.DeviceName))
            errors.Add("Device name is required.");
        else if (request.DeviceName.Trim().Length < 3)
            errors.Add("Device name must be at least 3 characters.");

        // --- Type ---
        if (!DeviceTypes.IsKnown(request.DeviceType))
            errors.Add($"Device type must be one of: {string.Join(", ", DeviceTypes.All)}.");

        // --- Location, validated recursively against the hierarchy ---
        if (string.IsNullOrWhiteSpace(request.LocationCode))
        {
            errors.Add("Location code is required.");
        }
        else if (!_locations.IsValidDeviceLocation(request.LocationCode.Trim(), out string reason, out string path))
        {
            errors.Add(reason);
        }
        else
        {
            locationPath = path;
        }

        return errors;
    }
}
