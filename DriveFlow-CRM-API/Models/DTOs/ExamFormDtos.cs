namespace DriveFlow_CRM_API.Models.DTOs;

/// <summary>Represents a single exam item in the response.</summary>
public sealed record ExamItemDto(
    int id_item,
    string description,
    int penaltyPoints,
    int orderIndex
);

/// <summary>Represents the complete exam form with all items.</summary>
public sealed record ExamFormDto(
    int id_formular,
    int id_categ,
    int maxPoints,
    IEnumerable<ExamItemDto> items
);

/// <summary>List item DTO for session form history.</summary>
public sealed record SessionFormListItemDto(
    int id,                 // SessionFormId
    DateOnly date,          // Appointment date
    int? totalPoints,       // Total penalty points (null if not finalized)
    int maxPoints,          // Maximum allowed points
    string? result          // "OK" or "FAILED" (null if not finalized)
);

/// <summary>Generic paged result wrapper.</summary>
public sealed record PagedResult<T>(
    int page,               // Current page number (1-based)
    int pageSize,           // Items per page
    int total,              // Total count of items
    IEnumerable<T> items    // Current page items
);
