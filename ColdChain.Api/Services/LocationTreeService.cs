using ColdChain.Shared.Models;

namespace ColdChain.Api.Services;

/// <summary>
/// Holds the FreshRoute monitoring hierarchy and validates location codes against it
/// using recursion. Registration will not accept a code this class cannot find.
/// </summary>
public class LocationTreeService
{
    public LocationNode Root { get; }

    public LocationTreeService()
    {
        Root = BuildTree();
    }

    // Co-authored by Claude
    /// <summary>
    /// Builds the fixed location hierarchy for the exercise.
    /// Network -> Depot -> Cold Room / Vehicle Bay -> Shelf / Vehicle.
    /// </summary>
    private static LocationNode BuildTree()
    {
        return new LocationNode("FR-NET", "FreshRoute Network",
            new LocationNode("DEP-JHB", "Johannesburg Depot",
                new LocationNode("CR-JHB-01", "Cold Room 1",
                    new LocationNode("SH-JHB-01A", "Shelf A"),
                    new LocationNode("SH-JHB-01B", "Shelf B")),
                new LocationNode("CR-JHB-02", "Cold Room 2 (Frozen)",
                    new LocationNode("SH-JHB-02A", "Shelf A"),
                    new LocationNode("SH-JHB-02B", "Shelf B")),
                new LocationNode("VB-JHB-01", "Vehicle Bay 1",
                    new LocationNode("VEH-JHB-114", "Reefer Truck 114"),
                    new LocationNode("VEH-JHB-119", "Reefer Truck 119"))),
            new LocationNode("DEP-PTA", "Pretoria Depot",
                new LocationNode("CR-PTA-01", "Cold Room 1",
                    new LocationNode("SH-PTA-01A", "Shelf A"),
                    new LocationNode("SH-PTA-01B", "Shelf B")),
                new LocationNode("VB-PTA-01", "Vehicle Bay 1",
                    new LocationNode("VEH-PTA-207", "Reefer Van 207"))));
    }

    // Co-authored by Claude
    /// <summary>
    /// RECURSIVE SEARCH. Walks the hierarchy depth first looking for a location code.
    /// Base case: the current node matches, or it has no children left to search.
    /// Recursive case: ask every child to search its own subtree.
    /// </summary>
    /// <returns>The matching node, or null if the code does not exist anywhere in the tree.</returns>
    public LocationNode? FindByCode(LocationNode node, string code)
    {
        if (node is null || string.IsNullOrWhiteSpace(code))
            return null;

        // Base case: this node is the one we are looking for.
        if (string.Equals(node.Code, code.Trim(), StringComparison.OrdinalIgnoreCase))
            return node;

        // Recursive case: search each subtree in turn.
        foreach (LocationNode child in node.Children)
        {
            LocationNode? match = FindByCode(child, code);
            if (match is not null)
                return match;
        }

        // Base case: nothing matched anywhere below this node.
        return null;
    }

    // Co-authored by Claude
    /// <summary>Convenience overload that always starts the recursive search at the root.</summary>
    public LocationNode? FindByCode(string code) => FindByCode(Root, code);

    // Co-authored by Claude
    /// <summary>
    /// Validates a location code for device registration. A code is only valid if the
    /// recursive search finds it AND the node is a leaf, because a device sits on a
    /// shelf or in a vehicle, not on a whole depot.
    /// </summary>
    public bool IsValidDeviceLocation(string code, out string reason, out string path)
    {
        reason = string.Empty;
        path = string.Empty;

        LocationNode? node = FindByCode(Root, code);

        if (node is null)
        {
            reason = $"Location code '{code}' does not exist in the monitoring hierarchy.";
            return false;
        }

        if (!node.IsLeaf)
        {
            reason = $"Location code '{code}' is a grouping level ({node.Name}). " +
                     "Devices must be registered against a shelf or vehicle.";
            return false;
        }

        path = BuildPath(Root, node.Code) ?? node.Name;
        return true;
    }

    // Co-authored by Claude
    /// <summary>
    /// RECURSIVE PATH BUILD. Returns the readable path from the root down to a code,
    /// for example "Johannesburg Depot / Cold Room 1 / Shelf A".
    /// Each level prepends its own name to whatever the recursive call returns.
    /// </summary>
    public string? BuildPath(LocationNode node, string code)
    {
        if (node is null)
            return null;

        // Base case: found it, the path is just this node's name.
        if (string.Equals(node.Code, code, StringComparison.OrdinalIgnoreCase))
            return node.Name;

        // Recursive case: if a child can build a path, prefix it with this node's name.
        foreach (LocationNode child in node.Children)
        {
            string? childPath = BuildPath(child, code);
            if (childPath is not null)
                return node == Root ? childPath : $"{node.Name} / {childPath}";
        }

        return null;
    }

    // Co-authored by Claude
    /// <summary>
    /// RECURSIVE FLATTEN. Collects every node in the tree into a flat list so the
    /// frontend can populate its location dropdown without knowing the shape of the tree.
    /// </summary>
    public List<LocationOption> Flatten(LocationNode node, int depth = 0, string prefix = "")
    {
        var results = new List<LocationOption>();

        if (node is null)
            return results;

        string path = string.IsNullOrEmpty(prefix) ? node.Name : $"{prefix} / {node.Name}";

        results.Add(new LocationOption
        {
            Code = node.Code,
            Path = path,
            IsLeaf = node.IsLeaf,
            Depth = depth
        });

        // Recursive case: append everything each child contributes.
        foreach (LocationNode child in node.Children)
            results.AddRange(Flatten(child, depth + 1, depth == 0 ? string.Empty : path));

        return results;
    }

    // Co-authored by Claude
    public List<LocationOption> FlattenAll() => Flatten(Root);
}
