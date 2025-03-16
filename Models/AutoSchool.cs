using DriveFlow_CRM_API.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class AutoSchool
{
    [Key]
    public int AutoSchoolId { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? WebSite { get; set; }

    // Foreign key to Address
    public int AddressId { get; set; }

    [ForeignKey(nameof(AddressId))]
    public virtual Address? Address { get; set; }

    // 1-to-many relationship with Requests (already present)
    public virtual ICollection<Request>? Requests { get; set; }

    // 1-to-many relationship with ApplicationUser
    public virtual ICollection<ApplicationUser>? ApplicationUsers { get; set; }

    // 1-to-many relationship with Vehicles
    public virtual ICollection<Vehicle>? Vehicles { get; set; }

    public virtual ICollection<TeachingCategory>? TeachingCategories{ get; set; }
}
