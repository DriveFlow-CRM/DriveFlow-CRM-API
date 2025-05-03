using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;  
using Microsoft.EntityFrameworkCore;                 

namespace DriveFlow_CRM_API.Models;                  

// ─────────────────────── Request entity ───────────────────────

/// <summary>
/// Contact request submitted by a prospective student to an
/// <see cref="AutoSchool"/>.
/// </summary>
[Index(nameof(RequestDate))]
public class Request
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int RequestId { get; set; }

    /// <summary>First name of the applicant.</summary>
    [Required, StringLength(50)]
    public string FirstName { get; set; } = null!;

    /// <summary>Last name of the applicant.</summary>
    [Required, StringLength(50)]
    public string LastName { get; set; } = null!;

    /// <summary>Contact phone number.</summary>
    [Phone, StringLength(30)]
    public string PhoneNumber { get; set; } = null!;

    /// <summary>Desired driving category (“B”, “C” …).</summary>
    [StringLength(10)]
    public string? DrivingCategory { get; set; }


    [Required]
    public string Status { get; set; } = "Pending";

    /// <summary>Date and time when the request was created.</summary>
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    // ─────────────── Relationships ───────────────

    /// <summary>
    /// Optional FK to the targeted <see cref="AutoSchool"/>. Deleting the school cascades
    /// by convention (FK non-nullable); no custom rule required.
    /// </summary>
    [ForeignKey(nameof(AutoSchool))]
    public int? AutoSchoolId { get; set; }

    /// <summary>Navigation to the auto-school.</summary>
    public virtual AutoSchool? AutoSchool { get; set; }
}


