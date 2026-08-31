namespace ColdChain.Shared.Models;

/// <summary>
/// An abnormal reading that an operator needs to look at and sign off.
/// </summary>
public class AnomalyRecord
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime DetectedUtc { get; set; }

    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? OperatorNote { get; set; }
    public DateTime? AcknowledgedUtc { get; set; }
}
