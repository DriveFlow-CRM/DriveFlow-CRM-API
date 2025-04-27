using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;   

namespace DriveFlow_CRM_API.Models;                 

// ───────────────────────  File entity ───────────────────────

/// <summary>
/// File entity that stores any document uploaded to the platform:
/// invoices, lesson sheets, vehicle papers, etc.
/// </summary>
/// <remarks>
/// • One-to-one with <see cref="Payment"/> via <c>PaymentId</c>; deleting this file cascades to its payment.<br/>
/// • One-to-many with <see cref="Appointment"/>; deleting the file cascades to its appointments.<br/>
/// • Optional many-to-one toward <see cref="Vehicle"/>, <see cref="TeachingCategory"/>,
///   and an <see cref="ApplicationUser"/> acting as instructor.<br/>
/// • Mandatory many-to-one toward a student (<see cref="ApplicationUser"/>).<br/>
/// • Other optional links are set to <c>null</c> when the related entity is deleted.
/// </remarks>
public class File
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int FileId { get; set; }

    /// <summary>Date when scholarship starts, if relevant.</summary>
    public DateTime? ScholarshipStartDate { get; set; }

    /// <summary>Criminal-record certificate expiry.</summary>
    public DateTime? CriminalRecordExpiryDate { get; set; }

    /// <summary>Medical record expiry.</summary>
    public DateTime? MedicalRecordExpiryDate { get; set; }

    /// <summary>Current processing status of the file.</summary>
    public FileStatus Status { get; set; } = FileStatus.Draft;

    // ─────────────── Relationships ───────────────

    /// <summary>Appointments backed by this file (1 : M, cascade).</summary>
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    /// <summary>Foreign key to the linked <see cref="Vehicle"/> (optional).</summary>
    [ForeignKey(nameof(Vehicle))]
    public int? VehicleId { get; set; }

    /// <summary>Navigation to the vehicle (M : 1, set-null).</summary>
    public virtual Vehicle? Vehicle { get; set; }

    /// <summary>Foreign key to the linked <see cref="TeachingCategory"/> (optional).</summary>
    [ForeignKey(nameof(TeachingCategory))]
    public int? TeachingCategoryId { get; set; }

    /// <summary>Navigation to the teaching category (M : 1, set-null).</summary>
    public virtual TeachingCategory? TeachingCategory { get; set; }

    /// <summary>Foreign key to the student who owns this file (required).</summary>
    [Required]
    [ForeignKey(nameof(Student))]
    public required string StudentId { get; set; }

    /// <summary>Navigation to the student (M : 1).</summary>
    public virtual ApplicationUser Student { get; set; } = null!;

    /// <summary>Foreign key to the instructor (optional).</summary>
    [ForeignKey(nameof(Instructor))]
    public string? InstructorId { get; set; }

    /// <summary>Navigation to the instructor (M : 1, set-null).</summary>
    public virtual ApplicationUser? Instructor { get; set; }
}

// ───────────────  Transmission enum ───────────────

/// <summary>Status values for <see cref="File"/>.</summary>

public enum FileStatus
{
    Draft,
    Approved,
    Rejected
}
