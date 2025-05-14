using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using File = DriveFlow_CRM_API.Models.File;
using DriveFlow_CRM_API.Models.DTOs;

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
    /// Retrieves all files assigned to a specific student with associated instructor and license information.
    /// <para>
    /// <strong>Sample response format</strong>:
    /// </para>
    /// <code>
    /// [
    ///   {
    ///     "fileId": 1,
    ///     "status": "Pending",
    ///     "firstName": "John",
    ///     "lastName": "Doe",
    ///     "type": "B"
    ///   },
    ///   {
    ///     "fileId": 2,
    ///     "status": "Completed",
    ///     "firstName": "Jane",
    ///     "lastName": "Smith",
    ///     "type": "A"
    ///   }
    /// ]
    /// </code>
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
            .Include(f => f.Instructor)
            .Include(f => f.TeachingCategory)
                .ThenInclude(tc => tc.License)
            .Where(f => f.StudentId == studentId)
            .Select(f => new
            {
                f.FileId,
                Status = f.Status.ToString(),
                FirstName = f.Instructor != null ? f.Instructor.FirstName : null,
                LastName = f.Instructor != null ? f.Instructor.LastName : null,
#pragma warning disable CS8602 // Dereference of a possibly null reference
                Type = f.TeachingCategory != null ? 
                       (f.TeachingCategory.License != null ? f.TeachingCategory.License.Type : f.TeachingCategory.Code) : 
                       null
#pragma warning restore CS8602
            })
            .ToListAsync();

        // Convert to DTO
        var result = files.Select(f => new StudentFileDto
        {
            FileId = f.FileId,
            Status = f.Status,
            FirstName = f.FirstName,
            LastName = f.LastName,
            Type = f.Type
        }).ToList();

        return Ok(result);
    }
    
    /// <summary>
    /// Retrieves detailed information about a specific file including instructor, vehicle, payment, and appointment data.
    /// </summary>
    /// <remarks>
    /// Provides comprehensive information about a student's file including all related data necessary to track training progress.
    /// <para>
    /// <strong>Sample response format</strong>:
    /// </para>
    /// <code>
    /// {
    ///   "fileId": 201,
    ///   "status": "active",
    ///   "scholarshipStartDate": "2025-02-01",
    ///   "criminalRecordExpiryDate": "2025-12-01",
    ///   "medicalRecordExpiryDate": "2025-10-01",
    ///   "payment": {
    ///     "scholarshipPayment": true,
    ///     "sessionsPayed": 30
    ///   },
    ///   "instructor": {
    ///     "userId": "501",
    ///     "firstName": "Andrei",
    ///     "lastName": "Popescu",
    ///     "email": "andrei.popescu@school.ro",
    ///     "phone": "0723456789",
    ///     "role": "Instructor"
    ///   },
    ///   "vehicle": {
    ///     "licensePlateNumber": "B-123-XYZ",
    ///     "transmissionType": "MANUAL",
    ///     "color": "red",
    ///     "brand": "Dacia",
    ///     "model": "Logan",
    ///     "yearOfProduction": 2021,
    ///     "fuelType": "BENZINA",
    ///     "engineSizeLiters": 1.6,
    ///     "powertrainType": "COMBUSTIBIL",
    ///     "type": "B"
    ///   },
    ///   "appointments": [
    ///     {
    ///       "appointmentId": 701,
    ///       "date": "2025-03-01",
    ///       "startHour": "10:00",
    ///       "endHour": "12:00",
    ///       "status": "completed"
    ///     }
    ///   ],
    ///   "appointmentsCompleted": 1
    /// }
    /// </code>
    /// </remarks>
    /// <param name="fileId">The ID of the file to retrieve detailed information for</param>
    /// <response code="200">File details retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to access this file.</response>
    /// <response code="404">File with the specified ID was not found.</response>
    [HttpGet("file-details/{fileId}")]
    public async Task<ActionResult<FileDetailsDto>> GetStudentFileDetails(int fileId)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 2. Check if the file exists and belongs to the authenticated student
        var file = await _db.Files
            .FirstOrDefaultAsync(f => f.FileId == fileId);
            
        if (file == null)
        {
            return NotFound();
        }
            
        if (file.StudentId != userId)
        {
            return Forbid(); // Return 403 Forbidden if trying to access another student's file
        }

        // 3. Query the file with all required data
        var fileData = await (from f in _db.Files
                           where f.FileId == fileId
                           select new
                           {
                               File = f,
                               Payment = _db.Payments.FirstOrDefault(p => p.FileId == f.FileId),
                               Instructor = f.Instructor,
                               Vehicle = f.Vehicle,
                               TeachingCategory = f.TeachingCategory,
                               License = f.TeachingCategory != null ? f.TeachingCategory.License : null
                           }).FirstOrDefaultAsync();

        if (fileData == null)
        {
            return NotFound();
        }

        // Process appointments
        var appointments = await _db.Appointments
            .Where(a => a.FileId == fileId)
            .Select(a => new
            {
                a.AppointmentId,
                a.Date,
                StartHour = a.StartHour.ToString(@"hh\:mm"),
                EndHour = a.EndHour.ToString(@"hh\:mm"),
                Status = a.Date.Add(a.EndHour) < DateTime.Now ? "completed" : "pending"
            })
            .ToListAsync();

        // Convert to DTO
        var appointmentDtos = appointments.Select(a => new AppointmentDetailsDto
        {
            AppointmentId = a.AppointmentId,
            Date = a.Date,
            StartHour = a.StartHour,
            EndHour = a.EndHour,
            Status = a.Status
        }).ToList();

        // Count completed appointments
        int completedCount = appointmentDtos.Count(a => a.Status == "completed");

        // 4. Construct the response DTO
        var result = new FileDetailsDto
        {
            FileId = fileData.File.FileId,
            Status = fileData.File.Status.ToString().ToLower(),
            ScholarshipStartDate = fileData.File.ScholarshipStartDate,
            CriminalRecordExpiryDate = fileData.File.CriminalRecordExpiryDate,
            MedicalRecordExpiryDate = fileData.File.MedicalRecordExpiryDate,
            Payment = fileData.Payment != null ? new PaymentDetailsDto
            {
                ScholarshipPayment = fileData.Payment.ScholarshipBasePayment,
                SessionsPayed = fileData.Payment.SessionsPayed
            } : null,
            Instructor = fileData.Instructor != null ? new InstructorDetailsDto
            {
                UserId = fileData.Instructor.Id,
                FirstName = fileData.Instructor.FirstName,
                LastName = fileData.Instructor.LastName,
                Email = fileData.Instructor.Email,
                Phone = fileData.Instructor.PhoneNumber,
                Role = "Instructor"
            } : null,
            Vehicle = fileData.Vehicle != null ? new VehicleDetailsDto
            {
                LicensePlateNumber = fileData.Vehicle.LicensePlateNumber,
                TransmissionType = fileData.Vehicle.TransmissionType.ToString(),
                Color = fileData.Vehicle.Color,
                Brand = fileData.Vehicle.Brand,
                Model = fileData.Vehicle.Model,
                YearOfProduction = fileData.Vehicle.YearOfProduction,
                FuelType = fileData.Vehicle.FuelType.HasValue ? fileData.Vehicle.FuelType.ToString() : null,
                EngineSizeLiters = fileData.Vehicle.EngineSizeLiters,
                PowertrainType = fileData.Vehicle.PowertrainType.HasValue ? fileData.Vehicle.PowertrainType.ToString() : null,
                Type = fileData.License?.Type ?? fileData.TeachingCategory?.Code
            } : null,
            Appointments = appointmentDtos,
            AppointmentsCompleted = completedCount
        };

        return Ok(result);
    }
    
    /// <summary>
    /// Retrieves all future appointments for the authenticated student across all their files.
    /// </summary>
    /// <remarks>
    /// Returns a list of all upcoming appointments for the student with essential details including date, time, and license type.
    /// <para>
    /// <strong>Sample response format</strong>:
    /// </para>
    /// <code>
    /// [
    ///   {
    ///     "appointmentId": 701,
    ///     "date": "2025-03-01",
    ///     "startHour": "10:00",
    ///     "endHour": "12:00",
    ///     "fileId": 201,
    ///     "instructorName": "Andrei Popescu",
    ///     "licenseType": "B"
    ///   },
    ///   {
    ///     "appointmentId": 702,
    ///     "date": "2025-03-03",
    ///     "startHour": "14:00",
    ///     "endHour": "16:00",
    ///     "fileId": 201,
    ///     "instructorName": "Andrei Popescu",
    ///     "licenseType": "B"
    ///   }
    /// ]
    /// </code>
    /// </remarks>
    /// <response code="200">Future appointments retrieved successfully. Returns empty array if no appointments found.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("future-appointments")]
    public async Task<ActionResult<IEnumerable<StudentAppointmentDto>>> GetFutureAppointments()
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get current date/time
        var now = DateTime.Now;
        
        // 2. Query appointments from all student's files that are in the future
        var appointments = await (from a in _db.Appointments
                               join f in _db.Files on a.FileId equals f.FileId
                               join instructor in _db.ApplicationUsers on f.InstructorId equals instructor.Id into instructorJoin
                               from instructor in instructorJoin.DefaultIfEmpty()
                               join tc in _db.TeachingCategories on f.TeachingCategoryId equals tc.TeachingCategoryId into tcJoin
                               from tc in tcJoin.DefaultIfEmpty()
                               join license in _db.Licenses on tc.LicenseId equals license.LicenseId into licenseJoin
                               from license in licenseJoin.DefaultIfEmpty()
                               where f.StudentId == userId && 
                                     (a.Date > now.Date || 
                                     (a.Date == now.Date && a.StartHour > new TimeSpan(now.Hour, now.Minute, 0)))
                               orderby a.Date, a.StartHour
                               select new StudentAppointmentDto
                               {
                                   AppointmentId = a.AppointmentId,
                                   Date = a.Date,
                                   StartHour = a.StartHour.ToString(@"hh\:mm"),
                                   EndHour = a.EndHour.ToString(@"hh\:mm"),
                                   FileId = f.FileId,
                                   InstructorName = instructor != null ? $"{instructor.FirstName} {instructor.LastName}" : "N/A",
                                   LicenseType = license != null ? license.Type : tc != null ? tc.Code : "N/A"
                               }).ToListAsync();

        return Ok(appointments);
    }

    /// <summary>
    /// Retrieves all appointments (past and future) for the authenticated student across all their files.
    /// </summary>
    /// <remarks>
    /// Returns a comprehensive list of all appointments for the student with essential details and status.
    /// <para>
    /// <strong>Sample response format</strong>:
    /// </para>
    /// <code>
    /// [
    ///   {
    ///     "appointmentId": 701,
    ///     "date": "2025-03-01",
    ///     "startHour": "10:00",
    ///     "endHour": "12:00",
    ///     "fileId": 201,
    ///     "instructorName": "Andrei Popescu",
    ///     "licenseType": "B",
    ///     "status": "completed"
    ///   },
    ///   {
    ///     "appointmentId": 702,
    ///     "date": "2025-03-03",
    ///     "startHour": "14:00",
    ///     "endHour": "16:00",
    ///     "fileId": 201,
    ///     "instructorName": "Andrei Popescu",
    ///     "licenseType": "B",
    ///     "status": "pending"
    ///   }
    /// ]
    /// </code>
    /// </remarks>
    /// <response code="200">All appointments retrieved successfully. Returns empty array if no appointments found.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("all-appointments")]
    public async Task<ActionResult<IEnumerable<StudentAppointmentFullDto>>> GetAllAppointments()
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get current date/time for status determination
        var now = DateTime.Now;
        
        // 2. Query all appointments from all student's files (past and future)
        var appointments = await (from a in _db.Appointments
                               join f in _db.Files on a.FileId equals f.FileId
                               join instructor in _db.ApplicationUsers on f.InstructorId equals instructor.Id into instructorJoin
                               from instructor in instructorJoin.DefaultIfEmpty()
                               join tc in _db.TeachingCategories on f.TeachingCategoryId equals tc.TeachingCategoryId into tcJoin
                               from tc in tcJoin.DefaultIfEmpty()
                               join license in _db.Licenses on tc.LicenseId equals license.LicenseId into licenseJoin
                               from license in licenseJoin.DefaultIfEmpty()
                               where f.StudentId == userId
                               orderby a.Date, a.StartHour
                               select new 
                               {
                                   AppointmentId = a.AppointmentId,
                                   Date = a.Date,
                                   StartHour = a.StartHour,
                                   EndHour = a.EndHour,
                                   FileId = f.FileId,
                                   InstructorName = instructor != null ? $"{instructor.FirstName} {instructor.LastName}" : "N/A",
                                   LicenseType = license != null ? license.Type : tc != null ? tc.Code : "N/A"
                               }).ToListAsync();

        // Convert to DTO with status
        var result = appointments.Select(a => new StudentAppointmentFullDto
        {
            AppointmentId = a.AppointmentId,
            Date = a.Date,
            StartHour = a.StartHour.ToString(@"hh\:mm"),
            EndHour = a.EndHour.ToString(@"hh\:mm"),
            FileId = a.FileId,
            InstructorName = a.InstructorName,
            LicenseType = a.LicenseType,
            Status = a.Date.Add(a.EndHour) < now ? "completed" : "pending"
        }).ToList();

        return Ok(result);
    }
}

// ─────────────────────── DTOs ───────────────────────

/// <summary>
/// DTO representing a student's file with associated instructor and license information.
/// </summary>
public sealed class StudentFileDto
{
    /// <summary>
    /// Unique identifier of the file.
    /// </summary>
    public int FileId { get; init; }
    
    /// <summary>
    /// Current status of the file.
    /// </summary>
    public string Status { get; init; } = default!;
    
    /// <summary>
    /// First name of the assigned instructor.
    /// </summary>
    public string? FirstName { get; init; }
    
    /// <summary>
    /// Last name of the assigned instructor.
    /// </summary>
    public string? LastName { get; init; }
    
    /// <summary>
    /// Type of the associated license.
    /// </summary>
    public string? Type { get; init; }
}

/// <summary>
/// DTO representing a student's future appointment.
/// </summary>
public sealed class StudentAppointmentDto
{
    /// <summary>
    /// Unique identifier of the appointment.
    /// </summary>
    public int AppointmentId { get; init; }
    
    /// <summary>
    /// Date of the appointment.
    /// </summary>
    public DateTime Date { get; init; }
    
    /// <summary>
    /// Start hour of the appointment in HH:MM format.
    /// </summary>
    public string StartHour { get; init; } = default!;
    
    /// <summary>
    /// End hour of the appointment in HH:MM format.
    /// </summary>
    public string EndHour { get; init; } = default!;
    
    /// <summary>
    /// ID of the file associated with this appointment.
    /// </summary>
    public int FileId { get; init; }
    
    /// <summary>
    /// Full name of the instructor.
    /// </summary>
    public string InstructorName { get; init; } = default!;
    
    /// <summary>
    /// License type associated with this appointment.
    /// </summary>
    public string LicenseType { get; init; } = default!;
}

/// <summary>
/// DTO representing a student's appointment with status information.
/// </summary>
public sealed class StudentAppointmentFullDto
{
    /// <summary>
    /// Unique identifier of the appointment.
    /// </summary>
    public int AppointmentId { get; init; }
    
    /// <summary>
    /// Date of the appointment.
    /// </summary>
    public DateTime Date { get; init; }
    
    /// <summary>
    /// Start hour of the appointment in HH:MM format.
    /// </summary>
    public string StartHour { get; init; } = default!;
    
    /// <summary>
    /// End hour of the appointment in HH:MM format.
    /// </summary>
    public string EndHour { get; init; } = default!;
    
    /// <summary>
    /// ID of the file associated with this appointment.
    /// </summary>
    public int FileId { get; init; }
    
    /// <summary>
    /// Full name of the instructor.
    /// </summary>
    public string InstructorName { get; init; } = default!;
    
    /// <summary>
    /// License type associated with this appointment.
    /// </summary>
    public string LicenseType { get; init; } = default!;
    
    /// <summary>
    /// Status of the appointment (completed or pending).
    /// </summary>
    public string Status { get; init; } = default!;
} 