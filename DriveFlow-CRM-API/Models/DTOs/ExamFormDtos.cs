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
