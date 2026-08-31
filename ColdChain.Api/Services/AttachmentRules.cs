namespace ColdChain.Api.Services;

/// <summary>
/// What the gateway accepts as evidence: inspection photos and PDF reports only,
/// never an empty file, never anything oversized.
/// </summary>
public static class AttachmentRules
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };

    public static readonly string[] AllowedContentTypes =
    {
        "image/jpeg", "image/jpg", "image/png", "application/pdf"
    };

    // Co-authored by Claude
    /// <summary>Checks size, extension and reported content type. Returns every failure found.</summary>
    public static List<string> Validate(IFormFile? file)
    {
        var errors = new List<string>();

        if (file is null || file.Length == 0)
        {
            errors.Add("An evidence file is required and may not be empty.");
            return errors;
        }

        if (file.Length > MaxBytes)
            errors.Add($"File is {file.Length / 1024d / 1024d:0.00} MB. The limit is {MaxBytes / 1024 / 1024} MB.");

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
            errors.Add($"File type '{extension}' is not allowed. Accepted types: {string.Join(", ", AllowedExtensions)}.");

        if (!AllowedContentTypes.Contains(file.ContentType?.ToLowerInvariant()))
            errors.Add($"Content type '{file.ContentType}' is not allowed.");

        return errors;
    }
}
