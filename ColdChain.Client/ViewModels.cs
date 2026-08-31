using System.Globalization;
using ColdChain.Shared.Models;

namespace ColdChain.Client;

/// <summary>Display shapes for the grids. Keeping them separate from the API models
/// means the grid columns are exactly what the operator should see, in order.</summary>
public class DeviceRow
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Active { get; set; } = string.Empty;
    public int Files { get; set; }
    public string Registered { get; set; } = string.Empty;

    // Co-authored by Claude
    public static List<DeviceRow> From(IEnumerable<Device> devices) =>
        devices.Select(d => new DeviceRow
        {
            DeviceId = d.DeviceId,
            Name = d.DeviceName,
            Type = d.DeviceType,
            Location = d.LocationCode,
            Path = d.LocationPath,
            Active = d.IsActive ? "Yes" : "No",
            Files = d.AttachmentCount,
            Registered = d.RegisteredUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        }).ToList();
}

public class TelemetryRow
{
    public string Time { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Co-authored by Claude
    public static List<TelemetryRow> From(IEnumerable<TelemetryDto> packets) =>
        packets.Select(t => new TelemetryRow
        {
            Time = t.TimestampUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            Device = t.DeviceId,
            Metric = t.MetricName,
            Value = t.Value,
            Unit = t.Unit,
            ValueType = t.ValueType,
            Status = t.IsAnomaly ? "ANOMALY" : "OK"
        }).ToList();
}

public class AnomalyRow
{
    public int Id { get; set; }
    public string Detected { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Acknowledged { get; set; } = string.Empty;
    public string By { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;

    // Co-authored by Claude
    public static List<AnomalyRow> From(IEnumerable<AnomalyRecord> anomalies) =>
        anomalies.Select(a => new AnomalyRow
        {
            Id = a.Id,
            Detected = a.DetectedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            Device = $"{a.DeviceId} ({a.DeviceName})",
            Metric = a.MetricName,
            Value = string.IsNullOrEmpty(a.Unit) ? a.Value : $"{a.Value} {a.Unit}",
            Reason = a.Reason,
            Acknowledged = a.IsAcknowledged ? "Yes" : "No",
            By = a.AcknowledgedBy ?? string.Empty,
            Note = a.OperatorNote ?? string.Empty
        }).ToList();
}
