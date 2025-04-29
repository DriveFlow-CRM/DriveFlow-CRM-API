using System.ComponentModel.DataAnnotations;          
using Microsoft.EntityFrameworkCore;                 

namespace DriveFlow_CRM_API.Models;                  

// ─────────────────────── License entity ───────────────────────

/// <summary>
/// Driving-licence category (e.g. “A”, “B”, “C”).  
/// A licence can be referenced by multiple <see cref="Vehicle"/> and
/// multiple <see cref="TeachingCategory"/> rows.
/// </summary>
/// <remarks>
/// • <see cref="Type"/> is unique (A, B, C …).<br/>
/// • Deleting a licence sets the FK to <c>null</c> in <see cref="Vehicle"/> and
///   <see cref="TeachingCategory"/> (configured in <c>OnModelCreating</c>).
/// </remarks>
[Index(nameof(Type), IsUnique = true)]
public class License
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int LicenseId { get; set; }

    /// <summary>Licence code (A, B, C …).</summary>
    [Required, StringLength(5)]
    public string Type { get; set; } = null!;

    // ─────────────── Relationships ───────────────

    /// <summary>Vehicles that require this licence.</summary>
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    /// <summary>Teaching categories associated with this licence.</summary>
    public virtual ICollection<TeachingCategory> TeachingCategories { get; set; } = new List<TeachingCategory>();
}
