using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models;

/// <summary>
/// Represents an official exam form for a license category (e.g., license "B" has a standardized form).
/// </summary>
/// <remarks>
///  One-to-one with <see cref="License"/> (one form per license type).<br/>
///  <see cref="LicenseId"/> is the FK and is unique (immutable).<br/>
///  <see cref="MaxPoints"/> is the maximum score for this exam (typically 21 for category B).<br/>
///  Deleting the <see cref="License"/> cascades and deletes this form and all its items.
/// </remarks>
public class ExamForm
{
    /// <summary>Primary key.</summary>
    [Key]
    public int FormId { get; set; }

    /// <summary>Foreign key to the license (unique, immutable).</summary>
    [ForeignKey(nameof(License))]
    public int LicenseId { get; set; }

    /// <summary>Maximum points achievable on this exam form.</summary>
    [Range(0, int.MaxValue)]
    public int MaxPoints { get; set; }

    /// <summary>Navigation property to the license.</summary>
    public virtual License License { get; set; } = null!;

    /// <summary>Collection of exam items (ordered by OrderIndex).</summary>
    public virtual ICollection<ExamItem> Items { get; set; } = new List<ExamItem>();
}
