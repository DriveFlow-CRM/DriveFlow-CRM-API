using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow_CRM_API.Models;

// ─────────────────────── Item entity ───────────────────────

/// <summary>
/// Individual exam item (penalty) within a form.
/// Represents specific violations that can be marked during an exam.
/// </summary>
/// <remarks>
/// • FK <see cref="FormularId"/> is **required**.<br/>
/// • Combination of (FormularId, Description) is **unique**.<br/>
/// • Items are ordered by <see cref="OrderIndex"/> for display purposes.<br/>
/// • Immutable after seeding.
/// </remarks>
[Index(nameof(FormularId), nameof(Description), IsUnique = true)]
public class Item
{
    // ─────────────── Keys & properties ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int ItemId { get; set; }

    /// <summary>FK to the parent form (required).</summary>
    [ForeignKey(nameof(Formular))]
    public int FormularId { get; set; }

    /// <summary>Description of the violation or penalty item.</summary>
    [Required, StringLength(500)]
    public string Description { get; set; } = null!;

    /// <summary>Penalty points assigned for this violation.</summary>
    [Range(0, int.MaxValue)]
    public int PenaltyPoints { get; set; }

    /// <summary>Display order index for sorting items.</summary>
    [Range(0, int.MaxValue)]
    public int OrderIndex { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>Navigation to the parent form.</summary>
    public virtual Formular Formular { get; set; } = null!;
}
