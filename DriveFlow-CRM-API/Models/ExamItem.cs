using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models;

/// <summary>
/// Represents a single item/penalizable action on an exam form (e.g., "Failed to signal lane change" = 3 penalty points).
/// </summary>
/// <remarks>
/// • Multiple items per <see cref="ExamForm"/> (M:1 relationship).<br/>
/// • <see cref="OrderIndex"/> determines display order (immutable).<br/>
/// • Unique composite key: (<see cref="FormId"/>, <see cref="Description"/>) ensures
///   no duplicate descriptions per form.<br/>
/// • <see cref="PenaltyPoints"/> is the deduction for this infraction.
/// </remarks>
public class ExamItem
{
    /// <summary>Primary key.</summary>
    [Key]
    public int ItemId { get; set; }

    /// <summary>Foreign key to the parent exam form.</summary>
    [ForeignKey(nameof(ExamForm))]
    public int FormId { get; set; }

    /// <summary>Description of the penalizable action (e.g., "Failure to signal at lane change").</summary>
    [Required, StringLength(500)]
    public string Description { get; set; } = null!;

    /// <summary>Penalty points deducted for this infraction.</summary>
    [Range(0, int.MaxValue)]
    public int PenaltyPoints { get; set; }

    /// <summary>Display order within the form (1-based, immutable).</summary>
    [Range(1, int.MaxValue)]
    public int OrderIndex { get; set; }

    /// <summary>Navigation property to the parent exam form.</summary>
    public virtual ExamForm ExamForm { get; set; } = null!;
}
