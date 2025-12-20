using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using DriveFlow_CRM_API.Models.DTOs;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// Controller for managing exam forms and their items.
/// Each teaching category has one immutable exam form with standardized penalty items.
/// </summary>
[ApiController]
[Route("api/forms")]
public class ExamFormController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ExamFormController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ─────────────────────── GET FORM BY CATEGORY ───────────────────────
    /// <summary>
    /// Retrieves the exam form and all its items for a specific teaching category.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response (200 OK)</strong></para>
    ///
    /// ```json
    /// {
    ///   "id_formular": 1,
    ///   "id_categ": 1,
    ///   "maxPoints": 21,
    ///   "items": [
    ///     {
    ///       "id_item": 1,
    ///       "description": "Semnalizare la schimbarea direc?iei",
    ///       "penaltyPoints": 3,
    ///       "orderIndex": 1
    ///     },
    ///     {
    ///       "id_item": 2,
    ///       "description": "Neasigurare la plecarea de pe loc",
    ///       "penaltyPoints": 3,
    ///       "orderIndex": 2
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="id_categ">Teaching category ID.</param>
    /// <response code="200">Form retrieved successfully.</response>
    /// <response code="400">Invalid category ID.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="404">Category or form not found.</response>
    [HttpGet("by-category/{id_categ:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ExamFormDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFormByCategory(int id_categ)
    {
        if (id_categ <= 0)
            return BadRequest(new { message = "Category ID must be positive." });

        var form = await _db.ExamForms
            .AsNoTracking()
            .Where(f => f.TeachingCategoryId == id_categ)
            .Include(f => f.Items.OrderBy(i => i.OrderIndex))
            .FirstOrDefaultAsync();

        if (form == null)
            return NotFound(new { message = "Exam form not found for this category." });

        var itemDtos = form.Items
            .Select(i => new ExamItemDto(
                id_item: i.ItemId,
                description: i.Description,
                penaltyPoints: i.PenaltyPoints,
                orderIndex: i.OrderIndex
            ))
            .ToList();

        var formDto = new ExamFormDto(
            id_formular: form.FormId,
            id_categ: form.TeachingCategoryId,
            maxPoints: form.MaxPoints,
            items: itemDtos
        );

        return Ok(formDto);
    }

    // ─────────────────────── SEED FORM (OPTIONAL - SchoolAdmin only) ───────────────────────
    /// <summary>
    /// Seeds or updates the exam form for a teaching category (SchoolAdmin only, same school).
    /// Idempotent: if form already exists, returns 200; otherwise creates it with status 201.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request body</strong></para>
    ///
    /// ```json
    /// {
    ///   "maxPoints": 21,
    ///   "items": [
    ///     {
    ///       "description": "Semnalizare la schimbarea direc?iei",
    ///       "penaltyPoints": 3,
    ///       "orderIndex": 1
    ///     },
    ///     {
    ///       "description": "Neasigurare la plecarea de pe loc",
    ///       "penaltyPoints": 3,
    ///       "orderIndex": 2
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="teachingCategoryId">Teaching category ID.</param>
    /// <param name="dto">Form data (maxPoints and items).</param>
    /// <response code="200">Form already existed; updated with new data.</response>
    /// <response code="201">Form created successfully.</response>
    /// <response code="400">Invalid data or category not found.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is not a SchoolAdmin of the correct school.</response>
    /// <response code="404">Teaching category not found.</response>
    [HttpPost("seed/{teachingCategoryId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> SeedForm(int teachingCategoryId, [FromBody] CreateExamFormDto dto)
    {
        if (teachingCategoryId <= 0)
            return BadRequest(new { message = "Teaching category ID must be positive." });

        var caller = await _users.GetUserAsync(User);
        if (caller == null)
            return Unauthorized("User not found.");

        var category = await _db.TeachingCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == teachingCategoryId);

        if (category == null)
            return NotFound(new { message = "Teaching category not found." });

        // Ensure the SchoolAdmin belongs to the same school
        if (caller.AutoSchoolId != category.AutoSchoolId)
            return Forbid();

        // Check if form already exists
        var existingForm = await _db.ExamForms
            .FirstOrDefaultAsync(f => f.TeachingCategoryId == teachingCategoryId);

        if (existingForm != null)
        {
            // Update existing form
            existingForm.MaxPoints = dto.maxPoints;

            // Remove old items and add new ones
            _db.ExamItems.RemoveRange(existingForm.Items);
            existingForm.Items = dto.items
                .OrderBy(i => i.orderIndex)
                .Select((i, idx) => new ExamItem
                {
                    Description = i.description,
                    PenaltyPoints = i.penaltyPoints,
                    OrderIndex = idx + 1
                })
                .ToList();

            await _db.SaveChangesAsync();

            return Ok(new { message = "Form updated successfully.", formId = existingForm.FormId });
        }

        // Create new form
        var newForm = new ExamForm
        {
            TeachingCategoryId = teachingCategoryId,
            MaxPoints = dto.maxPoints,
            Items = dto.items
                .OrderBy(i => i.orderIndex)
                .Select((i, idx) => new ExamItem
                {
                    Description = i.description,
                    PenaltyPoints = i.penaltyPoints,
                    OrderIndex = idx + 1
                })
                .ToList()
        };

        _db.ExamForms.Add(newForm);
        await _db.SaveChangesAsync();

        return Created($"/api/examform/by-category/{teachingCategoryId}", 
            new { message = "Form created successfully.", formId = newForm.FormId });
    }
}

/// <summary>DTO for seeding exam form data.</summary>
public sealed class CreateExamFormDto
{
    /// <summary>Maximum points for this exam form.</summary>
    public int maxPoints { get; init; }

    /// <summary>List of exam items (infractions with penalties).</summary>
    public List<CreateExamItemDto> items { get; init; } = new();
}

/// <summary>DTO for a single exam item in the seed request.</summary>
public sealed class CreateExamItemDto
{
    /// <summary>Description of the infraction.</summary>
    public string description { get; init; } = null!;

    /// <summary>Penalty points for this infraction.</summary>
    public int penaltyPoints { get; init; }

    /// <summary>Display order (will be normalized to 1-based).</summary>
    public int orderIndex { get; init; }
}
