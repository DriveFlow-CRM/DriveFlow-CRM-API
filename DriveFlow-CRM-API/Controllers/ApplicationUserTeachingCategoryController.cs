using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;

namespace DriveFlow_CRM_API.Controllers;


[ApiController]
[Route("api/autoschool/{schoolId:int}/instructorCategories")]
[Authorize(Roles = "SchoolAdmin,SuperAdmin")]
public class ApplicationUserTeachingCategoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ApplicationUserTeachingCategoryController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ────────────────────────────── GET INSTRUCTOR TEACHING CATEGORIES ──────────────────────────────
    /// <summary>
    /// Returns all teaching categories linked to a specific instructor
    /// (SuperAdmin for any school; SchoolAdmin only for their own school – otherwise 403).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "teachingCategoryId": 10,
    ///     "code": "B1",
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
    /// <param name="instructorId">Instructor identifier from the route.</param>
    /// <response code="200">List returned successfully (can be empty).</response>
    /// <response code="400"><paramref name="schoolId"/> is not positive or instructor doesn't exist.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is a <c>SchoolAdmin</c> of another school or instructor belongs to another school.</response>
    /// <response code="404">Instructor not found or not an instructor.</response>
    [HttpGet("instructor/{instructorId}/teachingCategories")]
    public async Task<IActionResult> GetInstructorTeachingCategories(int schoolId, string instructorId)
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

        // Check if instructor exists and belongs to the specified school
        var instructor = await _users.Users
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(u => u.Id == instructorId);

        if (instructor == null)
            return NotFound(new { message = "Instructor not found." });

        // Check if user is an instructor
        var isInstructor = await _users.IsInRoleAsync(instructor, "Instructor");
        if (!isInstructor)
            return BadRequest(new { message = "The specified user is not an instructor." });

        // Check if instructor belongs to the specified school
        if (instructor.AutoSchoolId != schoolId)
            return Forbid();

        // Query and project teaching categories with license type
        var categories = await _db.ApplicationUserTeachingCategories
                                .AsNoTracking()
                                .Where(autc => autc.UserId == instructorId)
                                .Include(autc => autc.TeachingCategory)
                                    .ThenInclude(tc => tc.License)
                                .Where(autc => autc.TeachingCategory.AutoSchoolId == schoolId)
                                .Select(autc => new InstructorTeachingCategoryResponseDto
                                {
                                    TeachingCategoryId = autc.TeachingCategoryId,
                                    Code = autc.TeachingCategory.Code,
                                    LicenseId = autc.TeachingCategory.LicenseId ?? 0,
                                    LicenseType = autc.TeachingCategory.License != null ? autc.TeachingCategory.License.Type : null,
                                    SessionCost = autc.TeachingCategory.SessionCost,
                                    SessionDuration = autc.TeachingCategory.SessionDuration,
                                    ScholarshipPrice = autc.TeachingCategory.ScholarshipPrice,
                                    MinDrivingLessonsReq = autc.TeachingCategory.MinDrivingLessonsReq
                                })
                                .ToListAsync();

        return Ok(categories);
    }

    // ────────────────────────────── GET TEACHING CATEGORY INSTRUCTORS ──────────────────────────────
    /// <summary>
    /// Returns all instructors linked to a specific teaching category
    /// (SuperAdmin for any school; SchoolAdmin only for their own school – otherwise 403).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "instructorId": "abc123",
    ///     "firstName": "John",
    ///     "lastName": "Doe",
    ///     "email": "john.doe@example.com",
    ///     "phoneNumber": "0712345678"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="teachingCategoryId">Teaching category identifier from the route.</param>
    /// <response code="200">List returned successfully (can be empty).</response>
    /// <response code="400"><paramref name="schoolId"/> is not positive or teaching category doesn't exist.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is a <c>SchoolAdmin</c> of another school or teaching category belongs to another school.</response>
    /// <response code="404">Teaching category not found.</response>
    [HttpGet("teachingCategory/{teachingCategoryId:int}/instructors")]
    public async Task<IActionResult> GetTeachingCategoryInstructors(int schoolId, int teachingCategoryId)
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

        // Check if teaching category exists and belongs to the specified school
        var teachingCategory = await _db.TeachingCategories
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == teachingCategoryId);

        if (teachingCategory == null)
            return NotFound(new { message = "Teaching category not found." });

        // Check if teaching category belongs to the specified school
        if (teachingCategory.AutoSchoolId != schoolId)
            return Forbid();

        // Get all users in instructor role first
        var instructorsInRole = await _users.GetUsersInRoleAsync("Instructor");
        var instructorIds = instructorsInRole.Select(i => i.Id).ToHashSet();

        // Query and project instructors
        var instructors = await _db.ApplicationUserTeachingCategories
                                .AsNoTracking()
                                .Where(autc => autc.TeachingCategoryId == teachingCategoryId)
                                .Include(autc => autc.User)
                                .Where(autc => autc.User.AutoSchoolId == schoolId)
                                .Select(autc => new 
                                {
                                    UserId = autc.UserId,
                                    FirstName = autc.User.FirstName,
                                    LastName = autc.User.LastName,
                                    Email = autc.User.Email,
                                    PhoneNumber = autc.User.PhoneNumber
                                })
                                .ToListAsync();

        // Filter for only instructors in memory
        var result = instructors
                    .Where(u => instructorIds.Contains(u.UserId))
                    .Select(u => new TeachingCategoryInstructorResponseDto
                    {
                        InstructorId = u.UserId,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber
                    })
                    .ToList();

        return Ok(result);
    }

    // ────────────────────────────── POST INSTRUCTOR TEACHING CATEGORY ──────────────────────────────
    /// <summary>
    /// Links an instructor to a teaching category
    /// (SchoolAdmin only, same school).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// {
    ///   "instructorId": "abc123",
    ///   "teachingCategoryId": 10
    /// }
    /// ```
    /// </remarks>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="dto">Link data.</param>
    /// <response code="201">Link created successfully.</response>
    /// <response code="400">Validation failed (invalid instructorId or teachingCategoryId).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is SchoolAdmin of a different school.</response>
    /// <response code="409">Link already exists.</response>
    [HttpPost("link")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> LinkInstructorToTeachingCategory(int schoolId, [FromBody] InstructorTeachingCategoryLinkDto dto)
    {
        // Identify caller (id & associated school)
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users
                                   .AsNoTracking()
                                   .Select(u => new { u.Id, u.AutoSchoolId })
                                   .FirstOrDefaultAsync(u => u.Id == callerId);

        // A SchoolAdmin may access only their own school
        if (caller?.AutoSchoolId != schoolId)
            return Forbid();

        // Check if instructor exists and belongs to the specified school
        var instructor = await _users.Users
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(u => u.Id == dto.InstructorId);

        if (instructor == null)
            return BadRequest(new { message = "Instructor not found." });

        // Check if user is an instructor
        var isInstructor = await _users.IsInRoleAsync(instructor, "Instructor");
        if (!isInstructor)
            return BadRequest(new { message = "The specified user is not an instructor." });

        // Check if instructor belongs to the specified school
        if (instructor.AutoSchoolId != schoolId)
            return BadRequest(new { message = "The instructor does not belong to the specified school." });

        // Check if teaching category exists and belongs to the specified school
        var teachingCategory = await _db.TeachingCategories
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == dto.TeachingCategoryId);

        if (teachingCategory == null)
            return BadRequest(new { message = "Teaching category not found." });

        // Check if teaching category belongs to the specified school
        if (teachingCategory.AutoSchoolId != schoolId)
            return Forbid();

        // Check if link already exists
        var existingLink = await _db.ApplicationUserTeachingCategories
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(autc => autc.UserId == dto.InstructorId && 
                                                             autc.TeachingCategoryId == dto.TeachingCategoryId);

        if (existingLink != null)
            return Conflict(new { message = "The instructor is already linked to this teaching category." });

        // Create link
        var link = new ApplicationUserTeachingCategory
        {
            UserId = dto.InstructorId,
            TeachingCategoryId = dto.TeachingCategoryId
        };

        _db.ApplicationUserTeachingCategories.Add(link);
        await _db.SaveChangesAsync();

        return Created(
            $"/api/autoschool/{schoolId}/instructorCategories/instructor/{dto.InstructorId}/teachingCategories",
            new { applicationUserTeachingCategoryId = link.ApplicationUserTeachingCategoryId, message = "Instructor linked to teaching category successfully" });
    }

    // ────────────────────────────── DELETE INSTRUCTOR TEACHING CATEGORY ──────────────────────────────
    /// <summary>
    /// Removes a link between an instructor and a teaching category
    /// (SchoolAdmin only, same school).
    /// </summary>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="instructorId">Instructor identifier from the route.</param>
    /// <param name="teachingCategoryId">Teaching category identifier from the route.</param>
    /// <response code="200">Link deleted successfully.</response>
    /// <response code="400">Invalid instructorId or teachingCategoryId.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is SchoolAdmin of a different school.</response>
    /// <response code="404">Link not found.</response>
    [HttpDelete("unlink/instructor/{instructorId}/teachingCategory/{teachingCategoryId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> UnlinkInstructorFromTeachingCategory(int schoolId, string instructorId, int teachingCategoryId)
    {
        // Identify caller (id & associated school)
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users
                                   .AsNoTracking()
                                   .Select(u => new { u.Id, u.AutoSchoolId })
                                   .FirstOrDefaultAsync(u => u.Id == callerId);

        // A SchoolAdmin may access only their own school
        if (caller?.AutoSchoolId != schoolId)
            return Forbid();

        // Check if instructor exists and belongs to the specified school
        var instructor = await _users.Users
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(u => u.Id == instructorId);

        if (instructor == null)
            return BadRequest(new { message = "Instructor not found." });

        // Check if user is an instructor
        var isInstructor = await _users.IsInRoleAsync(instructor, "Instructor");
        if (!isInstructor)
            return BadRequest(new { message = "The specified user is not an instructor." });

        // Check if instructor belongs to the specified school
        if (instructor.AutoSchoolId != schoolId)
            return Forbid();

        // Check if teaching category exists and belongs to the specified school
        var teachingCategory = await _db.TeachingCategories
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == teachingCategoryId);

        if (teachingCategory == null)
            return BadRequest(new { message = "Teaching category not found." });

        // Check if teaching category belongs to the specified school
        if (teachingCategory.AutoSchoolId != schoolId)
            return Forbid();

        // Find link
        var link = await _db.ApplicationUserTeachingCategories
                          .FirstOrDefaultAsync(autc => autc.UserId == instructorId && 
                                                     autc.TeachingCategoryId == teachingCategoryId);

        if (link == null)
            return NotFound(new { message = "Link between instructor and teaching category not found." });

        // Delete link
        _db.ApplicationUserTeachingCategories.Remove(link);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Instructor unlinked from teaching category successfully" });
    }
} 

public sealed class InstructorTeachingCategoryResponseDto
{
    public int TeachingCategoryId { get; init; }
    public string Code { get; init; } = null!;
    public int LicenseId { get; init; }
    public string? LicenseType { get; init; }
    public decimal SessionCost { get; init; }
    public int SessionDuration { get; init; }
    public decimal ScholarshipPrice { get; init; }
    public int MinDrivingLessonsReq { get; init; }
}

public sealed class TeachingCategoryInstructorResponseDto
{
    public string InstructorId { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}

public sealed class InstructorTeachingCategoryLinkDto
{
    public string InstructorId { get; init; } = null!;
    public int TeachingCategoryId { get; init; }
}
