using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachingCategoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public TeachingCategoryController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ────────────────────────────── GET TEACHING CATEGORIES ──────────────────────────────
    /// <summary>
    /// Returns all teaching categories that belong to the specified school
    /// (SuperAdmin for any school; SchoolAdmin only for their own school – otherwise 403).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
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
    /// <param name="schoolId">School identifier from the route.</param>
    /// <response code="200">List returned successfully (can be empty).</response>
    /// <response code="400"><paramref name="schoolId"/> is not positive.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is a <c>SchoolAdmin</c> of another school.</response>
    [HttpGet("get/{schoolId:int}")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> GetTeachingCategories(int schoolId)
    {
        // Validate route parameter
        if (schoolId <= 0)
            return BadRequest(new { message = "schoolId must be a positive integer." });

        // Identify caller (id & associated school)
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users
                                   .AsNoTracking()
                                   .Select(u => new { u.Id, u.AutoSchoolId })
                                   .FirstOrDefaultAsync(u => u.Id == callerId);

        var isSchoolAdmin = User.IsInRole("SchoolAdmin");

        // A SchoolAdmin may access only their own school
        if (isSchoolAdmin && caller?.AutoSchoolId != schoolId)
            return Forbid();

        // Query and project teaching categories with license type
        var categories = await _db.TeachingCategories
                                .AsNoTracking()
                                .Where(tc => tc.AutoSchoolId == schoolId)
                                .Include(tc => tc.License)
                                .OrderBy(tc => tc.TeachingCategoryId)
                                .Select(tc => new TeachingCategoryResponseDto
                                {
                                    TeachingCategoryId = tc.TeachingCategoryId,
                                    LicenseId = tc.LicenseId ?? 0,
                                    LicenseType = tc.License != null ? tc.License.Type : null,
                                    SessionCost = tc.SessionCost,
                                    SessionDuration = tc.SessionDuration,
                                    ScholarshipPrice = tc.ScholarshipPrice,
                                    MinDrivingLessonsReq = tc.MinDrivingLessonsReq
                                })
                                .ToListAsync();

        return Ok(categories);
    }

    // ────────────────────────────── POST TEACHING CATEGORY ──────────────────────────────
    /// <summary>
    /// Creates a new teaching category in the specified school (SchoolAdmin only, same school).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
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
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="dto">Teaching category data.</param>
    /// <response code="201">Teaching category created successfully.</response>
    /// <response code="400">Validation failed (missing fields, invalid licenseId).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is SchoolAdmin of a different school.</response>
    [HttpPost("create/{schoolId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> CreateTeachingCategory(int schoolId, [FromBody] TeachingCategoryCreateDto dto)
    {
        // ─── caller must belong to this school ───
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users.AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.Id == callerId);

        if (caller?.AutoSchoolId != schoolId)
            return Forbid();

        // ─── basic validation ───
        if (dto.LicenseId <= 0 || dto.SessionDuration <= 0 || dto.MinDrivingLessonsReq < 0)
        {
            return BadRequest(new
            {
                message = "licenseId must be positive, sessionDuration must be positive, and minDrivingLessonsReq must be non-negative."
            });
        }

        // license must exist
        if (!await _db.Licenses.AnyAsync(l => l.LicenseId == dto.LicenseId))
            return BadRequest(new { message = "licenseId does not reference an existing license." });

        // Get license type for code generation
        var license = await _db.Licenses
                              .AsNoTracking()
                              .FirstOrDefaultAsync(l => l.LicenseId == dto.LicenseId);
        
        if (license == null)
            return BadRequest(new { message = "License not found." });

        // Generate a unique code based on license type
        string code = license.Type;
        int counter = 1;
        
        while (await _db.TeachingCategories.AnyAsync(tc => 
            tc.AutoSchoolId == schoolId && tc.Code == code))
        {
            code = $"{license.Type}{counter}";
            counter++;
        }

        // ─── insert ───
        var category = new TeachingCategory
        {
            Code = code,
            LicenseId = dto.LicenseId,
            SessionCost = dto.SessionCost,
            SessionDuration = dto.SessionDuration,
            ScholarshipPrice = dto.ScholarshipPrice,
            MinDrivingLessonsReq = dto.MinDrivingLessonsReq,
            AutoSchoolId = schoolId
        };

        _db.TeachingCategories.Add(category);
        await _db.SaveChangesAsync();   // generates TeachingCategoryId

        return Created(
            $"/api/teachingCategory/get/{schoolId}",
            new { teachingCategoryId = category.TeachingCategoryId, message = "Teaching category created successfully" });
    }

    // ────────────────────────────── PUT TEACHING CATEGORY ──────────────────────────────
    /// <summary>
    /// Updates an existing teaching category (SchoolAdmin of the same school).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
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
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="teachingCategoryId">Teaching category identifier from the route.</param>
    /// <param name="dto">Updated data.</param>
    /// <response code="200">Teaching category updated successfully.</response>
    /// <response code="400">Validation failed (bad licenseId).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is SchoolAdmin of a different school.</response>
    /// <response code="404">Teaching category not found or doesn't belong to the specified school.</response>
    [HttpPut("update/{schoolId:int}/{teachingCategoryId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> UpdateTeachingCategory(int schoolId, int teachingCategoryId, [FromBody] TeachingCategoryUpdateDto dto)
    {
        // ─── caller must belong to this school ───
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users.AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.Id == callerId);

        if (caller?.AutoSchoolId != schoolId)
            return Forbid();

        // ─── find the teaching category ───
        var category = await _db.TeachingCategories
                                .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == teachingCategoryId && tc.AutoSchoolId == schoolId);

        if (category == null)
            return NotFound(new { message = "Teaching category not found or doesn't belong to the specified school." });

        // ─── basic validation ───
        if (dto.LicenseId <= 0 || dto.SessionDuration <= 0 || dto.MinDrivingLessonsReq < 0)
        {
            return BadRequest(new
            {
                message = "licenseId must be positive, sessionDuration must be positive, and minDrivingLessonsReq must be non-negative."
            });
        }

        // license must exist
        if (!await _db.Licenses.AnyAsync(l => l.LicenseId == dto.LicenseId))
            return BadRequest(new { message = "licenseId does not reference an existing license." });

        // ─── update ───
        category.LicenseId = dto.LicenseId;
        category.SessionCost = dto.SessionCost;
        category.SessionDuration = dto.SessionDuration;
        category.ScholarshipPrice = dto.ScholarshipPrice;
        category.MinDrivingLessonsReq = dto.MinDrivingLessonsReq;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Teaching category updated successfully" });
    }

    // ────────────────────────────── DELETE TEACHING CATEGORY ──────────────────────────────
    /// <summary>
    /// Deletes an existing teaching category (SchoolAdmin of the same school).
    /// </summary>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="teachingCategoryId">Teaching category identifier from the route.</param>
    /// <response code="204">Teaching category deleted successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is SchoolAdmin of a different school.</response>
    /// <response code="404">Teaching category not found or doesn't belong to the specified school.</response>
    [HttpDelete("delete/{schoolId:int}/{teachingCategoryId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> DeleteTeachingCategory(int schoolId, int teachingCategoryId)
    {
        // ─── caller must belong to this school ───
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users.AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.Id == callerId);

        if (caller?.AutoSchoolId != schoolId)
            return Forbid();

        // ─── find the teaching category ───
        var category = await _db.TeachingCategories
                                .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == teachingCategoryId && tc.AutoSchoolId == schoolId);

        if (category == null)
            return NotFound(new { message = "Teaching category not found or doesn't belong to the specified school." });

        // ─── delete ───
        _db.TeachingCategories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

// ─────────────────────── DTOs ───────────────────────

/// <summary>DTO for teaching category response.</summary>
public sealed class TeachingCategoryResponseDto
{
    /// <summary>Teaching category ID</summary>
    public int TeachingCategoryId { get; init; }
    
    /// <summary>License ID</summary>
    public int LicenseId { get; init; }
    
    /// <summary>License type (A, B, C, etc.)</summary>
    public string? LicenseType { get; init; }
    
    /// <summary>Cost per session</summary>
    public decimal SessionCost { get; init; }
    
    /// <summary>Duration of one session in minutes</summary>
    public int SessionDuration { get; init; }
    
    /// <summary>Total price of the scholarship pack</summary>
    public decimal ScholarshipPrice { get; init; }
    
    /// <summary>Minimum number of driving lessons required</summary>
    public int MinDrivingLessonsReq { get; init; }
}

/// <summary>DTO for creating a teaching category.</summary>
public sealed class TeachingCategoryCreateDto
{
    /// <summary>License ID</summary>
    public int LicenseId { get; init; }
    
    /// <summary>Cost per session</summary>
    public decimal SessionCost { get; init; }
    
    /// <summary>Duration of one session in minutes</summary>
    public int SessionDuration { get; init; }
    
    /// <summary>Total price of the scholarship pack</summary>
    public decimal ScholarshipPrice { get; init; }
    
    /// <summary>Minimum number of driving lessons required</summary>
    public int MinDrivingLessonsReq { get; init; }
}

/// <summary>DTO for updating a teaching category.</summary>
public sealed class TeachingCategoryUpdateDto
{
    /// <summary>License ID</summary>
    public int LicenseId { get; init; }
    
    /// <summary>Cost per session</summary>
    public decimal SessionCost { get; init; }
    
    /// <summary>Duration of one session in minutes</summary>
    public int SessionDuration { get; init; }
    
    /// <summary>Total price of the scholarship pack</summary>
    public decimal ScholarshipPrice { get; init; }
    
    /// <summary>Minimum number of driving lessons required</summary>
    public int MinDrivingLessonsReq { get; init; }
} 