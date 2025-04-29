using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;   
using Microsoft.AspNetCore.Identity;                

namespace DriveFlow_CRM_API.Models;                 

// ─────────────────────── ApplicationUser entity ───────────────────────

/// <summary>
/// Platform user. Identity roles (<c>SuperAdmin</c>, <c>SchoolAdmin</c>,
/// <c>Instructor</c>, <c>Student</c>) decide at runtime what operations are allowed,
/// but the database schema remains a single <see cref="ApplicationUser"/> table.
/// </summary>
/// <remarks>
/// • Optional FK <see cref="AutoSchoolId"/> — only school-level roles belong to a school.<br/>
/// • One-to-many toward <see cref="File"/>, <see cref="InstructorAvailability"/>,
///   and the bridge entity <see cref="ApplicationUserTeachingCategory"/>.<br/>
/// • Deleting the user sets those FKs to <c>null</c>
///   (<c>DeleteBehavior.SetNull</c> in <c>OnModelCreating</c>).
/// </remarks>
public class ApplicationUser : IdentityUser
{
    // ─────────────── Keys & status ───────────────

    /// <summary>User’s given name.</summary>
    public string? FirstName { get; set; }

    /// <summary>User’s family name.</summary>
    public string? LastName { get; set; }

    /// <summary>National identifier (CNP, 13 digits).</summary>
    [StringLength(13)]
    public string? Cnp { get; set; }

    /// <summary>FK to the driving school this user belongs to (optional).</summary>
    public int? AutoSchoolId { get; set; }

    /// <summary>Navigation to the owning <see cref="AutoSchool"/>.</summary>
    [ForeignKey(nameof(AutoSchoolId))]
    public virtual AutoSchool? AutoSchool { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>
    /// Files owned by the user in their <b>Student</b> role
    /// (medical record, criminal record, contract etc.).
    /// </summary>
    public virtual ICollection<File> StudentFiles { get; set; } = new List<File>();

    /// <summary>
    /// Files uploaded by the user in their <b>Instructor</b> role
    /// (lesson sheets, attendance, grading documents).
    /// </summary>
    public virtual ICollection<File> InstructorFiles { get; set; } = new List<File>();

    /// <summary>Availability slots (only relevant for instructors).</summary>
    public virtual ICollection<InstructorAvailability> InstructorAvailabilities
    { get; set; } = new List<InstructorAvailability>();

    /// <summary>Bridge rows that link the user to teaching categories.</summary>
    public virtual ICollection<ApplicationUserTeachingCategory> ApplicationUserTeachingCategories
    { get; set; } = new List<ApplicationUserTeachingCategory>();
}
