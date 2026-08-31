using System.Globalization;
using ColdChain.Shared.Models;

namespace ColdChain.Api.Services;

/// <summary>
/// Stands in for the remote cold-chain hardware. It seeds a fleet of devices and
/// then produces mixed telemetry on a timer, so the API always has realistic data
/// without any physical sensors being present.
/// </summary>
public class TelemetrySimulator
{
    private readonly GatewayStore _store;
    private readonly LocationTreeService _locations;
    private readonly Random _random = new();

    /// <summary>Last value produced per device, so readings drift instead of jumping randomly.</summary>
    private readonly Dictionary<string, double> _lastValues = new(StringComparer.OrdinalIgnoreCase);

    public TelemetrySimulator(GatewayStore store, LocationTreeService locations)
    {
        _store = store;
        _locations = locations;
    }

    // Co-authored by Claude
    /// <summary>
    /// Registers the starting fleet and back-fills a short history so the dashboard
    /// is populated the moment the frontend connects.
    /// </summary>
    public void Seed()
    {
        (string id, string name, string type, string location)[] fleet =
        {
            ("TMP-001", "Cold Room 1 Probe A",   DeviceTypes.Temperature, "SH-JHB-01A"),
            ("TMP-002", "Cold Room 1 Probe B",   DeviceTypes.Temperature, "SH-JHB-01B"),
            ("HUM-001", "Cold Room 1 Humidity",  DeviceTypes.Humidity,    "SH-JHB-01A"),
            ("DOR-001", "Cold Room 1 Door",      DeviceTypes.Door,        "SH-JHB-01A"),
            ("TMP-003", "Freezer Probe A",       DeviceTypes.Temperature, "SH-JHB-02A"),
            ("CMP-001", "Freezer Compressor",    DeviceTypes.Compressor,  "SH-JHB-02A"),
            ("COO-001", "Freezer Cooling Unit",  DeviceTypes.Cooling,     "SH-JHB-02B"),
            ("TMP-004", "Truck 114 Probe",       DeviceTypes.Temperature, "VEH-JHB-114"),
            ("DOR-002", "Truck 114 Rear Door",   DeviceTypes.Door,        "VEH-JHB-114"),
            ("TMP-005", "PTA Cold Room Probe",   DeviceTypes.Temperature, "SH-PTA-01A"),
            ("HUM-002", "PTA Cold Room Humidity",DeviceTypes.Humidity,    "SH-PTA-01A"),
            ("CMP-002", "PTA Compressor",        DeviceTypes.Compressor,  "SH-PTA-01B")
        };

        foreach ((string id, string name, string type, string location) in fleet)
        {
            if (_store.DeviceExists(id))
                continue;

            _locations.IsValidDeviceLocation(location, out _, out string path);

            _store.AddDevice(new Device
            {
                DeviceId = id,
                DeviceName = name,
                DeviceType = type,
                LocationCode = location,
                LocationPath = path,
                IsActive = true,
                RegisteredUtc = DateTime.UtcNow.AddDays(-30)
            });
        }

        // Back-fill roughly the last ten minutes of readings.
        for (int i = 20; i > 0; i--)
            Tick(DateTime.UtcNow.AddSeconds(-i * 30));
    }

    // Co-authored by Claude
    /// <summary>
    /// Produces one reading for every active device. The switch chooses which closed
    /// version of TelemetryPacket&lt;T&gt; to build: double, int or bool.
    /// </summary>
    public void Tick(DateTime? timestampUtc = null)
    {
        DateTime stamp = timestampUtc ?? DateTime.UtcNow;

        foreach (Device device in _store.GetDevices().Where(d => d.IsActive))
        {
            switch (device.DeviceType)
            {
                case DeviceTypes.Temperature:
                    EmitTemperature(device, stamp);
                    break;

                case DeviceTypes.Humidity:
                    EmitHumidity(device, stamp);
                    break;

                case DeviceTypes.Compressor:
                    EmitCompressor(device, stamp);
                    break;

                case DeviceTypes.Door:
                    EmitDoor(device, stamp);
                    break;

                case DeviceTypes.Cooling:
                    EmitCooling(device, stamp);
                    break;
            }
        }
    }

    // Co-authored by Claude
    /// <summary>TelemetryPacket&lt;double&gt; - degrees Celsius.</summary>
    private void EmitTemperature(Device device, DateTime stamp)
    {
        bool frozen = AnomalyDetector.IsFrozenLocation(device.LocationCode);
        double baseline = frozen ? -19.0 : 4.5;

        double last = _lastValues.TryGetValue(device.DeviceId, out double previous) ? previous : baseline;
        double value = Math.Round(last + (_random.NextDouble() - 0.5) * 0.8, 2);

        // Pull the value gently back towards the baseline so it does not wander off.
        value = Math.Round(value + (baseline - value) * 0.15, 2);

        // Roughly one reading in twelve is an excursion (door left open, defrost cycle).
        if (_random.Next(0, 12) == 0)
            value = Math.Round(value + _random.NextDouble() * 6.0 + 2.0, 2);

        _lastValues[device.DeviceId] = value;

        bool isAnomaly = AnomalyDetector.CheckTemperature(value, frozen, out string reason);

        var packet = new TelemetryPacket<double>
        {
            DeviceId = device.DeviceId,
            MetricName = "Temperature",
            Value = value,
            Unit = "C",
            TimestampUtc = stamp,
            IsAnomaly = isAnomaly
        };

        Record(packet, device, reason);

        // Kept separately as a TemperatureReading so the zone endpoint can add them with +.
        _store.AddTemperatureReading(new TemperatureReading(device.DeviceId, value, stamp));
    }

    // Co-authored by Claude
    /// <summary>TelemetryPacket&lt;double&gt; - percent relative humidity.</summary>
    private void EmitHumidity(Device device, DateTime stamp)
    {
        double last = _lastValues.TryGetValue(device.DeviceId, out double previous) ? previous : 65.0;
        double value = Math.Round(Math.Clamp(last + (_random.NextDouble() - 0.5) * 6.0, 20.0, 99.0), 1);

        if (_random.Next(0, 15) == 0)
            value = Math.Round(Math.Clamp(value + _random.Next(10, 25), 20.0, 99.0), 1);

        _lastValues[device.DeviceId] = value;

        bool isAnomaly = AnomalyDetector.CheckHumidity(value, out string reason);

        Record(new TelemetryPacket<double>
        {
            DeviceId = device.DeviceId,
            MetricName = "Humidity",
            Value = value,
            Unit = "%",
            TimestampUtc = stamp,
            IsAnomaly = isAnomaly
        }, device, reason);
    }

    // Co-authored by Claude
    /// <summary>TelemetryPacket&lt;int&gt; - compressor current in whole amps.</summary>
    private void EmitCompressor(Device device, DateTime stamp)
    {
        int value = _random.Next(6, 13);

        if (_random.Next(0, 14) == 0)
            value = _random.Next(16, 23);

        bool isAnomaly = AnomalyDetector.CheckCompressorCurrent(value, out string reason);

        Record(new TelemetryPacket<int>
        {
            DeviceId = device.DeviceId,
            MetricName = "CompressorCurrent",
            Value = value,
            Unit = "A",
            TimestampUtc = stamp,
            IsAnomaly = isAnomaly
        }, device, reason);
    }

    // Co-authored by Claude
    /// <summary>TelemetryPacket&lt;bool&gt; - door open state.</summary>
    private void EmitDoor(Device device, DateTime stamp)
    {
        bool isOpen = _random.Next(0, 10) == 0;
        bool isAnomaly = AnomalyDetector.CheckDoor(isOpen, out string reason);

        Record(new TelemetryPacket<bool>
        {
            DeviceId = device.DeviceId,
            MetricName = "DoorOpen",
            Value = isOpen,
            Unit = string.Empty,
            TimestampUtc = stamp,
            IsAnomaly = isAnomaly
        }, device, reason);
    }

    // Co-authored by Claude
    /// <summary>TelemetryPacket&lt;bool&gt; - cooling unit running state.</summary>
    private void EmitCooling(Device device, DateTime stamp)
    {
        bool isRunning = _random.Next(0, 18) != 0;
        bool isAnomaly = AnomalyDetector.CheckCooling(isRunning, out string reason);

        Record(new TelemetryPacket<bool>
        {
            DeviceId = device.DeviceId,
            MetricName = "CoolingActive",
            Value = isRunning,
            Unit = string.Empty,
            TimestampUtc = stamp,
            IsAnomaly = isAnomaly
        }, device, reason);
    }

    // Co-authored by Claude
    /// <summary>
    /// Generic helper: stores any TelemetryPacket&lt;T&gt; as a DTO and raises an
    /// anomaly record when the reading broke a rule.
    /// </summary>
    private void Record<T>(TelemetryPacket<T> packet, Device device, string reason)
    {
        TelemetryDto dto = packet.ToDto();
        _store.AddTelemetry(dto);

        if (!packet.IsAnomaly)
            return;

        _store.AddAnomaly(new AnomalyRecord
        {
            DeviceId = device.DeviceId,
            DeviceName = device.DeviceName,
            LocationCode = device.LocationCode,
            MetricName = dto.MetricName,
            Value = dto.Value,
            Unit = dto.Unit,
            Reason = reason,
            DetectedUtc = packet.TimestampUtc,
            IsAcknowledged = false
        });
    }

    // Co-authored by Claude
    /// <summary>
    /// Folds every current temperature reading in a zone into one figure using the
    /// overloaded + operator on TemperatureReading. The zone's device list comes
    /// from the jagged array in MonitoringZones.
    /// </summary>
    public ZoneTemperatureSummary? CombineZoneTemperatures(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= MonitoringZones.DeviceIdsByZone.Length)
            return null;

        string[] zoneDevices = MonitoringZones.DeviceIdsByZone[zoneIndex];

        // Only temperature devices can contribute to a temperature average.
        List<string> temperatureDevices = zoneDevices
            .Where(id => _store.FindDevice(id)?.DeviceType == DeviceTypes.Temperature)
            .ToList();

        List<TemperatureReading> readings = _store.LatestTemperatureReadings(temperatureDevices);

        if (readings.Count == 0)
        {
            return new ZoneTemperatureSummary
            {
                ZoneIndex = zoneIndex,
                ZoneName = MonitoringZones.ZoneName(zoneIndex),
                DeviceIds = zoneDevices,
                ReadingsCombined = 0,
                Explanation = "No temperature readings are available for this zone yet."
            };
        }

        // THE OPERATOR OVERLOAD IN USE: fold the list with +.
        TemperatureReading combined = readings[0];
        for (int i = 1; i < readings.Count; i++)
            combined += readings[i];

        Device? first = _store.FindDevice(temperatureDevices.First());
        bool frozen = first is not null && AnomalyDetector.IsFrozenLocation(first.LocationCode);

        bool outOfRange = AnomalyDetector.CheckZoneAverage(combined, frozen, out string reason);

        string sum = string.Join(" + ", readings.Select(r =>
            r.Celsius.ToString("0.00", CultureInfo.InvariantCulture) + " C"));

        return new ZoneTemperatureSummary
        {
            ZoneIndex = zoneIndex,
            ZoneName = MonitoringZones.ZoneName(zoneIndex),
            DeviceIds = zoneDevices,
            ReadingsCombined = combined.SampleCount,
            AverageCelsius = combined.Celsius,
            LatestTimestampUtc = combined.TimestampUtc,
            IsOutOfRange = outOfRange,
            Explanation = outOfRange
                ? $"{sum} = {combined.Celsius.ToString("0.00", CultureInfo.InvariantCulture)} C. {reason}"
                : $"{sum} = {combined.Celsius.ToString("0.00", CultureInfo.InvariantCulture)} C. Zone is within range."
        };
    }
}
