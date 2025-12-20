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
[Route("api/appointments")]
[Authorize(Roles = "Instructor")]
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
}

/// <summary>Internal class for JSON serialization of mistake entries.</summary>
internal class MistakeEntry
{
    public int id_item { get; set; }
    public int count { get; set; }
}
