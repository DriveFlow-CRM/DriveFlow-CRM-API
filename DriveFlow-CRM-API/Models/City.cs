using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;  
using Microsoft.EntityFrameworkCore;                 

namespace DriveFlow_CRM_API.Models;                  

// ─────────────────────── City entity ───────────────────────

/// <summary>
/// City that belongs to a <see cref="County"/> and can hold
/// multiple <see cref="Address"/> entries.
/// </summary>
/// <remarks>
/// • Required FK <see cref="CountyId"/> ⇒ deleting a county cascades to its cities.<br/>
/// • Relationship City (1) → Address (M) is configured in <c>OnModelCreating</c>.
/// </remarks>
public class City
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int CityId { get; set; }

    /// <summary>City name.</summary>
    [Required, StringLength(150)]
    public string Name { get; set; } = null!;

    // ─────────────── Relationships ───────────────

    /// <summary>Foreign key to the parent <see cref="County"/> (required).</summary>
    [ForeignKey(nameof(County))]
    public int CountyId { get; set; }

    /// <summary>Navigation to the parent county.</summary>
    public virtual County County { get; set; } = null!;

    /// <summary>All addresses located in this city.</summary>
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
}
