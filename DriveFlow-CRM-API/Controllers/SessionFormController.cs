using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using DriveFlow_CRM_API.Models.DTOs;
using System.Security.Claims;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// Controller for managing session forms during driving lessons.
/// </summary>
[ApiController]
[Route("api/session-forms")]
[Authorize(Roles = "Instructor,Student,SchoolAdmin")]
public class SessionFormController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public SessionFormController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    /// <summary>
    /// Retrieves a session form with all details including mistake breakdown.
    /// </summary>
    /// <remarks>
    /// <para>Access control:</para>
    /// <list type="bullet">
    /// <item>Instructor: must own the appointment's file</item>
    /// <item>Student: must be the student on the file</item>
    /// <item>SchoolAdmin: must belong to the same school</item>
    /// </list>
    /// 
    /// <para><strong>Sample response (200 OK):</strong></para>
    /// ```json
    /// {
    ///   "id": 501,
    ///   "appointmentDate": "2025-11-01",
    ///   "studentName": "Ionescu Maria",
    ///   "instructorName": "Popescu Ion",
    ///   "totalPoints": 24,
    ///   "maxPoints": 21,
    ///   "result": "FAILED",
    ///   "mistakes": [
    ///     {
    ///       "id_item": 1,
    ///       "description": "Semnalizare la schimbarea directiei",
    ///       "count": 3,
    ///       "penaltyPoints": 3
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">The session form ID</param>
    /// <response code="200">Session form retrieved successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is not authorized to view this session form.</response>
    /// <response code="404">Session form not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SessionFormViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionFormViewDto>> Get(int id)
    {
        // 1. Get authenticated user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _users.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        // 2. Get session form with all required relationships
        var sessionForm = await _db.SessionForms
            .Include(sf => sf.Appointment)
                .ThenInclude(a => a.File)
                    .ThenInclude(f => f.Student)
            .Include(sf => sf.Appointment)
                .ThenInclude(a => a.File)
                    .ThenInclude(f => f.Instructor)
            .Include(sf => sf.ExamForm)
                .ThenInclude(ef => ef.Items)
            .FirstOrDefaultAsync(sf => sf.SessionFormId == id);

        if (sessionForm == null)
            return NotFound(new { message = "Session form not found." });

        // 3. Authorization checks
        var file = sessionForm.Appointment?.File;
        if (file == null)
            return NotFound(new { message = "Associated file not found." });

        var isInstructor = User.IsInRole("Instructor");
        var isStudent = User.IsInRole("Student");
        var isSchoolAdmin = User.IsInRole("SchoolAdmin");

        // Instructor: must own the file
        if (isInstructor && file.InstructorId != userId)
            return Forbid();

        // Student: must be the student on the file
        if (isStudent && file.StudentId != userId)
            return Forbid();

        // SchoolAdmin: must belong to the same school
        if (isSchoolAdmin && user.AutoSchoolId != file.Student?.AutoSchoolId)
            return Forbid();

        // 4. Parse mistakes JSON
        List<MistakeEntry> mistakes;
        try
        {
            mistakes = System.Text.Json.JsonSerializer.Deserialize<List<MistakeEntry>>(sessionForm.MistakesJson) ?? new List<MistakeEntry>();
        }
        catch
        {
            mistakes = new List<MistakeEntry>();
        }

        // 5. Build mistake breakdown with item details
        var mistakeBreakdown = mistakes
            .Select(m =>
            {
                var item = sessionForm.ExamForm.Items.FirstOrDefault(i => i.ItemId == m.id_item);
                return item != null
                    ? new MistakeBreakdownDto(
                        id_item: m.id_item,
                        description: item.Description,
                        count: m.count,
                        penaltyPoints: item.PenaltyPoints
                    )
                    : null;
            })
            .Where(m => m != null)
            .Cast<MistakeBreakdownDto>()
            .OrderBy(m => m.id_item)
            .ToList();

        // 6. Build response DTO
        var dto = new SessionFormViewDto(
            id: sessionForm.SessionFormId,
            appointmentDate: DateOnly.FromDateTime(sessionForm.Appointment.Date),
            studentName: $"{file.Student?.FirstName} {file.Student?.LastName}",
            instructorName: $"{file.Instructor?.FirstName} {file.Instructor?.LastName}",
            totalPoints: sessionForm.TotalPoints,
            maxPoints: sessionForm.ExamForm.MaxPoints,
            result: sessionForm.Result,
            mistakes: mistakeBreakdown
        );

        return Ok(dto);
    }

    /// <summary>
    /// Submits a completed session form with all mistakes recorded during the driving lesson.
    /// </summary>
    /// <remarks>
    /// <para>Creates a new session form with the final mistake counts and calculates the result.</para>
    /// <para>Only the instructor who owns the appointment can submit the form.</para>
    /// <para>Only one form is allowed per appointment (409 Conflict if already exists).</para>
    /// <para>Result calculation: totalPoints = ?(count ? penaltyPoints)</para>
    /// <para>Pass/Fail logic: FAILED if totalPoints > maxPoints, OK otherwise</para>
    ///
    /// <para><strong>Sample request body:</strong></para>
    /// ```json
    /// {
    ///   "mistakes": [
    ///     { "id_item": 1, "count": 2 },
    ///     { "id_item": 3, "count": 1 }
    ///   ],
    ///   "maxPoints": 21
    /// }
    /// ```
    ///
    /// <para><strong>Sample response (201 Created) - Passing:</strong></para>
    /// ```json
    /// {
    ///   "id": 501,
    ///   "totalPoints": 15,
    ///   "maxPoints": 21,
    ///   "result": "OK"
    /// }
    /// ```
    ///
    /// <para><strong>Sample response (201 Created) - Failing:</strong></para>
    /// ```json
    /// {
    ///   "id": 501,
    ///   "totalPoints": 24,
    ///   "maxPoints": 21,
    ///   "result": "FAILED"
    /// }
    /// ```
    /// </remarks>
    /// <param name="appointmentId">The appointment ID</param>
    /// <param name="request">Request containing the list of mistakes and maxPoints</param>
    /// <response code="201">Session form submitted successfully.</response>
    /// <response code="400">Invalid data, appointment ID, or items not found in exam form.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Instructor is not authorized for this appointment.</response>
    /// <response code="404">Appointment not found or no exam form exists for the category.</response>
    /// <response code="409">Session form already exists for this appointment.</response>
    [HttpPost("{appointmentId}/submit")]
    [ProducesResponseType(typeof(SubmitSessionFormResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmitSessionFormResponse>> SubmitSessionForm(
        int appointmentId,
        [FromBody] SubmitSessionFormRequest request)
    {
        // 1. Validate appointment ID
        if (appointmentId <= 0)
            return BadRequest(new { message = "Appointment ID must be positive." });

        // 2. Validate request
        if (request == null)
            return BadRequest(new { message = "Request body is required." });

        if (request.MaxPoints <= 0)
            return BadRequest(new { message = "MaxPoints must be positive." });

        // 3. Get authenticated instructor
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // 4. Get appointment with file and teaching category
        var appointment = await _db.Appointments
            .Include(a => a.File)
                .ThenInclude(f => f.TeachingCategory)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });

        // 5. Verify instructor owns this appointment
        if (appointment.File?.InstructorId != userId)
            return Forbid();

        // 6. Check if session form already exists
        var existingForm = await _db.SessionForms
            .FirstOrDefaultAsync(sf => sf.AppointmentId == appointmentId);

        if (existingForm != null)
            return Conflict(new { message = "Session form already exists for this appointment." });

        // 7. Get the exam form for this teaching category's license
        var teachingCategory = appointment.File?.TeachingCategory;
        if (teachingCategory?.LicenseId == null)
            return NotFound(new { message = "Appointment file has no teaching category with license assigned." });

        var examForm = await _db.ExamForms
            .Include(ef => ef.Items)
            .FirstOrDefaultAsync(ef => ef.LicenseId == teachingCategory.LicenseId);

        if (examForm == null)
            return NotFound(new { message = "No exam form found for this license." });

        // 8. Validate all items exist in the exam form and calculate total points
        var mistakes = request.Mistakes ?? new List<MistakeItemDto>();
        int totalPoints = 0;

        foreach (var mistake in mistakes)
        {
            if (mistake.Count < 0)
                return BadRequest(new { message = $"Mistake count for item {mistake.IdItem} cannot be negative." });

            var examItem = examForm.Items.FirstOrDefault(i => i.ItemId == mistake.IdItem);
            if (examItem == null)
                return BadRequest(new { message = $"Item with id_item {mistake.IdItem} does not exist in the exam form." });

            totalPoints += mistake.Count * examItem.PenaltyPoints;
        }

        // 9. Determine result
        var result = totalPoints > request.MaxPoints ? "FAILED" : "OK";

        // 10. Create session form
        var mistakesJson = System.Text.Json.JsonSerializer.Serialize(
            mistakes.Where(m => m.Count > 0).Select(m => new MistakeEntry(m.IdItem, m.Count)).ToList()
        );

        var sessionForm = new SessionForm
        {
            AppointmentId = appointmentId,
            FormId = examForm.FormId,
            MistakesJson = mistakesJson,
            CreatedAt = DateTime.UtcNow,
            FinalizedAt = DateTime.UtcNow,
            TotalPoints = totalPoints,
            Result = result
        };

        _db.SessionForms.Add(sessionForm);
        await _db.SaveChangesAsync();

        // 11. Return response
        return Created($"/api/session-forms/{sessionForm.SessionFormId}", new SubmitSessionFormResponse(
            Id: sessionForm.SessionFormId,
            TotalPoints: totalPoints,
            MaxPoints: request.MaxPoints,
            Result: result
        ));
    }

    // ?????????????????????????????? GET STUDENT SESSION FORMS (HISTORY) ??????????????????????????????
    /// <summary>
    /// Lists all session forms for a student with optional date filtering, sorting, and pagination.
    /// </summary>
    /// <remarks>
    /// <para><strong>Access control:</strong></para>
    /// <list type="bullet">
    /// <item>Student: can only view their own forms (self)</item>
    /// <item>Instructor: can view forms for students in their active files</item>
    /// <item>SchoolAdmin: can view forms for all students in their school</item>
    /// </list>
    ///
    /// <para><strong>Query parameters:</strong></para>
    /// <list type="bullet">
    /// <item>from: Start date filter (optional, format: YYYY-MM-DD)</item>
    /// <item>to: End date filter (optional, format: YYYY-MM-DD)</item>
    /// <item>page: Page number (default: 1)</item>
    /// <item>pageSize: Items per page (default: 20, max: 100)</item>
    /// <item>fileId: If provided, returns only session forms for that specific FileId (simple list, no pagination)</item>
    /// </list>
    ///
    /// <para><strong>Sample response (200 OK) - without fileId (paginated):</strong></para>
    /// ```json
    /// {
    ///   "page": 1,
    ///   "pageSize": 20,
    ///   "total": 2,
    ///   "items": [
    ///     {
    ///       "id": 502,
    ///       "date": "2025-10-19",
    ///       "totalPoints": 24,
    ///       "maxPoints": 21,
    ///       "result": "FAILED"
    ///     },
    ///     {
    ///       "id": 501,
    ///       "date": "2025-10-12",
    ///       "totalPoints": 18,
    ///       "maxPoints": 21,
    ///       "result": "OK"
    ///     }
    ///   ]
    /// }
    /// ```
    ///
    /// <para><strong>Sample response (200 OK) - with fileId (filtered, simple list):</strong></para>
    /// ```json
    /// [
    ///   { "id": 502, "date": "2025-10-19", "totalPoints": 24, "maxPoints": 21, "result": "FAILED" },
    ///   { "id": 501, "date": "2025-10-12", "totalPoints": 18, "maxPoints": 21, "result": "OK" }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="id_student">The student user ID</param>
    /// <param name="from">Start date filter (optional)</param>
    /// <param name="to">End date filter (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="fileId">If provided, returns only session forms for that specific FileId (simple list, no pagination)</param>
    /// <response code="200">Session forms retrieved successfully.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is not authorized to view this student's forms.</response>
    /// <response code="404">Student not found.</response>
    [HttpGet("/api/students/{id_student}/session-forms")]
    [ProducesResponseType(typeof(PagedResult<SessionFormListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListStudentForms(
        string id_student,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? fileId = null)
    {
        // 1. Validate pagination parameters
        if (page < 1)
            return BadRequest(new { message = "Page must be at least 1." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "PageSize must be between 1 and 100." });

        // 2. Get authenticated user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var currentUser = await _users.FindByIdAsync(userId);
        if (currentUser == null)
            return Unauthorized();

        // 3. Check if student exists
        var student = await _users.FindByIdAsync(id_student);
        if (student == null)
            return NotFound(new { message = "Student not found." });

        // 4. Authorization checks
        var isStudent = User.IsInRole("Student");
        var isInstructor = User.IsInRole("Instructor");
        var isSchoolAdmin = User.IsInRole("SchoolAdmin");

        // Student can only view their own forms
        if (isStudent && userId != id_student)
            return Forbid();

        // Instructor can only view students in their active files
        if (isInstructor)
        {
            var hasActiveFile = await _db.Files
                .AnyAsync(f => f.StudentId == id_student && f.InstructorId == userId);

            if (!hasActiveFile)
                return Forbid();
        }

        // SchoolAdmin can only view students in their school
        if (isSchoolAdmin && currentUser.AutoSchoolId != student.AutoSchoolId)
            return Forbid();

        // 5. Parse date filters
        DateTime? fromDate = null;
        DateTime? toDate = null;

        if (!string.IsNullOrWhiteSpace(from))
        {
            if (!DateTime.TryParse(from, out var parsedFrom))
                return BadRequest(new { message = "Invalid 'from' date format. Use YYYY-MM-DD." });
            fromDate = parsedFrom.Date;
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            if (!DateTime.TryParse(to, out var parsedTo))
                return BadRequest(new { message = "Invalid 'to' date format. Use YYYY-MM-DD." });
            toDate = parsedTo.Date.AddDays(1).AddSeconds(-1); // End of day
        }

        // 6. If fileId is provided, return only session forms for that specific file
        if (fileId.HasValue)
        {
            // Verify the file belongs to the student
            var fileExists = await _db.Files.AnyAsync(f => f.FileId == fileId.Value && f.StudentId == id_student);
            if (!fileExists)
                return NotFound(new { message = $"File with ID {fileId.Value} not found for this student." });

            // Get appointment IDs for this specific file
            var appointmentIds = await _db.Appointments
                .Where(a => a.FileId == fileId.Value)
                .Select(a => a.AppointmentId)
                .ToListAsync();

            // Build query for this file's session forms
            var fileQuery = _db.SessionForms
                .Include(sf => sf.Appointment)
                .Include(sf => sf.ExamForm)
                .Where(sf => appointmentIds.Contains(sf.AppointmentId));

            // Apply date filters
            if (fromDate.HasValue)
                fileQuery = fileQuery.Where(sf => sf.Appointment.Date >= fromDate.Value);

            if (toDate.HasValue)
                fileQuery = fileQuery.Where(sf => sf.Appointment.Date <= toDate.Value);

            var fileSessionForms = await fileQuery
                .OrderByDescending(sf => sf.Appointment.Date)
                .ThenByDescending(sf => sf.SessionFormId)
                .ToListAsync();

            var fileItems = fileSessionForms.Select(sf => new SessionFormListItemDto(
                sf.SessionFormId,
                DateOnly.FromDateTime(sf.Appointment.Date),
                sf.TotalPoints,
                sf.ExamForm.MaxPoints,
                sf.Result
            )).ToList();

            return Ok(fileItems);
        }

        // 7. Default behavior: Build query with filters (flat list, paginated)
        var query = _db.SessionForms
            .Include(sf => sf.Appointment)
            .Include(sf => sf.ExamForm)
            .Where(sf => sf.Appointment.File != null && sf.Appointment.File.StudentId == id_student);

        // Apply date filters
        if (fromDate.HasValue)
            query = query.Where(sf => sf.Appointment.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(sf => sf.Appointment.Date <= toDate.Value);

        // 8. Get total count
        var total = await query.CountAsync();

        // 9. Apply sorting (descending by date) and pagination
        var sessionForms = await query
            .OrderByDescending(sf => sf.Appointment.Date)
            .ThenByDescending(sf => sf.SessionFormId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to DTOs after materialization
        var items = sessionForms.Select(sf => new SessionFormListItemDto(
            sf.SessionFormId,
            DateOnly.FromDateTime(sf.Appointment.Date),
            sf.TotalPoints,
            sf.ExamForm.MaxPoints,
            sf.Result
        )).ToList();

        // 10. Build paged result
        var result = new PagedResult<SessionFormListItemDto>(
            page,
            pageSize,
            total,
            items
        );

        return Ok(result);
    }
}

/// <summary>Internal class for JSON serialization of mistake entries.</summary>
public sealed class MistakeEntry
{
    public int id_item { get; set; }
    public int count { get; set; }

    public MistakeEntry(int id_item, int count)
    {
        this.id_item = id_item;
        this.count = count;
    }

}
