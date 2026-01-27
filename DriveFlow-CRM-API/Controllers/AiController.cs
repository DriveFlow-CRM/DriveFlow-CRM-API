using DriveFlow_CRM_API.Models.DTOs;
using DriveFlow_CRM_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// AI context endpoints for the DriveFlow CRM API.
/// Provides structured context data for frontend AI chatbot integration.
/// </summary>
/// <remarks>
/// This controller does NOT call any AI/LLM services.
/// It only builds context data that the frontend will use with its own LLM calls.
/// </remarks>
[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiContextBuilder _contextBuilder;

    /// <summary>
    /// Constructor injected by the framework with request-scoped services.
    /// </summary>
    public AiController(IAiContextBuilder contextBuilder)
    {
        _contextBuilder = contextBuilder;
    }

    /// <summary>
    /// Builds and returns the AI chatbot context for the authenticated student.
    /// </summary>
    /// <remarks>
    /// Returns a system prompt and structured context object for use with an LLM.
    /// The frontend is responsible for calling the actual AI service.
    /// 
    /// <para><strong>Authorization:</strong></para>
    /// - Only students can access their own context
    /// - Instructors and admins are NOT allowed (use other endpoints for instructor insights)
    /// 
    /// <para><strong>Request body:</strong></para>
    /// <code>
    /// {
    ///   "historySessions": 5,    // Number of recent sessions to include per category (1-50)
    ///   "language": "ro"         // Language for system prompt ("ro" or "en")
    /// }
    /// </code>
    /// 
    /// <para><strong>Response format:</strong></para>
    /// <code>
    /// {
    ///   "generatedAt": "2026-01-27T10:30:00Z",
    ///   "systemPrompt": "Ești un asistent virtual...",
    ///   "context": {
    ///     "student": { "fullName": "Ion Popescu", ... },
    ///     "categories": [ { "categoryCode": "B", ... } ],
    ///     "overallProgress": { ... },
    ///     "commonMistakes": [ { "description": "...", ... } ],
    ///     "strongSkills": [ "..." ],
    ///     "skillsNeedingImprovement": [ "..." ],
    ///     "latestSessionHighlights": [ { ... } ],
    ///     "coachingNotes": [ "..." ],
    ///     "dataAvailability": { ... }
    ///   }
    /// }
    /// </code>
    /// 
    /// <para><strong>Edge cases handled:</strong></para>
    /// - Student with zero sessions: Returns empty arrays with appropriate warnings
    /// - Student with multiple categories: Each category has separate progress tracking
    /// - Missing evaluation data: Clearly indicated in dataAvailability section
    /// </remarks>
    /// <param name="request">Optional parameters for context building</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Context built successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not a student or trying to access another user's context</response>
    /// <response code="404">Student not found</response>
    [HttpPost("context/student")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(AiStudentContextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiStudentContextResponse>> GetStudentContext(
        [FromBody] AiStudentContextRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // 2. Verify the user has Student role (additional check beyond [Authorize])
        if (!User.IsInRole("Student"))
        {
            return Forbid();
        }

        // 3. Use defaults if request is null
        var historySessions = request?.HistorySessions ?? 5;
        var language = request?.Language ?? "ro";

        // 4. Build the context
        var context = await _contextBuilder.BuildStudentContextAsync(
            userId,
            historySessions,
            language,
            cancellationToken);

        if (context == null)
        {
            return NotFound(new { message = "Student not found" });
        }

        return Ok(context);
    }

    /// <summary>
    /// Health check endpoint for the AI context service.
    /// </summary>
    /// <remarks>
    /// Simple endpoint to verify the AI context service is available.
    /// Does not require authentication.
    /// </remarks>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "ai-context",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
