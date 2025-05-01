using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// Student-specific endpoints for the DriveFlow CRM API.
/// </summary>
/// <remarks>
/// Exposes endpoints for students to view their files and track learning progress.
/// All endpoints require authentication and are restricted to users with the Student role.
/// </remarks>
[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public class StudentController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    /// <summary>
    /// Constructor injected by the framework with request‑scoped services.
    /// </summary>
    public StudentController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────   FILES   ─────────────────────────
    /// <summary>
    /// Retrieves all files assigned to a specific student with associated instructor and license information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns a list of files with:
    /// <list type="bullet">
    ///   <item><description>File status and ID</description></item>
    ///   <item><description>Assigned instructor's name</description></item>
    ///   <item><description>Associated license type</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="studentId">The ID of the student whose files to retrieve</param>
    /// <response code="200">Files retrieved successfully. Returns empty array if no files found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to access these files.</response>
    [HttpGet("{studentId}/files")]
    public async Task<ActionResult<IEnumerable<StudentFileDto>>> GetStudentFiles(string studentId)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 2. Verify the authenticated user is the same as the requested studentId
        if (userId != studentId)
        {
            return Forbid(); // Return 403 Forbidden if trying to access another student's data
        }

        // 3. Query files with required joins and projection
        var files = await _db.Files
            .Where(f => f.StudentId == studentId)
            .Select(f => new 
            {
                FileId = f.FileId,
                Status = f.Status,
                FirstName = f.Instructor.FirstName,
                LastName = f.Instructor.LastName,
                Type = f.Vehicle.License.Type
            })
            .ToListAsync();

        // 4. Convert to DTO with string enum values
        var dtos = files.Select(f => new StudentFileDto(
            FileId: f.FileId,
            Status: f.Status.ToString(),
            FirstName: f.FirstName,
            LastName: f.LastName,
            Type: f.Type
        ));

        return Ok(dtos);
    }
}

/// <summary>
/// DTO representing a student's file with associated instructor and license information.
/// </summary>
/// <param name="FileId">Unique identifier of the file.</param>
/// <param name="Status">Current status of the file.</param>
/// <param name="FirstName">First name of the assigned instructor.</param>
/// <param name="LastName">Last name of the assigned instructor.</param>
/// <param name="Type">Type of the associated license.</param>
public record StudentFileDto(
    int FileId,
    string Status,
    string FirstName,
    string LastName,
    string Type
); 