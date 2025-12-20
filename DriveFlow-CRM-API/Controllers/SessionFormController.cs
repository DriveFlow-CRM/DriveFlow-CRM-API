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
    ///       "description": "Semnalizare la schimbarea direc?iei",
    ///       "count": 3,
    ///       "penaltyPoints": 3
    ///     }
    ///   ],
    ///   "isLocked": true
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
            isLocked: sessionForm.IsLocked,
            mistakes: mistakeBreakdown
        );

        return Ok(dto);
    }

    /// <summary>
    /// Starts a new session form for a driving lesson appointment.
    /// </summary>
    /// <remarks>
    /// <para>Instructors can only start session forms for their own lessons.</para>
    /// <para>Only one active form is allowed per appointment (409 Conflict if already exists).</para>
    /// <para><strong>Sample response (201 Created)</strong></para>
    ///
    /// ```json
    /// {
    ///   "id": 501,
    ///   "id_app": 5012,
    ///   "id_formular": 1,
    ///   "mistakesJson": "[]",
    ///   "isLocked": false,
    ///   "createdAt": "2025-11-01T10:00:00Z",
    ///   "finalizedAt": null,
    ///   "totalPoints": null,
    ///   "result": null
    /// }
    /// ```
    /// </remarks>
    /// <param name="id_app">The appointment ID</param>
    /// <response code="201">Session form created successfully.</response>
    /// <response code="400">Invalid appointment ID.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Instructor is not authorized for this appointment.</response>
    /// <response code="404">Appointment not found or no exam form exists for the category.</response>
    /// <response code="409">Session form already exists for this appointment.</response>
    [HttpPost("{id_app}/form/start")]
    [ProducesResponseType(typeof(SessionFormDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionFormDto>> StartSessionForm(int id_app)
    {
        // 1. Validate appointment ID
        if (id_app <= 0)
            return BadRequest(new { message = "Appointment ID must be positive." });

        // 2. Get authenticated instructor
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var instructor = await _users.FindByIdAsync(userId);
        if (instructor == null)
            return Unauthorized(new { message = "Instructor not found." });

        // 3. Get appointment with file and teaching category
        var appointment = await _db.Appointments
            .Include(a => a.File)
                .ThenInclude(f => f.TeachingCategory)
            .FirstOrDefaultAsync(a => a.AppointmentId == id_app);

        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });

        // 4. Verify instructor owns this appointment
        if (appointment.File?.InstructorId != userId)
            return Forbid();

        // 5. Check if session form already exists
        var existingForm = await _db.SessionForms
            .FirstOrDefaultAsync(sf => sf.AppointmentId == id_app);

        if (existingForm != null)
            return Conflict(new { message = "Session form already exists for this appointment." });

        // 6. Get the exam form for this teaching category
        if (appointment.File?.TeachingCategoryId == null)
            return NotFound(new { message = "Appointment file has no teaching category assigned." });

        var examForm = await _db.ExamForms
            .FirstOrDefaultAsync(ef => ef.TeachingCategoryId == appointment.File.TeachingCategoryId);

        if (examForm == null)
            return NotFound(new { message = "No exam form found for this teaching category." });

        // 7. Create new session form
        var sessionForm = new SessionForm
        {
            AppointmentId = id_app,
            FormId = examForm.FormId,
            MistakesJson = "[]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.SessionForms.Add(sessionForm);
        await _db.SaveChangesAsync();

        // 8. Return DTO
        var dto = new SessionFormDto(
            id: sessionForm.SessionFormId,
            id_app: sessionForm.AppointmentId,
            id_formular: sessionForm.FormId,
            isLocked: sessionForm.IsLocked,
            createdAt: sessionForm.CreatedAt,
            finalizedAt: sessionForm.FinalizedAt,
            totalPoints: sessionForm.TotalPoints,
            result: sessionForm.Result,
            mistakesJson: sessionForm.MistakesJson
        );

        return Created($"/api/appointments/{id_app}/form", dto);
    }

    /// <summary>
    /// Updates the mistake count for a specific exam item in the session form.
    /// Instructors can increment (+1) or decrement (-1) mistake counts during the lesson.
    /// </summary>
    /// <remarks>
    /// <para>Only the instructor who owns the appointment can update mistakes.</para>
    /// <para>The session form must not be locked (finalized).</para>
    /// <para>Count never goes below zero (idempotent on negative).</para>
    /// <para><strong>Sample request body</strong></para>
    ///
    /// ```json
    /// {
    ///   "id_item": 2,
    ///   "delta": 1
    /// }
    /// ```
    ///
    /// <para><strong>Sample response (200 OK)</strong></para>
    ///
    /// ```json
    /// {
    ///   "id_item": 2,
    ///   "count": 3
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">The session form ID</param>
    /// <param name="req">Request containing id_item and delta (+1 or -1)</param>
    /// <response code="200">Mistake count updated successfully.</response>
    /// <response code="400">Invalid data or item not found in exam form.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Instructor is not authorized for this session form.</response>
    /// <response code="404">Session form not found.</response>
    /// <response code="423">Session form is locked (finalized).</response>
    [HttpPatch("{id}/update-item")]
    [ProducesResponseType(typeof(UpdateMistakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<UpdateMistakeResponse>> UpdateItem(int id, [FromBody] UpdateMistakeRequest req)
    {
        // 1. Validate input
        if (req == null || req.id_item <= 0)
            return BadRequest(new { message = "Request must contain a valid id_item." });

        if (req.delta != 1 && req.delta != -1)
            return BadRequest(new { message = "Delta must be +1 or -1." });

        // 2. Get authenticated instructor
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // 3. Get session form with appointment and file
        var sessionForm = await _db.SessionForms
            .Include(sf => sf.Appointment)
                .ThenInclude(a => a.File)
            .Include(sf => sf.ExamForm)
                .ThenInclude(ef => ef.Items)
            .FirstOrDefaultAsync(sf => sf.SessionFormId == id);

        if (sessionForm == null)
            return NotFound(new { message = "Session form not found." });

        // 4. Check if locked
        if (sessionForm.IsLocked)
            return StatusCode(StatusCodes.Status423Locked, new { message = "Session form is locked and cannot be modified." });

        // 5. Verify instructor owns this session
        if (sessionForm.Appointment?.File?.InstructorId != userId)
            return Forbid();

        // 6. Verify item exists in the exam form
        var examItem = sessionForm.ExamForm.Items.FirstOrDefault(i => i.ItemId == req.id_item);
        if (examItem == null)
            return BadRequest(new { message = $"Item with id_item {req.id_item} does not exist in the exam form for this session." });

        // 7. Parse current mistakes JSON
        List<MistakeEntry> mistakes;
        try
        {
            mistakes = System.Text.Json.JsonSerializer.Deserialize<List<MistakeEntry>>(sessionForm.MistakesJson) ?? new List<MistakeEntry>();
        }
        catch
        {
            mistakes = new List<MistakeEntry>();
        }

        // 8. Find or create entry for this item
        var existingEntry = mistakes.FirstOrDefault(m => m.id_item == req.id_item);
        int newCount;

        if (existingEntry != null)
        {
            // Update existing count
            newCount = Math.Max(0, existingEntry.count + req.delta);
            existingEntry.count = newCount;

            // Remove entry if count becomes 0
            if (newCount == 0)
                mistakes.Remove(existingEntry);
        }
        else
        {
            // Only add new entry if delta is positive
            if (req.delta > 0)
            {
                newCount = req.delta;
                mistakes.Add(new MistakeEntry { id_item = req.id_item, count = newCount });
            }
            else
            {
                // Decrementing from 0 ? stay at 0 (idempotent)
                newCount = 0;
            }
        }

        // 9. Serialize and save
        sessionForm.MistakesJson = System.Text.Json.JsonSerializer.Serialize(mistakes);
        await _db.SaveChangesAsync();

        // 10. Return response
        return Ok(new UpdateMistakeResponse(
            id_item: req.id_item,
            count: newCount
        ));
    }

    /// <summary>
    /// Finalizes the session form by calculating total points and determining pass/fail result.
    /// Once finalized, the form is locked and cannot be modified.
    /// </summary>
    /// <remarks>
    /// <para>Only the instructor who owns the appointment can finalize the form.</para>
    /// <para>After finalization, any PATCH requests will return 423 Locked.</para>
    /// <para>Result calculation: totalPoints = ?(count × penaltyPoints)</para>
    /// <para>Pass/Fail logic: FAILED if totalPoints > maxPoints, OK otherwise</para>
    /// <para><strong>Sample responses:</strong></para>
    ///
    /// <para><strong>Passing exam (200 OK):</strong></para>
    /// ```json
    /// {
    ///   "id": 501,
    ///   "totalPoints": 21,
    ///   "maxPoints": 21,
    ///   "result": "OK"
    /// }
    /// ```
    ///
    /// <para><strong>Failing exam (200 OK):</strong></para>
    /// ```json
    /// {
    ///   "id": 501,
    ///   "totalPoints": 24,
    ///   "maxPoints": 21,
    ///   "result": "FAILED"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">The session form ID</param>
    /// <response code="200">Session form finalized successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Instructor is not authorized for this session form.</response>
    /// <response code="404">Session form not found.</response>
    /// <response code="423">Session form is already locked (finalized).</response>
    [HttpPost("{id}/finalize")]
    [ProducesResponseType(typeof(FinalizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<FinalizeResponse>> Finalize(int id)
    {
        // 1. Get authenticated instructor
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // 2. Get session form with all required data
        var sessionForm = await _db.SessionForms
            .Include(sf => sf.Appointment)
                .ThenInclude(a => a.File)
            .Include(sf => sf.ExamForm)
                .ThenInclude(ef => ef.Items)
            .FirstOrDefaultAsync(sf => sf.SessionFormId == id);

        if (sessionForm == null)
            return NotFound(new { message = "Session form not found." });

        // 3. Check if already locked
        if (sessionForm.IsLocked)
            return StatusCode(StatusCodes.Status423Locked, new { message = "Session form is already finalized and locked." });

        // 4. Verify instructor owns this session
        if (sessionForm.Appointment?.File?.InstructorId != userId)
            return Forbid();

        // 5. Parse mistakes JSON
        List<MistakeEntry> mistakes;
        try
        {
            mistakes = System.Text.Json.JsonSerializer.Deserialize<List<MistakeEntry>>(sessionForm.MistakesJson) ?? new List<MistakeEntry>();
        }
        catch
        {
            mistakes = new List<MistakeEntry>();
        }

        // 6. Calculate total points: ?(count × penaltyPoints)
        int totalPoints = 0;
        foreach (var mistake in mistakes)
        {
            var examItem = sessionForm.ExamForm.Items.FirstOrDefault(i => i.ItemId == mistake.id_item);
            if (examItem != null)
            {
                totalPoints += mistake.count * examItem.PenaltyPoints;
            }
        }

        // 7. Determine result (OK if totalPoints <= maxPoints, FAILED otherwise)
        var maxPoints = sessionForm.ExamForm.MaxPoints;
        var result = totalPoints > maxPoints ? "FAILED" : "OK";

        // 8. Update session form
        sessionForm.TotalPoints = totalPoints;
        sessionForm.Result = result;
        sessionForm.IsLocked = true;
        sessionForm.FinalizedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // 9. Return response
        return Ok(new FinalizeResponse(
            id: sessionForm.SessionFormId,
            totalPoints: totalPoints,
            maxPoints: maxPoints,
            result: result
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
    /// </list>
    ///
    /// <para><strong>Sample response (200 OK):</strong></para>
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
    /// </remarks>
    /// <param name="id_student">The student user ID</param>
    /// <param name="from">Start date filter (optional)</param>
    /// <param name="to">End date filter (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
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
    public async Task<ActionResult<PagedResult<SessionFormListItemDto>>> ListStudentForms(
        string id_student,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
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

        // 6. Build query with filters
        var query = _db.SessionForms
            .Include(sf => sf.Appointment)
            .Include(sf => sf.ExamForm)
            .Where(sf => sf.Appointment.File != null && sf.Appointment.File.StudentId == id_student);

        // Apply date filters
        if (fromDate.HasValue)
            query = query.Where(sf => sf.Appointment.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(sf => sf.Appointment.Date <= toDate.Value);

        // 7. Get total count
        var total = await query.CountAsync();

        // 8. Apply sorting (descending by date) and pagination
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

        // 9. Build paged result
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
internal class MistakeEntry
{
    public int id_item { get; set; }
    public int count { get; set; }
}
