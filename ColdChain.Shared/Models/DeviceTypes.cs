namespace ColdChain.Shared.Models;

/// <summary>
/// The sensor categories the gateway understands. Each type produces a different
/// telemetry value type, which is what TelemetryPacket&lt;T&gt; is built for.
/// </summary>
public static class DeviceTypes
{
    public const string Temperature = "Temperature";   // double, degrees Celsius
    public const string Humidity    = "Humidity";      // double, percent RH
    public const string Compressor  = "Compressor";    // int, amps
    public const string Door        = "Door";          // bool, open / closed
    public const string Cooling     = "Cooling";       // bool, running / stopped

    public static readonly string[] All =
    {
        Temperature, Humidity, Compressor, Door, Cooling
    };

    public static bool IsKnown(string? deviceType) =>
        !string.IsNullOrWhiteSpace(deviceType) &&
        All.Any(t => string.Equals(t, deviceType, StringComparison.OrdinalIgnoreCase));
}
