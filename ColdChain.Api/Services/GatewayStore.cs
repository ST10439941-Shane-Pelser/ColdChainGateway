using ColdChain.Shared.Models;

namespace ColdChain.Api.Services;

/// <summary>
/// The gateway's in-memory database. Registered as a singleton, so a lock guards
/// the collections while the background simulator writes and HTTP requests read.
///
/// Every collection here is a List&lt;T&gt;, and the methods below are the required
/// add / search / filter / display operations over those lists.
/// </summary>
public class GatewayStore
{
    private readonly object _sync = new();

    private readonly List<Device> _devices = new();
    private readonly List<TelemetryDto> _telemetry = new();
    private readonly List<TemperatureReading> _temperatureHistory = new();
    private readonly List<AnomalyRecord> _anomalies = new();
    private readonly List<AttachmentMetadata> _attachments = new();

    private int _nextAnomalyId = 1;
    private int _nextAttachmentId = 1;

    /// <summary>Keeps memory bounded during a long demo run.</summary>
    private const int MaxTelemetryHistory = 600;
    private const int MaxTemperatureHistory = 400;

    // ---------------------------------------------------------------- devices

    // Co-authored by Claude
    public List<Device> GetDevices()
    {
        lock (_sync)
            return _devices.OrderBy(d => d.DeviceId).ToList();
    }

    // Co-authored by Claude
    /// <summary>Linear search of the device list by ID, case insensitive.</summary>
    public Device? FindDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;

        lock (_sync)
            return _devices.FirstOrDefault(d =>
                string.Equals(d.DeviceId, deviceId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // Co-authored by Claude
    public bool DeviceExists(string deviceId) => FindDevice(deviceId) is not null;

    // Co-authored by Claude
    /// <summary>Filters the device list by free text, type and active state.</summary>
    public List<Device> SearchDevices(string? search, string? deviceType, bool? isActive)
    {
        lock (_sync)
        {
            IEnumerable<Device> query = _devices;

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(d =>
                    d.DeviceId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.DeviceName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.LocationCode.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(deviceType))
                query = query.Where(d => string.Equals(d.DeviceType, deviceType, StringComparison.OrdinalIgnoreCase));

            if (isActive.HasValue)
                query = query.Where(d => d.IsActive == isActive.Value);

            return query.OrderBy(d => d.DeviceId).ToList();
        }
    }

    // Co-authored by Claude
    public void AddDevice(Device device)
    {
        lock (_sync)
            _devices.Add(device);
    }

    // ---------------------------------------------------------------- telemetry

    // Co-authored by Claude
    public void AddTelemetry(TelemetryDto packet)
    {
        lock (_sync)
        {
            _telemetry.Add(packet);

            if (_telemetry.Count > MaxTelemetryHistory)
                _telemetry.RemoveRange(0, _telemetry.Count - MaxTelemetryHistory);
        }
    }

    // Co-authored by Claude
    /// <summary>Newest first, optionally filtered by device, metric or anomaly state.</summary>
    public List<TelemetryDto> GetTelemetry(string? deviceId, string? metricName, bool? onlyAnomalies, int take)
    {
        lock (_sync)
        {
            IEnumerable<TelemetryDto> query = _telemetry;

            if (!string.IsNullOrWhiteSpace(deviceId))
                query = query.Where(t => string.Equals(t.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(metricName))
                query = query.Where(t => t.MetricName.Contains(metricName, StringComparison.OrdinalIgnoreCase));

            if (onlyAnomalies == true)
                query = query.Where(t => t.IsAnomaly);

            return query.OrderByDescending(t => t.TimestampUtc)
                        .Take(take <= 0 ? 100 : take)
                        .ToList();
        }
    }

    // Co-authored by Claude
    public void AddTemperatureReading(TemperatureReading reading)
    {
        lock (_sync)
        {
            _temperatureHistory.Add(reading);

            if (_temperatureHistory.Count > MaxTemperatureHistory)
                _temperatureHistory.RemoveRange(0, _temperatureHistory.Count - MaxTemperatureHistory);
        }
    }

    // Co-authored by Claude
    /// <summary>
    /// The most recent temperature reading for each of the supplied device IDs.
    /// Used by the zone-average endpoint that demonstrates the overloaded + operator.
    /// </summary>
    public List<TemperatureReading> LatestTemperatureReadings(IEnumerable<string> deviceIds)
    {
        lock (_sync)
        {
            var results = new List<TemperatureReading>();

            foreach (string id in deviceIds)
            {
                TemperatureReading? latest = _temperatureHistory
                    .Where(r => string.Equals(r.DeviceId, id, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.TimestampUtc)
                    .FirstOrDefault();

                if (latest is not null)
                    results.Add(latest);
            }

            return results;
        }
    }

    // ---------------------------------------------------------------- anomalies

    // Co-authored by Claude
    public AnomalyRecord AddAnomaly(AnomalyRecord anomaly)
    {
        lock (_sync)
        {
            anomaly.Id = _nextAnomalyId++;
            _anomalies.Add(anomaly);
            return anomaly;
        }
    }

    // Co-authored by Claude
    public List<AnomalyRecord> GetAnomalies(bool? acknowledged, string? deviceId, int take)
    {
        lock (_sync)
        {
            IEnumerable<AnomalyRecord> query = _anomalies;

            if (acknowledged.HasValue)
                query = query.Where(a => a.IsAcknowledged == acknowledged.Value);

            if (!string.IsNullOrWhiteSpace(deviceId))
                query = query.Where(a => string.Equals(a.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));

            return query.OrderByDescending(a => a.DetectedUtc)
                        .Take(take <= 0 ? 100 : take)
                        .ToList();
        }
    }

    // Co-authored by Claude
    public AnomalyRecord? FindAnomaly(int id)
    {
        lock (_sync)
            return _anomalies.FirstOrDefault(a => a.Id == id);
    }

    // ---------------------------------------------------------------- attachments

    // Co-authored by Claude
    public AttachmentMetadata AddAttachment(AttachmentMetadata attachment)
    {
        lock (_sync)
        {
            attachment.Id = _nextAttachmentId++;
            _attachments.Add(attachment);

            Device? device = _devices.FirstOrDefault(d =>
                string.Equals(d.DeviceId, attachment.DeviceId, StringComparison.OrdinalIgnoreCase));

            if (device is not null)
                device.AttachmentCount++;

            return attachment;
        }
    }

    // Co-authored by Claude
    public List<AttachmentMetadata> GetAttachments(string deviceId)
    {
        lock (_sync)
            return _attachments
                .Where(a => string.Equals(a.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.UploadedUtc)
                .ToList();
    }
}
