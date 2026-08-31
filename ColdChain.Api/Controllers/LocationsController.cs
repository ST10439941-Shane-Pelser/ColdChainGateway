using ColdChain.Api.Services;
using ColdChain.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ColdChain.Api.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly LocationTreeService _locations;

    public LocationsController(LocationTreeService locations)
    {
        _locations = locations;
    }

    // Co-authored by Claude
    /// <summary>GET /api/locations - the full hierarchy as a nested tree.</summary>
    [HttpGet]
    public ActionResult<LocationNode> GetTree() => Ok(_locations.Root);

    // Co-authored by Claude
    /// <summary>
    /// GET /api/locations/options - the tree flattened recursively, so the frontend
    /// can fill its location dropdown.
    /// </summary>
    [HttpGet("options")]
    public ActionResult<List<LocationOption>> GetOptions([FromQuery] bool leavesOnly = true)
    {
        List<LocationOption> all = _locations.FlattenAll();

        return Ok(leavesOnly ? all.Where(o => o.IsLeaf).ToList() : all);
    }

    // Co-authored by Claude
    /// <summary>
    /// GET /api/locations/validate/{code} - runs the recursive validation on its own,
    /// which is handy for demonstrating the method during the walkthrough.
    /// </summary>
    [HttpGet("validate/{code}")]
    public ActionResult ValidateCode(string code)
    {
        bool valid = _locations.IsValidDeviceLocation(code, out string reason, out string path);

        return Ok(new
        {
            Code = code,
            IsValid = valid,
            Reason = valid ? "Location found in the hierarchy and is a valid device position." : reason,
            Path = path
        });
    }
}
