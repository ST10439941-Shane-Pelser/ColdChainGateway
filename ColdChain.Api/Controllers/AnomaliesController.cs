using ColdChain.Api.Services;
using ColdChain.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ColdChain.Api.Controllers;

[ApiController]
[Route("api/anomalies")]
public class AnomaliesController : ControllerBase
{
    private readonly GatewayStore _store;
    private readonly ILogger<AnomaliesController> _logger;

    public AnomaliesController(GatewayStore store, ILogger<AnomaliesController> logger)
    {
        _store = store;
        _logger = logger;
    }

    // Co-authored by Claude
    /// <summary>GET /api/anomalies - abnormal events, newest first.</summary>
    [HttpGet]
    public ActionResult<List<AnomalyRecord>> GetAnomalies(
        [FromQuery] bool? acknowledged,
        [FromQuery] string? deviceId,
        [FromQuery] int take = 100)
    {
        if (take is < 1 or > 500)
            return BadRequest(new ApiError("The 'take' parameter must be between 1 and 500."));

        return Ok(_store.GetAnomalies(acknowledged, deviceId, take));
    }

    // Co-authored by Claude
    /// <summary>
    /// POST /api/anomalies/{id}/acknowledge - an operator signs off an abnormal
    /// event and records what they did about it.
    /// </summary>
    [HttpPost("{id:int}/acknowledge")]
    public ActionResult<AnomalyRecord> Acknowledge(int id, [FromBody] AcknowledgeRequest request)
    {
        AnomalyRecord? anomaly = _store.FindAnomaly(id);

        if (anomaly is null)
            return NotFound(new ApiError($"No anomaly exists with ID {id}."));

        var errors = new List<string>();

        if (request is null || string.IsNullOrWhiteSpace(request.OperatorName))
            errors.Add("Operator name is required.");

        if (request is null || string.IsNullOrWhiteSpace(request.Note))
            errors.Add("An operator note is required so the action taken is recorded.");
        else if (request.Note.Trim().Length < 5)
            errors.Add("The operator note must be at least 5 characters.");

        if (errors.Count > 0)
            return BadRequest(new ApiError("Acknowledgement rejected.") { Errors = errors });

        if (anomaly.IsAcknowledged)
            return Conflict(new ApiError(
                $"Anomaly {id} was already acknowledged by {anomaly.AcknowledgedBy} at {anomaly.AcknowledgedUtc:yyyy-MM-dd HH:mm} UTC."));

        anomaly.IsAcknowledged = true;
        anomaly.AcknowledgedBy = request!.OperatorName.Trim();
        anomaly.OperatorNote = request.Note.Trim();
        anomaly.AcknowledgedUtc = DateTime.UtcNow;

        _logger.LogInformation("Anomaly {Id} acknowledged by {Operator}.", id, anomaly.AcknowledgedBy);

        return Ok(anomaly);
    }
}
