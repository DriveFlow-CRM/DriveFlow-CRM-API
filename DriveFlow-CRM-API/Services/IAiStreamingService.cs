namespace DriveFlow_CRM_API.Services;

/// <summary>
/// Service interface for streaming AI chat responses via Server-Sent Events (SSE).
/// Handles communication with the OpenRouter API and streams responses back to clients.
/// </summary>
public interface IAiStreamingService
{
    /// <summary>
    /// Streams AI chat responses to the client via SSE.
    /// </summary>
    /// <param name="messages">
    /// List of messages to send to OpenRouter. Should include:
    /// - System message(s) with prompt and context
    /// - User/assistant conversation history
    /// </param>
    /// <param name="response">The HTTP response to stream SSE events to.</param>
    /// <param name="cancellationToken">Cancellation token for client disconnect handling.</param>
    /// <returns>Task that completes when streaming is finished or cancelled.</returns>
    Task StreamToClientAsync(
        List<object> messages,
        HttpResponse response,
        CancellationToken cancellationToken);
}
