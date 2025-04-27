using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;   
             

namespace DriveFlow_CRM_API.Models;                  

// ───────────────────────  Instructor availability ───────────────────────

/// <summary>
/// Time slot in which an <see cref="ApplicationUser"/> acting as instructor
/// is available for driving lessons.
/// </summary>
/// <remarks>
/// • Optional many-to-one toward the instructor (<see cref="ApplicationUser"/>); an availability can exist
///   without being assigned yet.<br/>
/// • No special cascade rules are required; EF Core conventions handle the FK
///   (deleting the instructor sets <see cref="InstructorId"/> to <c>null</c>).
/// </remarks>
public class InstructorAvailability
{
    /// <summary>Primary key.</summary>
    [Key]
    public int IntervalId { get; set; }

    /// <summary>Date of the availability slot (local time).</summary>
    public DateTime Date { get; set; }

    /// <summary>Start hour of the slot.</summary>
    public TimeSpan StartHour { get; set; }

    /// <summary>End hour of the slot.</summary>
    public TimeSpan EndHour { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>Foreign key to the instructor (optional).</summary>
    [ForeignKey(nameof(Instructor))]
    public string? InstructorId { get; set; }

    /// <summary>Navigation to the instructor (M : 1).</summary>
    public virtual ApplicationUser? Instructor { get; set; }
}
