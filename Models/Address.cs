using DriveFlow_CRM_API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Address
{
    [Key]
    public int AddressId { get; set; }

    [Required]
    [StringLength(100)]
    public string StreetName { get; set; } = null!;

    [StringLength(10)]
    public string? AddressNr { get; set; }

    [StringLength(10)]
    public string? Postcode { get; set; }

    // Foreign key to City
    public int CityId { get; set; }

    [ForeignKey(nameof(CityId))]
    public virtual City? City { get; set; }
}
