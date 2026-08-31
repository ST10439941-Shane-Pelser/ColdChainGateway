using System.Globalization;

namespace ColdChain.Shared.Models;

/// <summary>
/// An immutable temperature sample. Two readings can be added together with the
/// + operator to produce a single combined reading for a zone, which is how the
/// gateway rolls several sensors in the same cold room into one figure.
/// </summary>
public class TemperatureReading
{
    public string DeviceId { get; }
    public double Celsius { get; }
    public DateTime TimestampUtc { get; }

    /// <summary>How many raw samples this reading represents. A fresh reading is 1.</summary>
    public int SampleCount { get; }

    public TemperatureReading(string deviceId, double celsius, DateTime timestampUtc, int sampleCount = 1)
    {
        if (sampleCount < 1)
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "A reading must represent at least one sample.");

        DeviceId = deviceId;
        Celsius = celsius;
        TimestampUtc = timestampUtc;
        SampleCount = sampleCount;
    }

    // Co-authored by Claude
    /// <summary>
    /// Combines two readings into one averaged reading.
    /// The average is weighted by SampleCount so that adding an already combined
    /// reading to a single reading does not over-weight the single one:
    /// (4C x 3 samples) + (10C x 1 sample) = 5.5C over 4 samples, not 7C.
    /// The result keeps the later timestamp, because that is the age of the
    /// freshest evidence behind the combined figure.
    /// </summary>
    public static TemperatureReading operator +(TemperatureReading left, TemperatureReading right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        int combinedSamples = left.SampleCount + right.SampleCount;

        double weightedAverage =
            ((left.Celsius * left.SampleCount) + (right.Celsius * right.SampleCount)) / combinedSamples;

        string combinedId = left.DeviceId == right.DeviceId
            ? left.DeviceId
            : $"{left.DeviceId}+{right.DeviceId}";

        DateTime latest = left.TimestampUtc > right.TimestampUtc ? left.TimestampUtc : right.TimestampUtc;

        return new TemperatureReading(combinedId, Math.Round(weightedAverage, 2), latest, combinedSamples);
    }

    // Co-authored by Claude
    public override string ToString() =>
        $"{Celsius.ToString("0.00", CultureInfo.InvariantCulture)} C " +
        $"from {SampleCount} sample(s) [{DeviceId}] at {TimestampUtc:HH:mm:ss} UTC";
}
