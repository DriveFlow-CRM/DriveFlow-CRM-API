using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DriveFlow_CRM_API.Services;

/// <summary>
/// Implements AI chat streaming via OpenRouter API with Server-Sent Events (SSE).
/// Reads configuration from environment variables and streams responses to clients.
/// </summary>
public sealed class AiStreamingService : IAiStreamingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiStreamingService> _logger;

    // Environment variable keys
    private const string ApiKeyEnvVar = "OPENROUTER_API_KEY";
    private const string ModelEnvVar = "OPENROUTER_MODEL";
    private const string BaseUrlEnvVar = "OPENROUTER_BASE_URL";

    // Default values
    private const string DefaultModel = "deepseek/deepseek-r1-0528:free";
    private const string DefaultBaseUrl = "https://openrouter.ai/api/v1";

    public AiStreamingService(
        IHttpClientFactory httpClientFactory,
        ILogger<AiStreamingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task StreamToClientAsync(
        List<object> messages,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        // Read configuration from environment
        var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
        var model = Environment.GetEnvironmentVariable(ModelEnvVar) ?? DefaultModel;
        var baseUrl = Environment.GetEnvironmentVariable(BaseUrlEnvVar) ?? DefaultBaseUrl;

        // Validate API key
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("OpenRouter API key not configured. Set {EnvVar} environment variable.", ApiKeyEnvVar);
            await WriteErrorEventAsync(response, "AI service not configured", cancellationToken);
            return;
        }

        // Clean up model and baseUrl (remove quotes if present from env file)
        model = model.Trim('"');
        baseUrl = baseUrl.Trim('"');

        // Build the endpoint URL
        var endpoint = $"{baseUrl.TrimEnd('/')}/chat/completions";

        try
        {
            // Create HTTP client
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim('"'));
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://driveflow.ro");
            client.DefaultRequestHeaders.Add("X-Title", "DriveFlow CRM");

            // Build request payload
            var requestBody = new
            {
                model = model,
                stream = true,
                messages = messages
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Make streaming request
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };

            using var httpResponse = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenRouter API error: {StatusCode} - {Body}", httpResponse.StatusCode, errorBody);
                await WriteErrorEventAsync(response, $"AI service error: {httpResponse.StatusCode}", cancellationToken);
                return;
            }

            // Stream the response
            using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);

                if (string.IsNullOrEmpty(line))
                    continue;

                // OpenRouter sends lines prefixed with "data: "
                if (!line.StartsWith("data: "))
                    continue;

                var data = line.Substring(6); // Remove "data: " prefix

                // Check for stream end
                if (data == "[DONE]")
                {
                    await WriteDoneEventAsync(response, cancellationToken);
                    break;
                }

                // Parse the JSON chunk
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;

                    // Extract content from choices[0].delta.content
                    if (root.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var contentElement))
                        {
                            var text = contentElement.GetString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                await WriteChunkEventAsync(response, text, cancellationToken);
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse SSE chunk: {Data}", data);
                    // Continue processing other chunks
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Client disconnected, streaming cancelled");
            // Client disconnected - this is normal
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to OpenRouter API");
            await WriteErrorEventAsync(response, "Failed to connect to AI service", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during AI streaming");
            await WriteErrorEventAsync(response, "An unexpected error occurred", cancellationToken);
        }
    }

    /// <summary>
    /// Writes a chunk SSE event to the response.
    /// </summary>
    private static async Task WriteChunkEventAsync(
        HttpResponse response,
        string text,
        CancellationToken cancellationToken)
    {
        var eventData = $"event: chunk\ndata: {EscapeSseData(text)}\n\n";
        await response.WriteAsync(eventData, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Writes a done SSE event to the response.
    /// </summary>
    private static async Task WriteDoneEventAsync(
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync("event: done\ndata:\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Writes an error SSE event to the response.
    /// </summary>
    private static async Task WriteErrorEventAsync(
        HttpResponse response,
        string message,
        CancellationToken cancellationToken)
    {
        var errorJson = JsonSerializer.Serialize(new { message });
        await response.WriteAsync($"event: error\ndata: {errorJson}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Escapes newlines in SSE data (each line needs a "data: " prefix).
    /// </summary>
    private static string EscapeSseData(string text)
    {
        // SSE spec: multiline data needs each line prefixed with "data: "
        // For simplicity, we'll escape newlines as they're rare in chat chunks
        return text.Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
