namespace ColdChain.Shared.Models;

/// <summary>
/// A readable validation / error payload returned with non 2xx responses.
/// </summary>
public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public ApiError() { }

    public ApiError(string message, params string[] errors)
    {
        Message = message;
        Errors = errors.ToList();
    }

    public override string ToString() =>
        Errors.Count == 0 ? Message : $"{Message}{Environment.NewLine}- " + string.Join($"{Environment.NewLine}- ", Errors);
}
