using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using DriveFlow_CRM_API.Models.DTOs;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// Controller for managing exam forms and their items.
/// Provides read-only access to seeded form data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FormsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    /// <summary>Constructor invoked per request by DI.</summary>
    public FormsController(ApplicationDbContext db) => _db = db;

    // ────────────────────────────── GET FORM BY CATEGORY ──────────────────────────────
    /// <summary>
    /// Retrieves the exam form and its items for a specific teaching category.
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
    ///       "description": "Semnalizare la schimbarea direcției",
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
    /// <param name="id_categ">The teaching category ID.</param>
    /// <response code="200">Form and items returned successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">
    /// Authenticated user does not have one of the allowed roles
    /// (<c>SuperAdmin</c>, <c>SchoolAdmin</c>, or <c>Instructor</c>).
    /// </response>
    /// <response code="404">No form found for the specified teaching category.</response>
    [HttpGet("by-category/{id_categ}")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,Instructor")]
    [ProducesResponseType(typeof(FormDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFormByCategoryAsync(int id_categ)
    {
        // Validate input
        if (id_categ <= 0)
            return BadRequest(new { message = "id_categ must be a positive integer." });

        // Query form with items
        var formular = await _db.Formulars
            .AsNoTracking()
            .Include(f => f.Items)
            .FirstOrDefaultAsync(f => f.TeachingCategoryId == id_categ);

        if (formular == null)
            return NotFound(new { message = "No form found for the specified teaching category." });

        // Map to DTO
        var formDto = new FormDto(
            id_formular: formular.FormularId,
            id_categ: formular.TeachingCategoryId,
            maxPoints: formular.MaxPoints,
            items: formular.Items
                .OrderBy(i => i.OrderIndex)
                .Select(i => new ItemDto(
                    id_item: i.ItemId,
                    description: i.Description,
                    penaltyPoints: i.PenaltyPoints,
                    orderIndex: i.OrderIndex
                ))
        );

        return Ok(formDto);
    }
}
