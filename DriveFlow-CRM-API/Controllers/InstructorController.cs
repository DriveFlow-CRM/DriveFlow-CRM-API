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
    ///     "transmissionType": "MANUAL",
    ///     "status": "ARCHIVED",
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
    ///     "transmissionType": "AUTOMATIC",
    ///     "status": "ARCHIVED",
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

        // 3. Query files with required joins
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var files = await _db.Files
            .Where(f => f.InstructorId == instructorId)
            .Include(f => f.Student)
            .Include(f => f.Vehicle)
            .Include(f => f.TeachingCategory)
                .ThenInclude(tc => tc.License)
            .AsNoTracking()
            .ToListAsync();
#pragma warning restore CS8602

        // 4. Map to DTOs after materializing the query, with additional null checks
        var result = files.Select(f => 
        {
            var student = f.Student; // Avoid multiple property access that could trigger warning
            var vehicle = f.Vehicle; // Avoid multiple property access that could trigger warning
            var teachingCategory = f.TeachingCategory; // Avoid multiple property access
            
            return new InstructorAssignedFileDto
            {
                FirstName = student?.FirstName,
                LastName = student?.LastName,
                PhoneNumber = student?.PhoneNumber,
                Email = student?.Email,
                ScholarshipStartDate = f.ScholarshipStartDate?.Date,
                LicensePlateNumber = vehicle?.LicensePlateNumber,
                TransmissionType = vehicle != null ? vehicle.TransmissionType.ToString() : null,
                Status = f.Status.ToString(),
                Type = teachingCategory?.License?.Type ?? teachingCategory?.Code,
                Color = vehicle?.Color
            };
        }).ToList();

        return Ok(result);
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
    ///   "status": "APPROVED",
    ///   "scholarshipPayment": true,
    ///   "sessionsPayed": 30,
    ///   "minDrivingLessonsRequired": 30,
    ///   "lessonsMade": [
    ///     "2025-03-01",
    ///     "2025-03-05",
    ///     "2025-03-12"
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
            FirstName = file.Student?.FirstName,
            LastName = file.Student?.LastName,
            Email = file.Student?.Email,
            PhoneNo = file.Student?.PhoneNumber,
            ScholarshipStartDate = file.ScholarshipStartDate?.Date,
            CriminalRecordExpiryDate = file.CriminalRecordExpiryDate?.Date,
            MedicalRecordExpiryDate = file.MedicalRecordExpiryDate?.Date,
            Status = file.Status.ToString(),
            ScholarshipPayment = payment != null && payment.ScholarshipBasePayment,
            SessionsPayed = payment != null ? payment.SessionsPayed : 0,
            MinDrivingLessonsRequired = file.TeachingCategory?.MinDrivingLessonsReq ?? 0,
            LessonsMade = file.Appointments
                .Where(a => a.Date.Add(a.EndHour) <= now)
                .OrderBy(a => a.Date)
                .Select(a => a.Date.Add(a.StartHour).Date)
                .ToList()
        };

        return Ok(fileDetails);
    }

    /// <summary>
    /// Retrieves all future appointments for an instructor filtered by date range.
    /// </summary>
    /// <remarks>
    /// <para>Returns a list of appointments with student and vehicle details.</para>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "appointmentId": 5012,
    ///     "date": "2025-05-15",
    ///     "startHour": "09:00",
    ///     "endHour": "11:00",
    ///     "fileId": 918,
    ///     "firstName": "Maria",
    ///     "lastName": "Ionescu",
    ///     "phoneNo": "+40 712 345 678",
    ///     "licensePlateNumber": "B‑12‑XYZ",
    ///     "type": "B"
    ///   },
    ///   {
    ///     "appointmentId": 5013,
    ///     "date": "2025-05-16",
    ///     "startHour": "14:00",
    ///     "endHour": "16:00",
    ///     "fileId": 922,
    ///     "firstName": "Andrei",
    ///     "lastName": "Pop",
    ///     "phoneNo": "+40 745 987 654",
    ///     "licensePlateNumber": "CJ‑34‑ABC",
    ///     "type": "BE"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="instructorId">The ID of the instructor whose appointments to retrieve</param>
    /// <param name="startDate">Start date for filtering appointments (inclusive)</param>
    /// <param name="endDate">End date for filtering appointments (inclusive)</param>
    /// <response code="200">Appointments retrieved successfully. Returns empty array if no appointments found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to access these appointments.</response>
    [HttpGet("{instructorId}/fetchInstructorAppointments/{startDate}/{endDate}")]
    [Authorize(Roles = "Instructor,SchoolAdmin")]
    public async Task<ActionResult<IEnumerable<InstructorAppointmentDto>>> FetchInstructorAppointments(string instructorId, DateTime startDate, DateTime endDate)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 2. Check if caller is Instructor role and verify access rights if so
        var isCallerInstructor = User.IsInRole("Instructor");
        if (isCallerInstructor && userId != instructorId)
        {
            return Forbid(); // Return 403 Forbidden if instructor trying to access another instructor's data
        }

        // 3. Query appointments with required joins
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var query = from instructor in _db.ApplicationUsers
                    where instructor.Id == instructorId
                    join file in _db.Files
                        .Include(f => f.Student)
                        .Include(f => f.Vehicle)
                        .Include(f => f.TeachingCategory)
                            .ThenInclude(tc => tc.License)
                        on instructor.Id equals file.InstructorId
                    join appointment in _db.Appointments
                        on file.FileId equals appointment.FileId
                    where appointment.Date >= startDate && appointment.Date <= endDate
                    orderby appointment.Date, appointment.StartHour
                    select new { appointment, file };

        // 4. Execute the query and materialize the results
        var results = await query.AsNoTracking().ToListAsync();
#pragma warning restore CS8602

        // 5. Map to DTOs safely
        var appointments = results.Select(r => 
        {
            var student = r.file.Student; // Avoid multiple property access that could trigger warning
            var vehicle = r.file.Vehicle; // Avoid multiple property access that could trigger warning
            var teachingCategory = r.file.TeachingCategory; // Avoid multiple property access
            
            return new InstructorAppointmentDto
            {
                AppointmentId = r.appointment.AppointmentId,
                Date = r.appointment.Date.Date,
                StartHour = r.appointment.StartHour.ToString(@"hh\:mm"),
                EndHour = r.appointment.EndHour.ToString(@"hh\:mm"),
                FileId = r.file.FileId,
                FirstName = student?.FirstName,
                LastName = student?.LastName,
                PhoneNo = student?.PhoneNumber,
                LicensePlateNumber = vehicle?.LicensePlateNumber,
                Type = teachingCategory?.License?.Type ?? teachingCategory?.Code
            };
        }).ToList();

        return Ok(appointments);
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
    
    /// <summary>Expiry date for criminal record</summary>
    public DateTime? CriminalRecordExpiryDate { get; init; }
    
    /// <summary>Expiry date for medical certificate</summary>
    public DateTime? MedicalRecordExpiryDate { get; init; }
    
    /// <summary>File status</summary>
    public string Status { get; init; } = null!;
    
    /// <summary>Whether scholarship payment is complete</summary>
    public bool ScholarshipPayment { get; init; }
    
    /// <summary>Number of sessions paid for</summary>
    public int SessionsPayed { get; init; }
    
    /// <summary>Minimum required driving lessons</summary>
    public int MinDrivingLessonsRequired { get; init; }
    
    /// <summary>Dates of completed lessons</summary>
    public List<DateTime> LessonsMade { get; init; } = new List<DateTime>();
}

/// <summary>
/// DTO for instructor appointment information
/// </summary>
public sealed class InstructorAppointmentDto
{
    /// <summary>Appointment identifier</summary>
    public int AppointmentId { get; init; }
    
    /// <summary>Date of the appointment</summary>
    public DateTime Date { get; init; }
    
    /// <summary>Start time of the appointment</summary>
    public string StartHour { get; init; } = null!;
    
    /// <summary>End time of the appointment</summary>
    public string EndHour { get; init; } = null!;
    
    /// <summary>Associated file identifier</summary>
    public int FileId { get; init; }
    
    /// <summary>Student's first name</summary>
    public string? FirstName { get; init; }
    
    /// <summary>Student's last name</summary>
    public string? LastName { get; init; }
    
    /// <summary>Student's phone number</summary>
    public string? PhoneNo { get; init; }
    
    /// <summary>Vehicle license plate number</summary>
    public string? LicensePlateNumber { get; init; }
    
    /// <summary>License type</summary>
    public string? Type { get; init; }
} 