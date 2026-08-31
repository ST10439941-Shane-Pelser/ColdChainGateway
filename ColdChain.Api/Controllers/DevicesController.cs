using ColdChain.Api.Services;
using ColdChain.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ColdChain.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly GatewayStore _store;
    private readonly DeviceValidator _validator;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(
        GatewayStore store,
        DeviceValidator validator,
        IWebHostEnvironment environment,
        ILogger<DevicesController> logger)
    {
        _store = store;
        _validator = validator;
        _environment = environment;
        _logger = logger;
    }

    // Co-authored by Claude
    /// <summary>GET /api/devices - every registered device, optionally searched and filtered.</summary>
    [HttpGet]
    public ActionResult<List<Device>> GetDevices(
        [FromQuery] string? search,
        [FromQuery] string? deviceType,
        [FromQuery] bool? isActive)
    {
        return Ok(_store.SearchDevices(search, deviceType, isActive));
    }

    // Co-authored by Claude
    /// <summary>GET /api/devices/{id} - a single device.</summary>
    [HttpGet("{id}")]
    public ActionResult<Device> GetDevice(string id)
    {
        Device? device = _store.FindDevice(id);

        if (device is null)
            return NotFound(new ApiError($"No device is registered with ID '{id}'."));

        return Ok(device);
    }

    // Co-authored by Claude
    /// <summary>
    /// POST /api/devices - registers a device after full validation, including the
    /// recursive location check.
    /// </summary>
    [HttpPost]
    public ActionResult<Device> RegisterDevice([FromBody] DeviceRegistrationRequest request)
    {
        List<string> errors = _validator.Validate(request, out string locationPath);

        if (errors.Count > 0)
            return BadRequest(new ApiError("Device registration failed validation.") { Errors = errors });

        var device = new Device
        {
            DeviceId = request.DeviceId.Trim().ToUpperInvariant(),
            DeviceName = request.DeviceName.Trim(),
            DeviceType = request.DeviceType.Trim(),
            LocationCode = request.LocationCode.Trim().ToUpperInvariant(),
            LocationPath = locationPath,
            IsActive = request.IsActive,
            RegisteredUtc = DateTime.UtcNow
        };

        _store.AddDevice(device);
        _logger.LogInformation("Registered device {DeviceId} at {LocationCode}.", device.DeviceId, device.LocationCode);

        return CreatedAtAction(nameof(GetDevice), new { id = device.DeviceId }, device);
    }

    // Co-authored by Claude
    /// <summary>
    /// POST /api/devices/{id}/attachments - accepts an evidence file as
    /// multipart/form-data, writes the bytes to disk and keeps the metadata.
    /// </summary>
    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(AttachmentRules.MaxBytes + 4096)]
    public async Task<ActionResult<AttachmentMetadata>> UploadAttachment(
        string id,
        [FromForm] IFormFile? file,
        [FromForm] string? description)
    {
        Device? device = _store.FindDevice(id);

        if (device is null)
            return NotFound(new ApiError($"No device is registered with ID '{id}'."));

        List<string> errors = AttachmentRules.Validate(file);

        if (errors.Count > 0)
            return BadRequest(new ApiError("Evidence upload rejected.") { Errors = errors });

        string uploadFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadFolder);

        string extension = Path.GetExtension(file!.FileName);
        string storedName = $"{device.DeviceId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
        string fullPath = Path.Combine(uploadFolder, storedName);

        try
        {
            await using FileStream stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to write evidence file for {DeviceId}.", device.DeviceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiError("The evidence file could not be stored on the gateway."));
        }

        var metadata = _store.AddAttachment(new AttachmentMetadata
        {
            DeviceId = device.DeviceId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedName,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            UploadedUtc = DateTime.UtcNow,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        });

        return Ok(metadata);
    }

    // Co-authored by Claude
    /// <summary>GET /api/devices/{id}/attachments - evidence metadata for a device.</summary>
    [HttpGet("{id}/attachments")]
    public ActionResult<List<AttachmentMetadata>> GetAttachments(string id)
    {
        if (_store.FindDevice(id) is null)
            return NotFound(new ApiError($"No device is registered with ID '{id}'."));

        return Ok(_store.GetAttachments(id));
    }
}
