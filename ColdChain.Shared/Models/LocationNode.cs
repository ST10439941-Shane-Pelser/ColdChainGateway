namespace ColdChain.Shared.Models;

/// <summary>
/// One node in the monitoring location hierarchy:
/// Network -> Depot -> Cold Room / Vehicle Bay -> Shelf / Vehicle.
/// Devices may only be registered against a leaf node.
/// </summary>
public class LocationNode
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<LocationNode> Children { get; set; } = new();

    public bool IsLeaf => Children.Count == 0;

    public LocationNode() { }

    public LocationNode(string code, string name, params LocationNode[] children)
    {
        Code = code;
        Name = name;
        Children = children.ToList();
    }

    public override string ToString() => $"{Code} - {Name}";
}
