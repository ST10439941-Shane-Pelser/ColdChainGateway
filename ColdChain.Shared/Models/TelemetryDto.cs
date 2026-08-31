namespace ColdChain.Shared.Models;

/// <summary>
/// The wire format for a telemetry reading. Value is carried as text and
/// ValueType records which generic type produced it (Double, Int32, Boolean).
/// </summary>
public class TelemetryDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public bool IsAnomaly { get; set; }
}
