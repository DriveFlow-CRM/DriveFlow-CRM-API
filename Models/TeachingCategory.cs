using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;  
using Microsoft.EntityFrameworkCore;                 

namespace DriveFlow_CRM_API.Models;                 

// ─────────────────────── TeachingCategory entity ───────────────────────

/// <summary>
/// A teaching category offered by an <see cref="AutoSchool"/> (e.g. “B Manual”).
/// </summary>
/// <remarks>
/// • FK <see cref="AutoSchoolId"/> is **required**; deleting the school cascades.<br/>
/// • FK <see cref="LicenseId"/> is **optional**; deleting the licence sets it to <c>null</c>.<br/>
/// • Deleting a category cascades to the join-table
///   <see cref="ApplicationUserTeachingCategory"/> but <b>does not</b> delete files
///   (their FK is set to <c>null</c>).<br/>
/// • <see cref="Code"/> is unique inside one school.
/// </remarks>
[Index(nameof(AutoSchoolId), nameof(Code), IsUnique = true)]
public class TeachingCategory
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int TeachingCategoryId { get; set; }

    /// <summary>Short code (“B”, “C+E”…), unique per school.</summary>
    [Required, StringLength(10)]
    public string Code { get; set; } = null!;

    /// <summary>Cost per driving session (≥ 0).</summary>
    [Range(0, double.MaxValue)]
    public decimal SessionCost { get; set; }

    /// <summary>Duration of one session, in minutes (≥ 0).</summary>
    [Range(0, int.MaxValue)]
    public int SessionDuration { get; set; }

    /// <summary>Total price of the scholarship pack, if any (≥ 0).</summary>
    [Range(0, double.MaxValue)]
    public decimal ScholarshipPrice { get; set; }

    /// <summary>Minimum number of driving lessons required (≥ 0).</summary>
    [Range(0, int.MaxValue)]
    public int MinDrivingLessonsReq { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>FK to the owning <see cref="AutoSchool"/> (mandatory).</summary>
    [ForeignKey(nameof(AutoSchool))]
    public int AutoSchoolId { get; set; }

    /// <summary>Navigation to the auto-school.</summary>
    public virtual AutoSchool AutoSchool { get; set; } = null!;

    /// <summary>FK to the required <see cref="License"/> (optional).</summary>
    [ForeignKey(nameof(License))]
    public int? LicenseId { get; set; }

    /// <summary>Navigation to the driving-license type.</summary>
    public virtual License? License { get; set; }

    /// <summary>Files linked to this category (set-null on delete).</summary>
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    /// <summary>
    /// Join entities that map students/instructors to this category
    /// (cascades on delete).
    /// </summary>
    public virtual ICollection<ApplicationUserTeachingCategory> ApplicationUserTeachingCategories
    { get; set; } = new List<ApplicationUserTeachingCategory>();
}
