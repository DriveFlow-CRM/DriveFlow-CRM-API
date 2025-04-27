using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;   

namespace DriveFlow_CRM_API.Models;                 

// ─────────────────────── Appointment entity ───────────────────────

/// <summary>
/// Driving-lesson appointment scheduled between a student and an instructor.
/// </summary>
/// <remarks>
/// • Optional FK <see cref="FileId"/> references a single <see cref="File"/>
///   (e.g. lesson report or signed attendance sheet).<br/>
/// • No special cascade rules: deleting an appointment does <b>not</b> affect the file
///   and vice-versa (EF conventions handle the FK).
/// </remarks>
public class Appointment
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int AppointmentId { get; set; }

    /// <summary>Date of the lesson (local time).</summary>
    public DateTime Date { get; set; }

    /// <summary>Lesson start time.</summary>
    public TimeSpan StartHour { get; set; }

    /// <summary>Lesson end time.</summary>
    public TimeSpan EndHour { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>Optional FK to the associated file (report, document, etc.).</summary>
    [ForeignKey(nameof(File))]
    public int? FileId { get; set; }

    /// <summary>Navigation to the attached file.</summary>
    public virtual File? File { get; set; }
}
