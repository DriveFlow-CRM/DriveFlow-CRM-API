using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <-- Adaugat pentru ForeignKey
using Microsoft.EntityFrameworkCore;
namespace DriveFlow_CRM_API.Models;

/// <summary>
///     Physical address located in a <see cref="City"/>; it can optionally be
///     linked one-to-one to an <see cref="AutoSchool"/> (AutoSchool holds the FK).
/// </summary>
/// <remarks>
///     • <see cref="Postcode"/> is unique across all addresses.  
///     • Required FK <see cref="CityId"/> ⇒ deleting a city cascades to its addresses.  
///     • The optional 1 : 1 relation Address ↔ AutoSchool is configured in <c>OnModelCreating</c>.
/// </remarks>
[Index(nameof(Postcode), IsUnique = true)]
public class Address
{
    /// <summary>Primary key.</summary>
    [Key]
    public int AddressId { get; set; }

    /// <summary>Street name.</summary>
    [Required, StringLength(100)]
    public string StreetName { get; set; } = null!;

    /// <summary>Street / house number (optional).</summary>
    [StringLength(10)]
    public string? AddressNumber { get; set; }

    /// <summary>Postal code (unique, optional).</summary>
    [StringLength(10)]
    public string? Postcode { get; set; }

    // ──────────────── Relationships ────────────────

    /// <summary>Required foreign key to the parent city.</summary>
    [ForeignKey(nameof(City))] 
    public int CityId { get; set; }

    /// <summary>Navigation to the parent <see cref="City"/>.</summary>
    public virtual City City { get; set; } = null!;
}
