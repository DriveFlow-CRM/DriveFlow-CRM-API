using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// Manages teaching categories scoped per driving school.
/// </summary>
/// <remarks>
/// <para><strong>Access Rules:</strong></para>
/// - <c>GET</c> allowed only for <c>SuperAdmin</c>.  
/// - <c>POST / PUT / DELETE</c> allowed only for <c>SchoolAdmin</c> and only for their own school.  
/// <para><strong>Ownership Check:</strong></para>
/// For POST/PUT/DELETE, verifies that the authenticated user's <c>AutoSchoolId</c> matches the <c>schoolId</c> in the route.
/// </remarks>
[Route("api/[controller]")]
[ApiController]
public class TeachingCategoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeachingCategoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ──────────────────────── GET CATEGORIES ────────────────────────
    /// <summary>
    /// Returns all teaching categories for a given school.
    /// </summary>
    /// <remarks>
    /// <para><strong>Allowed only for SuperAdmin</strong></para>
    ///
    /// Sample response:
    /// ```json
    /// [
    ///   {
    ///     "teachingCategoryId": 10,
    ///     "licenseId": 1,
    ///     "licenseType": "B",
    ///     "sessionCost": 120,
    ///     "sessionDuration": 90,
    ///     "scholarshipPrice": 2500,
    ///     "minDrivingLessonsReq": 30
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="schoolId">The school identifier.</param>
    /// <response code="200">Returns list of categories.</response>
    /// <response code="401">JWT is missing or invalid.</response>
    /// <response code="403">User is not a SuperAdmin.</response>
    [HttpGet("get/{schoolId:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetTeachingCategories(int schoolId)
    {
        var categories = await _context.TeachingCategories
            .Where(tc => tc.AutoSchoolId == schoolId)
            .Select(tc => new
            {
                tc.TeachingCategoryId,
                tc.LicenseId,
                licenseType = tc.License!.Type,
                tc.SessionCost,
                tc.SessionDuration,
                tc.ScholarshipPrice,
                tc.MinDrivingLessonsReq
            })
            .ToListAsync();

        return Ok(categories);
    }

    // ──────────────────────── CREATE CATEGORY ────────────────────────
    /// <summary>
    /// Creates a new teaching category for the authenticated school admin.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample body:</strong></para>
    /// ```json
    /// {
    ///   "licenseId": 1,
    ///   "sessionCost": 120,
    ///   "sessionDuration": 90,
    ///   "scholarshipPrice": 2500,
    ///   "minDrivingLessonsReq": 30
    /// }
    /// ```
    /// </remarks>
    /// <param name="schoolId">The school ID (must match user's school).</param>
    /// <param name="dto">Teaching category to create (without ID).</param>
    /// <response code="201">Category created successfully.</response>
    /// <response code="403">Caller does not belong to this school.</response>
    [HttpPost("create/{schoolId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> CreateTeachingCategory(int schoolId, [FromBody] TeachingCategory dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || user.AutoSchoolId != schoolId)
            return Forbid();

        dto.AutoSchoolId = schoolId;
        dto.AutoSchool = null;

        _context.TeachingCategories.Add(dto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTeachingCategories), new { schoolId }, new
        {
            dto.TeachingCategoryId,
            message = "Teaching category created successfully"
        });
    }

    // ──────────────────────── UPDATE CATEGORY ────────────────────────
    /// <summary>
    /// Updates an existing teaching category for the authenticated admin's school.
    /// </summary>
    /// <remarks>
    /// <para><strong>PUT body (excluding AutoSchoolId):</strong></para>
    /// ```json
    /// {
    ///   "licenseId": 1,
    ///   "sessionCost": 130,
    ///   "sessionDuration": 90,
    ///   "scholarshipPrice": 2600,
    ///   "minDrivingLessonsReq": 32
    /// }
    /// ```
    /// </remarks>
    /// <param name="schoolId">The school ID (must match user's school).</param>
    /// <param name="teachingCategoryId">The ID of the category to update.</param>
    /// <param name="dto">Updated data.</param>
    /// <response code="200">Update successful.</response>
    /// <response code="403">Caller does not belong to this school.</response>
    /// <response code="404">Teaching category not found.</response>
    [HttpPut("update/{schoolId:int}/{teachingCategoryId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> UpdateTeachingCategory(int schoolId, int teachingCategoryId, [FromBody] TeachingCategory dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || user.AutoSchoolId != schoolId)
            return Forbid();

        var category = await _context.TeachingCategories
            .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == teachingCategoryId && tc.AutoSchoolId == schoolId);

        if (category == null)
            return NotFound();

        category.LicenseId = dto.LicenseId;
        category.SessionCost = dto.SessionCost;
        category.SessionDuration = dto.SessionDuration;
        category.ScholarshipPrice = dto.ScholarshipPrice;
        category.MinDrivingLessonsReq = dto.MinDrivingLessonsReq;
        category.Code = dto.Code;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Teaching category updated successfully" });
    }

    // ──────────────────────── DELETE CATEGORY ────────────────────────
    /// <summary>
    /// Deletes a teaching category, if it belongs to the caller’s school.
    /// </summary>
    /// <param name="schoolId">The school ID.</param>
    /// <param name="teachingCategoryId">ID of the category to delete.</param>
    /// <response code="204">Category deleted successfully.</response>
    /// <response code="403">Caller does not belong to this school.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("delete/{schoolId:int}/{teachingCategoryId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> DeleteTeachingCategory(int schoolId, int teachingCategoryId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || user.AutoSchoolId != schoolId)
            return Forbid();

        var category = await _context.TeachingCategories
            .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == teachingCategoryId && tc.AutoSchoolId == schoolId);

        if (category == null)
            return NotFound();

        _context.TeachingCategories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
