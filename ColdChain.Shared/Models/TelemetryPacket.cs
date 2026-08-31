using System.Globalization;

namespace ColdChain.Shared.Models;

/// <summary>
/// A single strongly typed reading from a device.
/// The same class carries a double (temperature), an int (compressor amps)
/// or a bool (door open) without any casting or boxing at the call site.
/// </summary>
/// <typeparam name="T">The CLR type of the measured value.</typeparam>
public class TelemetryPacket<T>
{
    public string DeviceId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public T? Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public bool IsAnomaly { get; set; }

    // Co-authored by Claude
    /// <summary>
    /// Flattens the typed packet into a transport object.
    /// JSON has no way of preserving the generic argument, so the type name
    /// travels with the value and the client displays it in the ValueType column.
    /// </summary>
    public TelemetryDto ToDto()
    {
        return new TelemetryDto
        {
            DeviceId = DeviceId,
            MetricName = MetricName,
            Value = Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty,
            ValueType = typeof(T).Name,
            Unit = Unit,
            TimestampUtc = TimestampUtc,
            IsAnomaly = IsAnomaly
        };
    }

    // Co-authored by Claude
    public override string ToString() =>
        $"{TimestampUtc:HH:mm:ss} {DeviceId} {MetricName}=" +
        $"{Convert.ToString(Value, CultureInfo.InvariantCulture)}{Unit}" +
        (IsAnomaly ? " [ANOMALY]" : string.Empty);
}
