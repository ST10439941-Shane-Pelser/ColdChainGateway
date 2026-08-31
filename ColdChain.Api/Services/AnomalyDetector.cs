using ColdChain.Shared.Models;

namespace ColdChain.Api.Services;

/// <summary>
/// The cold-chain rules. Each device type has its own acceptable band and a
/// readable reason string that ends up on the anomaly dashboard.
/// </summary>
public static class AnomalyDetector
{
    // Chilled cold-chain band for food and medicine.
    public const double MinChilledCelsius = 2.0;
    public const double MaxChilledCelsius = 8.0;

    // Frozen band, used by devices in a frozen cold room.
    public const double MinFrozenCelsius = -22.0;
    public const double MaxFrozenCelsius = -16.0;

    public const double MinHumidityPercent = 45.0;
    public const double MaxHumidityPercent = 85.0;

    public const int MaxCompressorAmps = 15;

    // Co-authored by Claude
    /// <summary>True when the device sits in the frozen cold room and uses the frozen band.</summary>
    public static bool IsFrozenLocation(string locationCode) =>
        locationCode.StartsWith("SH-JHB-02", StringComparison.OrdinalIgnoreCase);

    // Co-authored by Claude
    public static bool CheckTemperature(double celsius, bool frozen, out string reason)
    {
        double min = frozen ? MinFrozenCelsius : MinChilledCelsius;
        double max = frozen ? MaxFrozenCelsius : MaxChilledCelsius;
        string band = frozen ? "frozen" : "chilled";

        if (celsius < min)
        {
            reason = $"Temperature {celsius:0.0} C is below the {band} minimum of {min:0.0} C (product freeze / damage risk).";
            return true;
        }

        if (celsius > max)
        {
            reason = $"Temperature {celsius:0.0} C is above the {band} maximum of {max:0.0} C (cold-chain breach).";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    // Co-authored by Claude
    public static bool CheckHumidity(double percent, out string reason)
    {
        if (percent < MinHumidityPercent || percent > MaxHumidityPercent)
        {
            reason = $"Humidity {percent:0.0} % is outside the acceptable range " +
                     $"{MinHumidityPercent:0}-{MaxHumidityPercent:0} %.";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    // Co-authored by Claude
    public static bool CheckCompressorCurrent(int amps, out string reason)
    {
        if (amps > MaxCompressorAmps)
        {
            reason = $"Compressor draw {amps} A exceeds the {MaxCompressorAmps} A limit (possible seizing compressor).";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    // Co-authored by Claude
    public static bool CheckDoor(bool isOpen, out string reason)
    {
        if (isOpen)
        {
            reason = "Cold room door reported OPEN.";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    // Co-authored by Claude
    public static bool CheckCooling(bool isRunning, out string reason)
    {
        if (!isRunning)
        {
            reason = "Cooling system reported STOPPED.";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    // Co-authored by Claude
    /// <summary>
    /// Applies the same range rule used for live readings to a combined zone reading
    /// produced by the overloaded + operator.
    /// </summary>
    public static bool CheckZoneAverage(TemperatureReading combined, bool frozen, out string reason) =>
        CheckTemperature(combined.Celsius, frozen, out reason);
}
