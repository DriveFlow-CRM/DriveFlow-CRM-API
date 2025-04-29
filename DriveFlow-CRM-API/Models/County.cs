using System.ComponentModel.DataAnnotations;          
using Microsoft.EntityFrameworkCore;                 

namespace DriveFlow_CRM_API.Models;                  

// ─────────────────────── County entity ───────────────────────

/// <summary>
/// Administrative unit that may contain multiple cities.
/// </summary>
/// <remarks>
/// Uniqueness of <see cref="Name"/> and <see cref="Abbreviation"/> is enforced
/// via indexes declared both here and in <c>OnModelCreating</c>.
/// </remarks>
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Abbreviation), IsUnique = true)]
public class County
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int CountyId { get; set; }

    /// <summary>Full county name (unique).</summary>
    [Required, StringLength(150)]
    public string Name { get; set; } = null!;

    /// <summary>Short code, e.g. “AB” (unique).</summary>
    [Required, StringLength(10)]
    public string Abbreviation { get; set; } = null!;

    // ─────────────── Relationships ───────────────

    /// <summary>
    /// Cities that belong to this county (1 : M, cascade delete).
    /// </summary>
    public virtual ICollection<City> Cities { get; set; } = new List<City>();
}
