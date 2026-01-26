namespace DriveFlow_CRM_API.Models.DTOs;

/// <summary>DTO for a single mistake item in the submit request.</summary>
public sealed record MistakeItemDto(
    int IdItem,
    int Count
);

/// <summary>DTO for submitting a completed session form.</summary>
public sealed record SubmitSessionFormRequest(
    List<MistakeItemDto>? Mistakes,
    int MaxPoints
);

/// <summary>DTO for submit session form response showing total points and result.</summary>
public sealed record SubmitSessionFormResponse(
    int Id,
    int TotalPoints,
    int MaxPoints,
    string Result
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
    IEnumerable<MistakeBreakdownDto> mistakes
);
