using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveFlow_CRM_API.Models;

/// <summary>
/// Represents a session form filled during a driving lesson to record mistakes.
/// </summary>
/// <remarks>
/// • One-to-one with <see cref="Appointment"/> (one form per appointment).<br/>
/// • FK <see cref="AppointmentId"/> is unique (ensures single active form per lesson).<br/>
/// • FK <see cref="FormId"/> references the official <see cref="ExamForm"/>.<br/>
/// • <see cref="MistakesJson"/> stores mistakes as JSON array: [{ "id_item": 1, "count": 3 }, ...].<br/>
/// • <see cref="IsLocked"/> indicates if form is finalized (immutable after lock).<br/>
/// • Deleting the appointment cascades and deletes this form.
/// </remarks>
public class SessionForm
{
    /// <summary>Primary key.</summary>
    [Key]
    public int SessionFormId { get; set; }

    /// <summary>Foreign key to the appointment (unique - one form per appointment).</summary>
    [ForeignKey(nameof(Appointment))]
    public int AppointmentId { get; set; }

    /// <summary>Foreign key to the official exam form template.</summary>
    [ForeignKey(nameof(ExamForm))]
    public int FormId { get; set; }

    /// <summary>JSON array of mistakes: [{ "id_item": 1, "count": 3 }, ...].</summary>
    [Required]
    public string MistakesJson { get; set; } = "[]";

    /// <summary>Indicates if the form is locked (finalized, immutable).</summary>
    public bool IsLocked { get; set; }

    /// <summary>Timestamp when the form was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp when the form was finalized (locked).</summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>Total penalty points calculated when finalized.</summary>
    public int? TotalPoints { get; set; }

    /// <summary>Result of the session (e.g., "PASSED", "FAILED").</summary>
    [StringLength(50)]
    public string? Result { get; set; }

    /// <summary>Navigation property to the appointment.</summary>
    public virtual Appointment Appointment { get; set; } = null!;

    /// <summary>Navigation property to the exam form template.</summary>
    public virtual ExamForm ExamForm { get; set; } = null!;
}
