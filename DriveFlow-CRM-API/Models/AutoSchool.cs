using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;   
using Microsoft.EntityFrameworkCore;                 

namespace DriveFlow_CRM_API.Models;                  

// ─────────────────────── AutoSchool entity ───────────────────────

/// <summary>
/// Driving school (AutoSchool) entity.  
/// Holds business details and connects to users, vehicles, teaching categories
/// and requests.
/// </summary>
/// <remarks>
/// • <see cref="PhoneNumber"/> and <see cref="Email"/> are unique.<br/>
/// • Optional FK <see cref="AddressId"/> → <see cref="Address"/>.<br/>
/// • Deleting an AutoSchool cascades to Users, Vehicles, TeachingCategories, Requests
///   (configured in <c>OnModelCreating</c>).
/// </remarks>
[Index(nameof(PhoneNumber), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
public class AutoSchool
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int AutoSchoolId { get; set; }

    /// <summary>School name.</summary>
    [Required, StringLength(150)]
    public string Name { get; set; } = null!;

    /// <summary>Description (optional).</summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Website URL (optional).</summary>
    [StringLength(200)]
    public string? WebSite { get; set; }

    /// <summary>Contact phone number (unique).</summary>
    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    /// <summary>Contact e-mail address (unique, RFC-5322 regex).</summary>
    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    /// <summary>Operational status of the school.</summary>
    public AutoSchoolStatus Status { get; set; } = AutoSchoolStatus.Active;

    // ─────────────── Relationships ───────────────

    /// <summary>Optional FK to the headquarters <see cref="Address"/>.</summary>
    [ForeignKey(nameof(Address))]
    public int? AddressId { get; set; }

    /// <summary>Navigation to headquarters address.</summary>
    public virtual Address? Address { get; set; }

    /// <summary>All users (students / instructors / admins) linked to this school.</summary>
    public virtual ICollection<ApplicationUser> ApplicationUsers { get; set; } = new List<ApplicationUser>();

    /// <summary>Fleet of vehicles owned by the school.</summary>
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    /// <summary>Teaching categories the school is certified for (at least one).</summary>
    public virtual ICollection<TeachingCategory> TeachingCategories { get; set; } = new List<TeachingCategory>();

    /// <summary>Requests submitted for this school.</summary>
    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}

// ───────────────────────  AutoSchoolStatus enum ───────────────────────

/// <summary>Status values for <see cref="AutoSchool"/>.</summary>
public enum AutoSchoolStatus
{
    Active,
    Restricted,
    Demo
}
