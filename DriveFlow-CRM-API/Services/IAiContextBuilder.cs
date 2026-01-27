using DriveFlow_CRM_API.Models.DTOs;

namespace DriveFlow_CRM_API.Services;

/// <summary>
/// Service interface for building AI chatbot context for students.
/// The context contains structured data about the student's driving progress,
/// designed to be consumed by an LLM on the frontend.
/// </summary>
public interface IAiContextBuilder
{
    /// <summary>
    /// Builds a complete AI context for a student including system prompt and structured data.
    /// </summary>
    /// <param name="studentId">The student's user ID.</param>
    /// <param name="historySessions">Number of recent sessions to include per file.</param>
    /// <param name="language">Language code for the system prompt (e.g., "ro", "en").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete AI context response or null if student not found.</returns>
    Task<AiStudentContextResponse?> BuildStudentContextAsync(
        string studentId,
        int historySessions = 5,
        string language = "ro",
        CancellationToken cancellationToken = default);
}
