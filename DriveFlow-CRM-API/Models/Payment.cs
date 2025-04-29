using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;  

namespace DriveFlow_CRM_API.Models;                  
// ─────────────────────── Payment entity ───────────────────────

/// <summary>
/// Represents a payment made by a student (either the base scholarship
/// fee or individual driving-session payments).
/// </summary>
/// <remarks>
/// • One-to-one with <see cref="File"/> (receipt / invoice).<br/>
/// • <see cref="FileId"/> is the FK; deleting the parent <c>File</c> cascades
///   and deletes this <c>Payment</c>.
/// </remarks>
public class Payment
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int PaymentId { get; set; }

    /// <summary><c>true</c> = base-scholarship payment; <c>false</c> = per-lesson.</summary>
    public bool ScholarshipBasePayment { get; set; }

    /// <summary>Number of driving sessions covered by this payment (≥ 0).</summary>
    [Range(0, int.MaxValue)]
    public int SessionsPayed { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>Foreign key to the attached receipt/invoice file.</summary>
    [ForeignKey(nameof(File))]
    public int FileId { get; set; }

    /// <summary>Navigation property to the receipt/invoice file.</summary>
    public virtual File File { get; set; } = null!;
}
