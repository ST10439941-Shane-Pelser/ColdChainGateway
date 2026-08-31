namespace ColdChain.Shared.Models;

/// <summary>
/// A flattened location entry, used to fill the location dropdown in the frontend.
/// </summary>
public class LocationOption
{
    public string Code { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsLeaf { get; set; }
    public int Depth { get; set; }

    public override string ToString() => $"{Code} - {Path}";
}
