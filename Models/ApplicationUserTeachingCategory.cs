using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;  

namespace DriveFlow_CRM_API.Models;                  

// ─────────────────────── ApplicationUserTeachingCategory entity ───────────────────────

/// <summary>
/// Join-entity that links an <see cref="ApplicationUser"/> (student or instructor)
/// to a <see cref="TeachingCategory"/>.
/// </summary>
/// <remarks>
/// • Deleting the user (instructor / student) <b>cascades</b> to this row.<br/>
/// • Deleting the teaching category cascades as well.<br/>
/// • No additional columns, just the two foreign keys.
/// </remarks>
public class ApplicationUserTeachingCategory
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Surrogate primary key.</summary>
    [Key]
    public int ApplicationUserTeachingCategoryId { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>FK to the linked <see cref="ApplicationUser"/> (required).</summary>
    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }

    /// <summary>Navigation to the user (student / instructor).</summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>FK to the linked <see cref="TeachingCategory"/> (required).</summary>
    [ForeignKey(nameof(TeachingCategory))]
    public int TeachingCategoryId { get; set; }

    /// <summary>Navigation to the teaching category.</summary>
    public virtual TeachingCategory TeachingCategory { get; set; } = null!;
}
