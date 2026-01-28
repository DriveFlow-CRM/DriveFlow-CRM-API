using DriveFlow_CRM_API.Models.DTOs;
using DriveFlow_CRM_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// AI chat endpoints for the DriveFlow CRM API.
/// Provides a secure proxy to OpenRouter API with student context injection.
/// </summary>
/// <remarks>
/// This controller:
/// - Builds student context server-side from the database
/// - Calls OpenRouter API with the context and user messages
/// - Streams responses back via Server-Sent Events (SSE)
/// - Keeps the API key secure on the backend (never exposed to frontend)
/// </remarks>
[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiContextBuilder _contextBuilder;
    private readonly IAiStreamingService _streamingService;

    /// <summary>
    /// Constructor injected by the framework with request-scoped services.
    /// </summary>
    public AiController(
        IAiContextBuilder contextBuilder,
        IAiStreamingService streamingService)
    {
        _contextBuilder = contextBuilder;
        _streamingService = streamingService;
    }

    /// <summary>
    /// Streams AI chat responses for the authenticated student via Server-Sent Events (SSE).
    /// </summary>
    /// <remarks>
    /// This endpoint:
    /// 1. Builds student context from the database (progress, mistakes, coaching notes)
    /// 2. Prepends context as system messages to the conversation
    /// 3. Calls OpenRouter API with streaming enabled
    /// 4. Streams the response back to the client via SSE
    /// 
    /// <para><strong>Authorization:</strong></para>
    /// - Only students can access this endpoint
    /// - Student context is built server-side using the authenticated user's ID
    /// 
    /// <para><strong>Request body:</strong></para>
    /// <code>
    /// {
    ///   "messages": [
    ///     { "role": "user", "content": "Cum mă pot pregăti mai bine?" },
    ///     { "role": "assistant", "content": "Bună! Văd că ai..." },
    ///     { "role": "user", "content": "Ce greșeli fac cel mai des?" }
    ///   ],
    ///   "historySessions": 5,    // Optional, default 5 (sessions per category for context)
    ///   "language": "ro"         // Optional, default "ro" (system prompt language)
    /// }
    /// </code>
    /// 
    /// <para><strong>SSE Response format:</strong></para>
    /// <code>
    /// event: chunk
    /// data: Bună
    /// 
    /// event: chunk
    /// data: , Ion!
    /// 
    /// event: chunk
    /// data:  Văd că ai
    /// 
    /// event: done
    /// data:
    /// </code>
    /// 
    /// <para><strong>Error event:</strong></para>
    /// <code>
    /// event: error
    /// data: {"message": "Failed to connect to AI service"}
    /// </code>
    /// 
    /// <para><strong>Conversation flow:</strong></para>
    /// - Frontend sends full conversation history with each request
    /// - Backend prepends fresh context (from database) on every request
    /// - Context is always up-to-date with student's latest progress
    /// </remarks>
    /// <param name="request">Chat request with conversation messages</param>
    /// <response code="200">SSE stream started successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not a student</response>
    /// <response code="404">Student not found in database</response>
    [HttpPost("chat/stream")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task StreamChat([FromBody] ChatRequest request)
    {
        // 1. Get authenticated user's ID from JWT claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { message = "User not authenticated" });
            return;
        }

        // 2. Verify the user has Student role (additional check beyond [Authorize])
        if (!User.IsInRole("Student"))
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            await Response.WriteAsJsonAsync(new { message = "Access denied" });
            return;
        }

        // 3. Build student context from database
        var historySessions = request.HistorySessions ?? 5;
        var language = request.Language ?? "ro";

        var context = await _contextBuilder.BuildStudentContextAsync(
            userId,
            historySessions,
            language,
            HttpContext.RequestAborted);

        if (context == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { message = "Student not found" });
            return;
        }

        // 4. Prepare messages for OpenRouter
        //    - Message 1 (system): The system prompt with AI behavior instructions
        //    - Message 2 (system): The student context as JSON
        //    - Messages 3+: The conversation history from the request
        var messages = new List<object>
        {
            new { role = "system", content = context.SystemPrompt },
            new { role = "system", content = JsonSerializer.Serialize(context.Context) }
        };

        // Add conversation history from request
        if (request.Messages != null)
        {
            messages.AddRange(request.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }));
        }

        // 5. Set SSE headers
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering

        // 6. Stream response from OpenRouter to client
        await _streamingService.StreamToClientAsync(
            messages,
            Response,
            HttpContext.RequestAborted);
    }

    /// <summary>
    /// Health check endpoint for the AI chat service.
    /// </summary>
    /// <remarks>
    /// Simple endpoint to verify the AI chat service is available.
    /// Does not require authentication.
    /// </remarks>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> HealthCheck()
    {
        // Check if OpenRouter is configured
        var apiKeyConfigured = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));

        return Ok(new
        {
            status = "healthy",
            service = "ai-chat",
            timestamp = DateTime.UtcNow,
            version = "2.0.0",
            openRouterConfigured = apiKeyConfigured
        });
    }
}
