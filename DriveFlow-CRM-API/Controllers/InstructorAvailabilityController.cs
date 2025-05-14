using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// Controller for managing instructor availability
/// </summary>
[ApiController]
[Route("api/instructor-availability")]
public class InstructorAvailabilityController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    public InstructorAvailabilityController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _users = userManager;
    }

    /// <summary>
    /// Retrieves all availability intervals for a specific instructor
    /// </summary>
    /// <remarks>
    /// <para>Returns all future time slots when an instructor is available for lessons.</para>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "intervalId": 101,
    ///     "date": "2023-11-20",
    ///     "startHour": "09:00",
    ///     "endHour": "12:00"
    ///   },
    ///   {
    ///     "intervalId": 102,
    ///     "date": "2023-11-21",
    ///     "startHour": "14:00",
    ///     "endHour": "18:00"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="instructorId">The ID of the instructor whose availability to retrieve</param>
    /// <response code="200">Availability intervals retrieved successfully. Returns empty array if no intervals found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to access this data.</response>
    [HttpGet("{instructorId}")]
    [Authorize(Roles = "Instructor,SchoolAdmin")]
    public async Task<ActionResult<IEnumerable<InstructorAvailabilityDto>>> GetInstructorAvailability(string instructorId)
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

        // If admin, check if instructor belongs to the same school
        if (User.IsInRole("SchoolAdmin"))
        {
            var admin = await _users.FindByIdAsync(userId);
            var instructor = await _users.FindByIdAsync(instructorId);
            
            if (admin == null || instructor == null || admin.AutoSchoolId != instructor.AutoSchoolId)
            {
                return Forbid(); // Return 403 if admin trying to access instructor from another school
            }
        }

        // 3. Query availability intervals starting from current date
        var currentDate = DateTime.Today;
        var availabilityIntervals = await _db.InstructorAvailabilities
            .Where(a => a.InstructorId == instructorId && a.Date >= currentDate)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartHour)
            .Select(a => new InstructorAvailabilityDto
            {
                IntervalId = a.IntervalId,
                Date = a.Date,
                // Format time as string to avoid seconds
                StartHour = a.StartHour.ToString(@"hh\:mm"),
                EndHour = a.EndHour.ToString(@"hh\:mm")
            })
            .AsNoTracking()
            .ToListAsync();

        return Ok(availabilityIntervals);
    }
    
    /// <summary>
    /// Creates a new availability interval for an instructor
    /// </summary>
    /// <remarks>
    /// <para>Registers a time slot when an instructor will be available for driving lessons.</para>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// {
    ///   "date": "2023-12-15",
    ///   "startHour": "10:00",
    ///   "endHour": "13:00"
    /// }
    /// ```
    /// </remarks>
    /// <param name="instructorId">The ID of the instructor to create availability for</param>
    /// <param name="dto">Availability interval data</param>
    /// <response code="201">Availability interval created successfully.</response>
    /// <response code="400">Invalid data or overlapping interval.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to create availability for this instructor.</response>
    [HttpPost("{instructorId}")]
    [Authorize(Roles = "Instructor,SchoolAdmin")]
    public async Task<ActionResult<InstructorAvailabilityDto>> CreateInstructorAvailability(
        string instructorId, 
        [FromBody] CreateInstructorAvailabilityDto dto)
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
            return Forbid(); // Return 403 Forbidden if instructor trying to create availability for another instructor
        }

        // If admin, check if instructor belongs to the same school
        if (User.IsInRole("SchoolAdmin"))
        {
            var admin = await _users.FindByIdAsync(userId);
            var instructor = await _users.FindByIdAsync(instructorId);
            
            if (admin == null || instructor == null || admin.AutoSchoolId != instructor.AutoSchoolId)
            {
                return Forbid(); // Return 403 if admin trying to create availability for instructor from another school
            }
        }

        // 3. Validate input
        // Check if the instructor exists
        var instructorExists = await _users.FindByIdAsync(instructorId) != null;
        if (!instructorExists)
        {
            return BadRequest(new { message = "Instructor not found" });
        }

        // Validate date is not in the past
        if (dto.Date.Date < DateTime.Today)
        {
            return BadRequest(new { message = "Cannot create availability for past dates" });
        }

        // Parse time strings to TimeSpan
        if (!TimeSpan.TryParse(dto.StartHour, out TimeSpan startTime) || 
            !TimeSpan.TryParse(dto.EndHour, out TimeSpan endTime))
        {
            return BadRequest(new { message = "Invalid time format. Please use 'hh:mm' format." });
        }

        // Remove seconds and milliseconds
        startTime = new TimeSpan(startTime.Hours, startTime.Minutes, 0);
        endTime = new TimeSpan(endTime.Hours, endTime.Minutes, 0);

        // Validate that start time is before end time
        if (startTime >= endTime)
        {
            return BadRequest(new { message = "Start time must be earlier than end time" });
        }

        // 4. Check for overlapping intervals
        bool hasOverlap = await _db.InstructorAvailabilities
            .AnyAsync(a => a.InstructorId == instructorId 
                        && a.Date.Date == dto.Date.Date 
                        && ((a.StartHour <= startTime && a.EndHour > startTime) || 
                            (a.StartHour < endTime && a.EndHour >= endTime) ||
                            (a.StartHour >= startTime && a.EndHour <= endTime)));

        if (hasOverlap)
        {
            return BadRequest(new { message = "This time interval overlaps with an existing availability" });
        }

        // 5. Create the new availability interval
        var newAvailability = new InstructorAvailability
        {
            InstructorId = instructorId,
            Date = dto.Date.Date, // Store only the date part, without time
            StartHour = startTime,
            EndHour = endTime
        };

        _db.InstructorAvailabilities.Add(newAvailability);
        await _db.SaveChangesAsync();

        // 6. Return the created interval
        var createdInterval = new InstructorAvailabilityDto
        {
            IntervalId = newAvailability.IntervalId,
            Date = newAvailability.Date,
            StartHour = startTime.ToString(@"hh\:mm"),
            EndHour = endTime.ToString(@"hh\:mm")
        };

        return CreatedAtAction(
            nameof(GetInstructorAvailability),
            new { instructorId = instructorId },
            createdInterval);
    }
    
    /// <summary>
    /// Updates an existing availability interval for an instructor
    /// </summary>
    /// <remarks>
    /// <para>Updates the date, start time, or end time of an existing availability interval.</para>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// {
    ///   "date": "2023-12-16",
    ///   "startHour": "11:00",
    ///   "endHour": "14:00"
    /// }
    /// ```
    /// </remarks>
    /// <param name="instructorId">The ID of the instructor who owns the availability</param>
    /// <param name="intervalId">The ID of the availability interval to update</param>
    /// <param name="dto">Updated availability interval data</param>
    /// <response code="200">Availability interval updated successfully.</response>
    /// <response code="400">Invalid data, overlapping interval, or interval has appointments.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to update this instructor's availability.</response>
    /// <response code="404">Availability interval not found.</response>
    [HttpPut("{instructorId}/{intervalId}")]
    [Authorize(Roles = "Instructor,SchoolAdmin")]
    public async Task<ActionResult<InstructorAvailabilityDto>> UpdateInstructorAvailability(
        string instructorId, 
        int intervalId,
        [FromBody] CreateInstructorAvailabilityDto dto)
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
            return Forbid(); // Return 403 Forbidden if instructor trying to update another instructor's availability
        }

        // If admin, check if instructor belongs to the same school
        if (User.IsInRole("SchoolAdmin"))
        {
            var admin = await _users.FindByIdAsync(userId);
            var instructor = await _users.FindByIdAsync(instructorId);
            
            if (admin == null || instructor == null || admin.AutoSchoolId != instructor.AutoSchoolId)
            {
                return Forbid(); // Return 403 if admin trying to update availability for instructor from another school
            }
        }

        // 3. Find the availability interval
        var availabilityInterval = await _db.InstructorAvailabilities
            .FirstOrDefaultAsync(a => a.IntervalId == intervalId && a.InstructorId == instructorId);

        if (availabilityInterval == null)
        {
            return NotFound(new { message = "Availability interval not found" });
        }

        // 4. Validate input
        // Validate date is not in the past
        if (dto.Date.Date < DateTime.Today)
        {
            return BadRequest(new { message = "Cannot update availability to past dates" });
        }

        // Parse time strings to TimeSpan
        if (!TimeSpan.TryParse(dto.StartHour, out TimeSpan startTime) || 
            !TimeSpan.TryParse(dto.EndHour, out TimeSpan endTime))
        {
            return BadRequest(new { message = "Invalid time format. Please use 'hh:mm' format." });
        }

        // Remove seconds and milliseconds
        startTime = new TimeSpan(startTime.Hours, startTime.Minutes, 0);
        endTime = new TimeSpan(endTime.Hours, endTime.Minutes, 0);

        // Validate that start time is before end time
        if (startTime >= endTime)
        {
            return BadRequest(new { message = "Start time must be earlier than end time" });
        }

        // 5. Check for overlapping intervals (excluding the current interval)
        bool hasOverlap = await _db.InstructorAvailabilities
            .Where(a => a.IntervalId != intervalId) // Exclude the current interval being updated
            .AnyAsync(a => a.InstructorId == instructorId 
                        && a.Date.Date == dto.Date.Date 
                        && ((a.StartHour <= startTime && a.EndHour > startTime) || 
                            (a.StartHour < endTime && a.EndHour >= endTime) ||
                            (a.StartHour >= startTime && a.EndHour <= endTime)));

        if (hasOverlap)
        {
            return BadRequest(new { message = "This time interval overlaps with an existing availability" });
        }

        // 6. Check if there are any appointments scheduled during the current availability
        var hasAppointments = await _db.Files
            .Where(f => f.InstructorId == instructorId)
            .Join(_db.Appointments,
                  f => f.FileId,
                  a => a.FileId,
                  (f, a) => a)
            .AnyAsync(a => a.Date.Date == availabilityInterval.Date.Date &&
                        a.StartHour < availabilityInterval.EndHour &&
                        a.EndHour > availabilityInterval.StartHour);

        if (hasAppointments)
        {
            return BadRequest(new { message = "Cannot update availability that has appointments scheduled" });
        }

        // 7. Update the availability interval
        availabilityInterval.Date = dto.Date.Date;
        availabilityInterval.StartHour = startTime;
        availabilityInterval.EndHour = endTime;

        await _db.SaveChangesAsync();

        // 8. Return the updated interval
        var updatedInterval = new InstructorAvailabilityDto
        {
            IntervalId = availabilityInterval.IntervalId,
            Date = availabilityInterval.Date,
            StartHour = startTime.ToString(@"hh\:mm"),
            EndHour = endTime.ToString(@"hh\:mm")
        };

        return Ok(updatedInterval);
    }
    
    /// <summary>
    /// Deletes an availability interval for an instructor
    /// </summary>
    /// <remarks>
    /// Removes a specific availability interval based on its ID. Only the instructor who owns the interval or a school admin can delete it.
    /// </remarks>
    /// <param name="instructorId">ID of the instructor who owns the availability interval</param>
    /// <param name="intervalId">ID of the availability interval to delete</param>
    /// <response code="200">Availability interval deleted successfully.</response>
    /// <response code="400">Invalid parameters or the interval has already been booked.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to delete this interval.</response>
    /// <response code="404">Availability interval not found.</response>
    [HttpDelete("{instructorId}/{intervalId}")]
    [Authorize(Roles = "Instructor,SchoolAdmin")]
    public async Task<IActionResult> DeleteInstructorAvailability(string instructorId, int intervalId)
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
            return Forbid(); // Return 403 Forbidden if instructor trying to delete another instructor's availability
        }

        // If admin, check if instructor belongs to the same school
        if (User.IsInRole("SchoolAdmin"))
        {
            var admin = await _users.FindByIdAsync(userId);
            var instructor = await _users.FindByIdAsync(instructorId);
            
            if (admin == null || instructor == null || admin.AutoSchoolId != instructor.AutoSchoolId)
            {
                return Forbid(); // Return 403 if admin trying to delete availability for instructor from another school
            }
        }

        // 3. Find the availability interval
        var availabilityInterval = await _db.InstructorAvailabilities
            .FirstOrDefaultAsync(a => a.IntervalId == intervalId && a.InstructorId == instructorId);

        if (availabilityInterval == null)
        {
            return NotFound(new { message = "Availability interval not found" });
        }

        // 4. Check if the interval is in the past (optional additional check)
        if (availabilityInterval.Date.Date < DateTime.Today)
        {
            return BadRequest(new { message = "Cannot delete availability intervals from the past" });
        }

        // 5. Check if there are any appointments scheduled during this availability
        // This query checks if there are any appointments that overlap with this availability interval
        var hasAppointments = await _db.Files
            .Where(f => f.InstructorId == instructorId)
            .Join(_db.Appointments,
                  f => f.FileId,
                  a => a.FileId,
                  (f, a) => a)
            .AnyAsync(a => a.Date.Date == availabilityInterval.Date.Date &&
                        a.StartHour < availabilityInterval.EndHour &&
                        a.EndHour > availabilityInterval.StartHour);

        if (hasAppointments)
        {
            return BadRequest(new { message = "Cannot delete availability that has appointments scheduled" });
        }

        // 6. Delete the availability interval
        _db.InstructorAvailabilities.Remove(availabilityInterval);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Availability interval deleted successfully" });
    }
}

/// <summary>
/// DTO for instructor availability information
/// </summary>
public class InstructorAvailabilityDto
{
    /// <summary>Unique identifier for the availability interval</summary>
    public int IntervalId { get; set; }
    
    /// <summary>Date of availability</summary>
    public DateTime Date { get; set; }
    
    /// <summary>Start time of availability interval (format: hh:mm)</summary>
    public string StartHour { get; set; }
    
    /// <summary>End time of availability interval (format: hh:mm)</summary>
    public string EndHour { get; set; }
}

/// <summary>
/// DTO for creating instructor availability
/// </summary>
public class CreateInstructorAvailabilityDto
{
    /// <summary>Date of availability</summary>
    [Required]
    public DateTime Date { get; set; }
    
    /// <summary>Start time of availability interval (format: hh:mm)</summary>
    [Required]
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Start time must be in format 'hh:mm'")]
    public string StartHour { get; set; }
    
    /// <summary>End time of availability interval (format: hh:mm)</summary>
    [Required]
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "End time must be in format 'hh:mm'")]
    public string EndHour { get; set; }
} 