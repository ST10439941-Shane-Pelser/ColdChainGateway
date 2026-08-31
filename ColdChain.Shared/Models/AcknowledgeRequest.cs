namespace ColdChain.Shared.Models;

/// <summary>
/// Payload for POST /api/anomalies/{id}/acknowledge.
/// </summary>
public class AcknowledgeRequest
{
    public string OperatorName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}
