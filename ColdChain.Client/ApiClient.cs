using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ColdChain.Shared.Models;

namespace ColdChain.Client;

/// <summary>
/// The only place in the frontend that talks to the gateway. Every screen goes
/// through these methods, so the UI never touches the API's collections directly.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string BaseAddress => _http.BaseAddress?.ToString() ?? string.Empty;

    public ApiClient(string baseAddress)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    // ---------------------------------------------------------------- devices

    // Co-authored by Claude
    public async Task<List<Device>> GetDevicesAsync(string? search = null, string? deviceType = null)
    {
        string url = "api/devices" + BuildQuery(
            ("search", search),
            ("deviceType", deviceType));

        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, url);
        return await ReadAsync<List<Device>>(response);
    }

    // Co-authored by Claude
    public async Task<Device> RegisterDeviceAsync(DeviceRegistrationRequest request)
    {
        using HttpResponseMessage response = await SendJsonAsync(HttpMethod.Post, "api/devices", request);
        return await ReadAsync<Device>(response);
    }

    // Co-authored by Claude
    public async Task<List<LocationOption>> GetLocationOptionsAsync()
    {
        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, "api/locations/options?leavesOnly=true");
        return await ReadAsync<List<LocationOption>>(response);
    }

    // Co-authored by Claude
    /// <summary>Uploads an evidence file using multipart/form-data.</summary>
    public async Task<AttachmentMetadata> UploadEvidenceAsync(string deviceId, string filePath, string? description)
    {
        if (!File.Exists(filePath))
            throw new ApiException($"The file '{filePath}' no longer exists on disk.");

        using var content = new MultipartFormDataContent();
        await using FileStream fileStream = File.OpenRead(filePath);

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GuessContentType(filePath));

        content.Add(fileContent, "file", Path.GetFileName(filePath));

        if (!string.IsNullOrWhiteSpace(description))
            content.Add(new StringContent(description), "description");

        try
        {
            using HttpResponseMessage response =
                await _http.PostAsync($"api/devices/{Uri.EscapeDataString(deviceId)}/attachments", content);

            return await ReadAsync<AttachmentMetadata>(response);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"Could not reach the gateway at {BaseAddress}. Is the API running? ({ex.Message})");
        }
    }

    // Co-authored by Claude
    public async Task<List<AttachmentMetadata>> GetAttachmentsAsync(string deviceId)
    {
        using HttpResponseMessage response =
            await SendAsync(HttpMethod.Get, $"api/devices/{Uri.EscapeDataString(deviceId)}/attachments");

        return await ReadAsync<List<AttachmentMetadata>>(response);
    }

    // ---------------------------------------------------------------- telemetry

    // Co-authored by Claude
    public async Task<List<TelemetryDto>> GetTelemetryAsync(string? deviceId, bool onlyAnomalies, int take = 150)
    {
        string url = "api/telemetry" + BuildQuery(
            ("deviceId", deviceId),
            ("onlyAnomalies", onlyAnomalies ? "true" : null),
            ("take", take.ToString()));

        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, url);
        return await ReadAsync<List<TelemetryDto>>(response);
    }

    // Co-authored by Claude
    public async Task<ZoneTemperatureSummary> GetZoneAverageAsync(int zoneIndex)
    {
        using HttpResponseMessage response =
            await SendAsync(HttpMethod.Get, $"api/telemetry/zones/{zoneIndex}/average-temperature");

        return await ReadAsync<ZoneTemperatureSummary>(response);
    }

    // Co-authored by Claude
    /// <summary>Reads the jagged monitoring-zone array from the gateway.</summary>
    public async Task<List<ZoneInfo>> GetZonesAsync()
    {
        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, "api/telemetry/zones");
        return await ReadAsync<List<ZoneInfo>>(response);
    }

    // ---------------------------------------------------------------- anomalies

    // Co-authored by Claude
    public async Task<List<AnomalyRecord>> GetAnomaliesAsync(bool? acknowledged)
    {
        string url = "api/anomalies" + BuildQuery(
            ("acknowledged", acknowledged?.ToString().ToLowerInvariant()),
            ("take", "150"));

        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, url);
        return await ReadAsync<List<AnomalyRecord>>(response);
    }

    // Co-authored by Claude
    public async Task<AnomalyRecord> AcknowledgeAsync(int anomalyId, AcknowledgeRequest request)
    {
        using HttpResponseMessage response =
            await SendJsonAsync(HttpMethod.Post, $"api/anomalies/{anomalyId}/acknowledge", request);

        return await ReadAsync<AnomalyRecord>(response);
    }

    // ---------------------------------------------------------------- plumbing

    // Co-authored by Claude
    /// <summary>Sends a request and turns a dead connection into a readable message.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            return await _http.SendAsync(request);
        }
        catch (TaskCanceledException)
        {
            throw new ApiException($"The gateway at {BaseAddress} did not respond in time.");
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"Could not reach the gateway at {BaseAddress}. Is the API running? ({ex.Message})");
        }
    }

    // Co-authored by Claude
    private async Task<HttpResponseMessage> SendJsonAsync<TBody>(HttpMethod method, string url, TBody body)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(body, options: JsonOptions)
            };

            return await _http.SendAsync(request);
        }
        catch (TaskCanceledException)
        {
            throw new ApiException($"The gateway at {BaseAddress} did not respond in time.");
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"Could not reach the gateway at {BaseAddress}. Is the API running? ({ex.Message})");
        }
    }

    // Co-authored by Claude
    /// <summary>
    /// Deserialises a success response, or turns a 4xx/5xx into an ApiException
    /// carrying the gateway's validation messages.
    /// </summary>
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ApiError? error = null;

            try
            {
                error = JsonSerializer.Deserialize<ApiError>(body, JsonOptions);
            }
            catch (JsonException)
            {
                // The body was not our error shape, fall through to the raw text.
            }

            string message = error is not null && !string.IsNullOrWhiteSpace(error.Message)
                ? error.ToString()
                : $"The gateway returned {(int)response.StatusCode} {response.ReasonPhrase}.";

            throw new ApiException(message, (int)response.StatusCode, error);
        }

        try
        {
            T? result = JsonSerializer.Deserialize<T>(body, JsonOptions);

            if (result is null)
                throw new ApiException("The gateway returned an empty response.");

            return result;
        }
        catch (JsonException ex)
        {
            throw new ApiException($"The gateway response could not be read: {ex.Message}");
        }
    }

    // Co-authored by Claude
    private static string BuildQuery(params (string Key, string? Value)[] parameters)
    {
        List<string> parts = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}")
            .ToList();

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    // Co-authored by Claude
    private static string GuessContentType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
}
