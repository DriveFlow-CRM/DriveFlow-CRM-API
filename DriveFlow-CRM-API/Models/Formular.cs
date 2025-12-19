using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow_CRM_API.Models;

// ─────────────────────── Formular entity ───────────────────────

/// <summary>
/// Official exam form for a specific teaching category.
/// Contains the maximum penalty points allowed and related items.
/// </summary>
/// <remarks>
/// • FK <see cref="TeachingCategoryId"/> is **required** and **unique** per category.<br/>
/// • Immutable after seeding – no CRUD operations allowed after initial creation.<br/>
/// • Each category has exactly one form.
/// </remarks>
[Index(nameof(TeachingCategoryId), IsUnique = true)]
public class Formular
{
    // ─────────────── Keys & properties ───────────────

    /// <summary>Primary key.</summary>
    [Key]
    public int FormularId { get; set; }

    /// <summary>FK to the teaching category (unique).</summary>
    [ForeignKey(nameof(TeachingCategory))]
    public int TeachingCategoryId { get; set; }

    /// <summary>Maximum penalty points allowed for passing the exam.</summary>
    [Range(0, int.MaxValue)]
    public int MaxPoints { get; set; }

    // ─────────────── Relationships ───────────────

    /// <summary>Navigation to the teaching category.</summary>
    public virtual TeachingCategory TeachingCategory { get; set; } = null!;

    /// <summary>Collection of exam items associated with this form.</summary>
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
