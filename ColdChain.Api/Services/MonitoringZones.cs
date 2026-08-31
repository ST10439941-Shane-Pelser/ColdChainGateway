namespace ColdChain.Api.Services;

/// <summary>
/// The physical grouping of devices into monitoring zones.
/// A jagged array is the right shape here because every zone holds a different
/// number of devices. A rectangular 2D array would force empty padding cells
/// into the small zones.
/// </summary>
public static class MonitoringZones
{
    /// <summary>Friendly name of each zone. Index matches DeviceIdsByZone.</summary>
    public static readonly string[] ZoneNames =
    {
        "JHB Cold Room 1 (Chilled)",
        "JHB Cold Room 2 (Frozen)",
        "JHB Vehicle Bay 1",
        "PTA Cold Room 1 (Chilled)"
    };

    /// <summary>
    /// JAGGED ARRAY. Row = monitoring zone, columns = the device IDs in that zone.
    /// Row lengths deliberately differ: 4, 3, 2 and 3.
    /// </summary>
    public static readonly string[][] DeviceIdsByZone =
    {
        new[] { "TMP-001", "TMP-002", "HUM-001", "DOR-001" },
        new[] { "TMP-003", "CMP-001", "COO-001" },
        new[] { "TMP-004", "DOR-002" },
        new[] { "TMP-005", "HUM-002", "CMP-002" }
    };

    // Co-authored by Claude
    /// <summary>
    /// Walks the jagged array row by row and returns the zone a device belongs to,
    /// or -1 when the device is not mapped to any zone.
    /// </summary>
    public static int FindZoneIndex(string deviceId)
    {
        for (int zone = 0; zone < DeviceIdsByZone.Length; zone++)
        {
            // Inner loop length changes per row, which is the point of a jagged array.
            for (int slot = 0; slot < DeviceIdsByZone[zone].Length; slot++)
            {
                if (string.Equals(DeviceIdsByZone[zone][slot], deviceId, StringComparison.OrdinalIgnoreCase))
                    return zone;
            }
        }

        return -1;
    }

    // Co-authored by Claude
    public static string ZoneName(int zoneIndex) =>
        zoneIndex >= 0 && zoneIndex < ZoneNames.Length ? ZoneNames[zoneIndex] : "Unassigned";

    // Co-authored by Claude
    /// <summary>Total device slots across every row of the jagged array.</summary>
    public static int TotalMappedDevices()
    {
        int total = 0;
        foreach (string[] zone in DeviceIdsByZone)
            total += zone.Length;

        return total;
    }
}
