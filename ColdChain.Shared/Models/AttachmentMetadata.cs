namespace ColdChain.Shared.Models;

/// <summary>
/// Metadata for an evidence file (inspection photo or PDF report) attached to a device.
/// The bytes live on disk; only this record is held in memory.
/// </summary>
public class AttachmentMetadata
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedUtc { get; set; }
    public string? Description { get; set; }
}
