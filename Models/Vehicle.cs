using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;   
using Microsoft.EntityFrameworkCore;                 

namespace DriveFlow_CRM_API.Models;                  

// ───────────────────────  Vehicle entity ───────────────────────

/// <summary>
/// Vehicle owned (optionally) by an <see cref="AutoSchool"/>; stores
/// technical data and may have related files (photos, docs).
/// </summary>
/// <remarks>
/// • <see cref="LicensePlateNumber"/> is unique.<br/>
/// • Optional FK <see cref="LicenseId"/> → <see cref="License"/> (M : 1).<br/>
/// • Relationship Vehicle (1) → File (M) is configured in <c>OnModelCreating</c>.
/// </remarks>
[Index(nameof(LicensePlateNumber), IsUnique = true)]
public class Vehicle
{
    // ─────────────── Keys & status ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int VehicleId { get; set; }

    /// <summary>Unique licence-plate number.</summary>
    [Required, StringLength(15)]
    public string LicensePlateNumber { get; set; } = null!;

    /// <summary>Transmission type.</summary>
    public TransmissionType TransmissionType { get; set; }

    /// <summary>Paint colour.</summary>
    [StringLength(30)]
    public string? Color { get; set; }

    /// <summary>Technical inspection expiry (ITP).</summary>
    public DateTime? ItpExpiryDate { get; set; }

    /// <summary>Comprehensive insurance expiry.</summary>
    public DateTime? InsuranceExpiryDate { get; set; }

    /// <summary>Liability insurance (RCA) expiry.</summary>
    public DateTime? RcaExpiryDate { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>Optional FK to the required driving-license type.</summary>
    [ForeignKey(nameof(License))]
    public int? LicenseId { get; set; }

    /// <summary>Navigation to the driving-license entity.</summary>
    public virtual License? License { get; set; }

    /// <summary>Optional FK to the owning <see cref="AutoSchool"/>.</summary>
    [ForeignKey(nameof(AutoSchool))]
    public int? AutoSchoolId { get; set; }

    /// <summary>Navigation to the auto-school.</summary>
    public virtual AutoSchool? AutoSchool { get; set; }

    /// <summary>Files (documents, photos) attached to this vehicle.</summary>
    public virtual ICollection<File> Files { get; set; } = new List<File>();
}

// ───────────────  Transmission enum ───────────────

/// <summary>Manual / Automatic transmission.</summary>
public enum TransmissionType
{
    Manual,
    Automatic
}
