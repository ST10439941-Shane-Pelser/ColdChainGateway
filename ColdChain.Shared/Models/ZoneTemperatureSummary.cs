namespace ColdChain.Shared.Models;

/// <summary>
/// The result of folding every temperature reading in a monitoring zone together
/// with the overloaded + operator.
/// </summary>
public class ZoneTemperatureSummary
{
    public int ZoneIndex { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string[] DeviceIds { get; set; } = Array.Empty<string>();
    public int ReadingsCombined { get; set; }
    public double AverageCelsius { get; set; }
    public DateTime LatestTimestampUtc { get; set; }
    public bool IsOutOfRange { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
