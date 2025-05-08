using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using File = DriveFlow_CRM_API.Models.File;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// Instructor-specific endpoints for the DriveFlow CRM API.
/// </summary>
/// <remarks>
/// Exposes endpoints for instructors to manage their files and track student progress.
/// All endpoints require authentication and are restricted to users with the Instructor role.
/// </remarks>
[ApiController]
[Route("api/instructor")]
[Authorize(Roles = "Instructor")]
public class InstructorController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    /// <summary>
    /// Constructor injected by the framework with request‑scoped services.
    /// </summary>
    public InstructorController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves all files assigned to a specific instructor with student and vehicle details.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "firstName": "Maria",
    ///     "lastName": "Ionescu",
    ///     "phoneNumber": "+40 712 345 678",
    ///     "email": "maria.ionescu@email.com",
    ///     "scholarshipStartDate": "2025-02-01",
    ///     "licensePlateNumber": "B‑12‑XYZ",
    ///     "transmissionType": "manual",
    ///     "status": "archived",
    ///     "type": "B",
    ///     "color": "red"
    ///   },
    ///   {
    ///     "firstName": "Andrei",
    ///     "lastName": "Pop",
    ///     "phoneNumber": "+40 745 987 654",
    ///     "email": "andrei.pop@example.com",
    ///     "scholarshipStartDate": "2025-03-10",
    ///     "licensePlateNumber": "CJ‑34‑ABC",
    ///     "transmissionType": "automatic",
    ///     "status": "archived",
    ///     "type": "BE",
    ///     "color": "blue"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="instructorId">The ID of the instructor whose assigned files to retrieve</param>
    /// <response code="200">Files retrieved successfully. Returns empty array if no files found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to access these files.</response>
    [HttpGet("{instructorId}/fetchInstructorAssignedFiles")]
    public async Task<ActionResult<IEnumerable<InstructorAssignedFileDto>>> FetchInstructorAssignedFiles(string instructorId)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 2. Verify the authenticated user is the same as the requested instructorId
        if (userId != instructorId)
        {
            return Forbid(); // Return 403 Forbidden if trying to access another instructor's data
        }

        // 3. Query files with required joins and projection
        var files = await _db.Files
            .Where(f => f.InstructorId == instructorId)
            .Include(f => f.Student)
            .Include(f => f.Vehicle)
                .ThenInclude(v => v.License)
            .Select(f => new InstructorAssignedFileDto
            {
                FirstName = f.Student.FirstName,
                LastName = f.Student.LastName,
                PhoneNumber = f.Student.PhoneNumber,
                Email = f.Student.Email,
                ScholarshipStartDate = f.ScholarshipStartDate,
                LicensePlateNumber = f.Vehicle != null ? f.Vehicle.LicensePlateNumber : null,
                TransmissionType = f.Vehicle != null ? f.Vehicle.TransmissionType.ToString().ToLowerInvariant() : null,
                Status = f.Status.ToString().ToLowerInvariant(),
                Type = f.Vehicle != null && f.Vehicle.License != null ? f.Vehicle.License.Type : null,
                Color = f.Vehicle != null ? f.Vehicle.Color : null
            })
            .ToListAsync();

        return Ok(files);
    }

    /// <summary>
    /// Retrieves detailed information about a specific file assigned to the authenticated instructor.
    /// </summary>
    /// <remarks>
    /// <para>Returns comprehensive file details including student information, file status, payment details, and lesson history.</para>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// {
    ///   "firstName": "Maria",
    ///   "lastName": "Ionescu",
    ///   "email": "maria.ionescu@email.com",
    ///   "phoneNo": "+40 712 345 678",
    ///   "scholarshipStartDate": "2025-02-01",
    ///   "criminalRecordExpiryDate": "2026-02-01",
    ///   "medicalRecordExpiryDate": "2025-08-01",
    ///   "status": "active",
    ///   "scholarshipPayment": true,
    ///   "sessionsPayed": 30,
    ///   "minDrivingLessonsRequired": 30,
    ///   "lessonsMade": [
    ///     "2025-03-01T09:00:00",
    ///     "2025-03-05T14:00:00",
    ///     "2025-03-12T12:00:00"
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="fileId">The ID of the file to retrieve details for</param>
    /// <response code="200">File details retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">File not found or not assigned to the authenticated instructor.</response>
    [HttpGet("fetchFileDetails/{fileId:int}")]
    public async Task<ActionResult<InstructorFileDetailsDto>> FetchFileDetails(int fileId)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 2. Get current date and time for lessons filtering
        var now = DateTime.Now;

        // 3. Query file with all necessary data
        var file = await _db.Files
            .Where(f => f.FileId == fileId && f.InstructorId == userId)
            .Include(f => f.Student)
            .Include(f => f.TeachingCategory)
            .Include(f => f.Appointments)
            .FirstOrDefaultAsync();

        if (file == null)
        {
            return NotFound();
        }

        // 4. Get payment information separately
        var payment = await _db.Payments
            .Where(p => p.FileId == fileId)
            .FirstOrDefaultAsync();

        // 5. Create the DTO with all collected data
        var fileDetails = new InstructorFileDetailsDto
        {
            FirstName = file.Student.FirstName,
            LastName = file.Student.LastName,
            Email = file.Student.Email,
            PhoneNo = file.Student.PhoneNumber,
            ScholarshipStartDate = file.ScholarshipStartDate,
            CriminalRecordExpiryDate = file.CriminalRecordExpiryDate,
            MedicalRecordExpiryDate = file.MedicalRecordExpiryDate,
            Status = file.Status.ToString().ToLowerInvariant(),
            ScholarshipPayment = payment != null && payment.ScholarshipBasePayment,
            SessionsPayed = payment != null ? payment.SessionsPayed : 0,
            MinDrivingLessonsRequired = file.TeachingCategory != null ? file.TeachingCategory.MinDrivingLessonsReq : 0,
            LessonsMade = file.Appointments
                .Where(a => a.Date.Add(a.EndHour) <= now)
                .OrderBy(a => a.Date)
                .Select(a => a.Date.Add(a.StartHour))
                .ToList()
        };

        return Ok(fileDetails);
    }
}

/// <summary>
/// DTO for instructor assigned file information
/// </summary>
public sealed class InstructorAssignedFileDto
{
    /// <summary>Student's first name</summary>
    public string? FirstName { get; init; }
    
    /// <summary>Student's last name</summary>
    public string? LastName { get; init; }
    
    /// <summary>Student's phone number</summary>
    public string? PhoneNumber { get; init; }
    
    /// <summary>Student's email address</summary>
    public string? Email { get; init; }
    
    /// <summary>Date when scholarship starts</summary>
    public DateTime? ScholarshipStartDate { get; init; }
    
    /// <summary>Vehicle license plate number</summary>
    public string? LicensePlateNumber { get; init; }
    
    /// <summary>Vehicle transmission type (manual/automatic)</summary>
    public string? TransmissionType { get; init; }
    
    /// <summary>File status</summary>
    public string Status { get; init; } = null!;
    
    /// <summary>License type</summary>
    public string? Type { get; init; }
    
    /// <summary>Vehicle color</summary>
    public string? Color { get; init; }
}

/// <summary>
/// DTO for detailed file information retrieved by instructors
/// </summary>
public sealed class InstructorFileDetailsDto
{
    /// <summary>Student's first name</summary>
    public string? FirstName { get; init; }
    
    /// <summary>Student's last name</summary>
    public string? LastName { get; init; }
    
    /// <summary>Student's email address</summary>
    public string? Email { get; init; }
    
    /// <summary>Student's phone number</summary>
    public string? PhoneNo { get; init; }
    
    /// <summary>Date when scholarship starts</summary>
    public DateTime? ScholarshipStartDate { get; init; }
    
    /// <summary>Criminal record expiry date</summary>
    public DateTime? CriminalRecordExpiryDate { get; init; }
    
    /// <summary>Medical record expiry date</summary>
    public DateTime? MedicalRecordExpiryDate { get; init; }
    
    /// <summary>File status</summary>
    public string Status { get; init; } = null!;
    
    /// <summary>Whether scholarship payment has been made</summary>
    public bool ScholarshipPayment { get; init; }
    
    /// <summary>Number of sessions paid for</summary>
    public int SessionsPayed { get; init; }
    
    /// <summary>Minimum required driving lessons</summary>
    public int MinDrivingLessonsRequired { get; init; }
    
    /// <summary>List of completed lesson dates</summary>
    public List<DateTime> LessonsMade { get; init; } = new List<DateTime>();
} 