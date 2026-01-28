namespace DriveFlow_CRM_API.Models.DTOs;

// ═══════════════════════════════════════════════════════════════════════════════
//  AI CONTEXT DTOs - Request/Response for the AI chatbot context endpoint
// ═══════════════════════════════════════════════════════════════════════════════

#region Chat Request/Response DTOs

/// <summary>
/// Request DTO for the AI chat streaming endpoint.
/// </summary>
public sealed class ChatRequest
{
    /// <summary>
    /// Conversation messages (user and assistant history).
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Number of recent sessions to include per category for context (default: 5).
    /// </summary>
    public int? HistorySessions { get; set; } = 5;

    /// <summary>
    /// Language code for the system prompt ("ro" or "en", default: "ro").
    /// </summary>
    public string? Language { get; set; } = "ro";
}

/// <summary>
/// A single message in the chat conversation.
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    /// Message role: "user" or "assistant".
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Message content/text.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

#endregion

#region Internal Context DTOs

/// <summary>
/// Response DTO containing the system prompt and student context for the LLM.
/// </summary>
/// <param name="GeneratedAt">Timestamp when the context was generated</param>
/// <param name="SystemPrompt">The system role text for the LLM</param>
/// <param name="Context">The structured student context data</param>
public sealed record AiStudentContextResponse(
    DateTime GeneratedAt,
    string SystemPrompt,
    StudentContextDto Context
);

#endregion

#region Student Context Structure

/// <summary>
/// Complete student context for AI coaching. Contains all relevant data
/// about the student's driving progress without internal IDs.
/// </summary>
public sealed class StudentContextDto
{
    /// <summary>Student profile summary.</summary>
    public StudentSummaryDto Student { get; init; } = null!;

    /// <summary>Teaching categories the student is enrolled in.</summary>
    public List<CategoryProgressDto> Categories { get; init; } = new();

    /// <summary>Overall progress aggregated across all categories.</summary>
    public OverallProgressDto OverallProgress { get; init; } = null!;

    /// <summary>Most common mistakes across all sessions.</summary>
    public List<MistakeSummaryDto> CommonMistakes { get; init; } = new();

    /// <summary>Skills where the student performs well.</summary>
    public List<string> StrongSkills { get; init; } = new();

    /// <summary>Skills that need improvement.</summary>
    public List<string> SkillsNeedingImprovement { get; init; } = new();

    /// <summary>Highlights from the most recent sessions.</summary>
    public List<SessionHighlightDto> LatestSessionHighlights { get; init; } = new();

    /// <summary>Factual notes useful for AI coaching.</summary>
    public List<string> CoachingNotes { get; init; } = new();

    /// <summary>Data availability status (edge cases).</summary>
    public DataAvailabilityDto DataAvailability { get; init; } = null!;
}

/// <summary>
/// Student profile summary (no internal IDs).
/// </summary>
public sealed class StudentSummaryDto
{
    /// <summary>Student's full name.</summary>
    public string FullName { get; init; } = null!;

    /// <summary>Student's email (for reference).</summary>
    public string? Email { get; init; }

    /// <summary>Driving school name.</summary>
    public string? SchoolName { get; init; }

    /// <summary>Total number of active files/enrollments.</summary>
    public int TotalEnrollments { get; init; }

    /// <summary>Total completed driving sessions.</summary>
    public int TotalCompletedSessions { get; init; }

    /// <summary>Date of first session (if any).</summary>
    public DateOnly? FirstSessionDate { get; init; }

    /// <summary>Date of most recent session (if any).</summary>
    public DateOnly? LastSessionDate { get; init; }
}

/// <summary>
/// Progress data for a specific teaching category (e.g., category B).
/// </summary>
public sealed class CategoryProgressDto
{
    /// <summary>Teaching category code (e.g., "B", "C+E").</summary>
    public string CategoryCode { get; init; } = null!;

    /// <summary>License type if available.</summary>
    public string? LicenseType { get; init; }

    /// <summary>File status (e.g., "approved", "finalised").</summary>
    public string Status { get; init; } = null!;

    /// <summary>Assigned instructor name.</summary>
    public string? InstructorName { get; init; }

    /// <summary>Assigned vehicle info (brand/model).</summary>
    public string? VehicleInfo { get; init; }

    /// <summary>Transmission type of the vehicle.</summary>
    public string? TransmissionType { get; init; }

    /// <summary>Date when scholarship/training started.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Total appointments scheduled.</summary>
    public int TotalAppointments { get; init; }

    /// <summary>Completed appointments count.</summary>
    public int CompletedAppointments { get; init; }

    /// <summary>Sessions with evaluations.</summary>
    public int EvaluatedSessions { get; init; }

    /// <summary>Minimum required driving lessons for this category.</summary>
    public int MinRequiredLessons { get; init; }

    /// <summary>Session cost for this category.</summary>
    public decimal SessionCost { get; init; }

    /// <summary>Session duration in minutes.</summary>
    public int SessionDurationMinutes { get; init; }

    /// <summary>Recent session evaluations for this category.</summary>
    public List<SessionEvaluationDto> RecentSessions { get; init; } = new();

    /// <summary>Progress trend: "improving", "stable", "declining", or "insufficient_data".</summary>
    public string Trend { get; init; } = "insufficient_data";

    /// <summary>Average penalty points across evaluated sessions.</summary>
    public double? AveragePenaltyPoints { get; init; }

    /// <summary>Maximum points threshold for this category's exam.</summary>
    public int? MaxExamPoints { get; init; }

    /// <summary>Pass rate based on results.</summary>
    public double? PassRate { get; init; }

    /// <summary>Most frequent mistakes in this category.</summary>
    public List<MistakeSummaryDto> TopMistakes { get; init; } = new();
}

/// <summary>
/// Single session evaluation data.
/// </summary>
public sealed class SessionEvaluationDto
{
    /// <summary>Date of the session.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Total penalty points received.</summary>
    public int? TotalPoints { get; init; }

    /// <summary>Maximum points for the evaluation.</summary>
    public int MaxPoints { get; init; }

    /// <summary>Result: "OK" or "FAILED".</summary>
    public string? Result { get; init; }

    /// <summary>Mistakes made during this session.</summary>
    public List<MistakeDetailDto> Mistakes { get; init; } = new();
}

/// <summary>
/// Detailed mistake information from a session.
/// </summary>
public sealed class MistakeDetailDto
{
    /// <summary>Description of the mistake/infraction.</summary>
    public string Description { get; init; } = null!;

    /// <summary>Number of times this mistake occurred.</summary>
    public int Count { get; init; }

    /// <summary>Penalty points per occurrence.</summary>
    public int PenaltyPoints { get; init; }

    /// <summary>Total penalty for this mistake (count * penaltyPoints).</summary>
    public int TotalPenalty { get; init; }
}

/// <summary>
/// Aggregated mistake summary across sessions.
/// </summary>
public sealed class MistakeSummaryDto
{
    /// <summary>Mistake description.</summary>
    public string Description { get; init; } = null!;

    /// <summary>Total occurrences across all sessions.</summary>
    public int TotalOccurrences { get; init; }

    /// <summary>Number of sessions where this mistake appeared.</summary>
    public int SessionsAffected { get; init; }

    /// <summary>Severity level based on penalty points: "low", "medium", "high".</summary>
    public string Severity { get; init; } = "medium";
}

/// <summary>
/// Overall progress summary across all categories.
/// </summary>
public sealed class OverallProgressDto
{
    /// <summary>Total sessions across all categories.</summary>
    public int TotalSessions { get; init; }

    /// <summary>Total evaluated sessions.</summary>
    public int TotalEvaluatedSessions { get; init; }

    /// <summary>Overall pass rate percentage.</summary>
    public double? OverallPassRate { get; init; }

    /// <summary>Average penalty points across all sessions.</summary>
    public double? AveragePenaltyPoints { get; init; }

    /// <summary>Overall trend: "improving", "stable", "declining", "insufficient_data".</summary>
    public string OverallTrend { get; init; } = "insufficient_data";

    /// <summary>Number of categories showing improvement.</summary>
    public int CategoriesImproving { get; init; }

    /// <summary>Number of categories showing decline.</summary>
    public int CategoriesDeclining { get; init; }

    /// <summary>Total distinct mistake types made.</summary>
    public int TotalDistinctMistakes { get; init; }

    /// <summary>Most improved areas (reduced mistakes).</summary>
    public List<string> ImprovementAreas { get; init; } = new();
}

/// <summary>
/// Highlight from a recent session for quick context.
/// </summary>
public sealed class SessionHighlightDto
{
    /// <summary>Category code.</summary>
    public string CategoryCode { get; init; } = null!;

    /// <summary>Session date.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Brief summary of the session performance.</summary>
    public string Summary { get; init; } = null!;

    /// <summary>Was the session successful (passed)?</summary>
    public bool? Passed { get; init; }

    /// <summary>Penalty points received.</summary>
    public int? PenaltyPoints { get; init; }

    /// <summary>Maximum points for reference.</summary>
    public int MaxPoints { get; init; }

    /// <summary>Top mistake in this session (if any).</summary>
    public string? TopMistake { get; init; }
}

/// <summary>
/// Data availability status for edge cases.
/// </summary>
public sealed class DataAvailabilityDto
{
    /// <summary>Whether the student has any files/enrollments.</summary>
    public bool HasEnrollments { get; init; }

    /// <summary>Whether the student has any completed sessions.</summary>
    public bool HasCompletedSessions { get; init; }

    /// <summary>Whether there are any evaluated sessions with forms.</summary>
    public bool HasEvaluatedSessions { get; init; }

    /// <summary>Categories without any session data.</summary>
    public List<string> CategoriesWithoutSessions { get; init; } = new();

    /// <summary>Categories with incomplete evaluation data.</summary>
    public List<string> CategoriesWithIncompleteData { get; init; } = new();

    /// <summary>Warning messages about data limitations.</summary>
    public List<string> Warnings { get; init; } = new();
}

#endregion
