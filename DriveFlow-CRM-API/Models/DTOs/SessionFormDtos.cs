namespace DriveFlow_CRM_API.Models.DTOs;

/// <summary>DTO for session form response.</summary>
public sealed record SessionFormDto(
    int id,
    int id_app,
    int id_formular,
    bool isLocked,
    DateTime createdAt,
    DateTime? finalizedAt,
    int? totalPoints,
    string? result,
    string mistakesJson
);

/// <summary>DTO for updating mistake count on a session form.</summary>
public sealed record UpdateMistakeRequest(
    int id_item,
    int delta
);

/// <summary>DTO for update mistake response showing the new count.</summary>
public sealed record UpdateMistakeResponse(
    int id_item,
    int count
);

/// <summary>DTO for finalize response showing total points and result.</summary>
public sealed record FinalizeResponse(
    int id,
    int totalPoints,
    int maxPoints,
    string result
);

/// <summary>DTO for mistake breakdown with item details.</summary>
public sealed record MistakeBreakdownDto(
    int id_item,
    string description,
    int count,
    int penaltyPoints
);

/// <summary>DTO for viewing a complete session form with all details.</summary>
public sealed record SessionFormViewDto(
    int id,
    DateOnly appointmentDate,
    string studentName,
    string instructorName,
    int? totalPoints,
    int maxPoints,
    string? result,
    bool isLocked,
    IEnumerable<MistakeBreakdownDto> mistakes
);
