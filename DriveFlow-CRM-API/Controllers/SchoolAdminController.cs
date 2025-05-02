using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DriveFlow_CRM_API.Models;
using File = DriveFlow_CRM_API.Models.File;


namespace DriveFlow_CRM_API.Controllers;


[ApiController]
[Route("api/[controller]/autoschool/{schoolId:int}")]
[Authorize(Policy = "SchoolAdmin")]
public class SchoolAdminController : ControllerBase
{
    // ───────────────────────────── fields & ctor ─────────────────────────────
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    /// <summary>Constructor invoked per request by DI.</summary>
    public SchoolAdminController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }

    // ───────────────────────  CREATE INSTRUCTOR ───────────────────────
    /// <summary>
    /// Creates a new instructor and links them to the selected teaching categories.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request body</strong></para>
    ///
    /// ```json
    /// {
    ///   "firstName": "Marius",
    ///   "lastName":  "Popescu",
    ///   "email":     "marius.popescu@school.ro",
    ///   "phone":     "0723446789",
    ///   "password":  "*Parola193",
    ///   "teachingCategoryIds": [1, 2, 3]
    /// }
    /// ```
    /// </remarks>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="dto">
    /// Instructor details together with <c>teachingCategoryIds</c>.
    /// </param>
    /// <response code="201">Instructor created successfully.</response>
    /// <response code="400">
    /// Validation failed (password strength, duplicate e-mail, invalid IDs).
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">
    /// Authenticated user does not belong to the specified school.
    /// </response>
    [HttpPost("create/instructor")]
    public async Task<IActionResult> CreateInstructorAsync(
    int schoolId,
    [FromBody] InstructorCreateDto dto)
    {
        // ─── basic validation ───
        if (dto.TeachingCategoryIds is null || dto.TeachingCategoryIds.Count == 0)
            return BadRequest(new { message = "teachingCategoryIds must contain at least one element" });

        // duplicate e-mail check
        if (await _users.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "A user with the given email address already exists" });

        // ─── validate teachingCategoryIds belong to this school ───
        var validIds = await _db.TeachingCategories
                                .Where(tc => tc.AutoSchoolId == schoolId &&
                                             dto.TeachingCategoryIds.Contains(tc.TeachingCategoryId))
                                .Select(tc => tc.TeachingCategoryId)
                                .ToListAsync();

        if (validIds.Count != dto.TeachingCategoryIds.Distinct().Count())
            return BadRequest(new { message = "One or more teachingCategoryIds are invalid for this school" });

        // ─── transaction ───
        await using var tx = await _db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.Email,
            PhoneNumber = dto.Phone,
            AutoSchoolId = schoolId
        };

        var createRes = await _users.CreateAsync(user, dto.Password);
        if (!createRes.Succeeded)
        {
            await tx.RollbackAsync();
            return BadRequest(new { message = "Identity creation failed", errors = createRes.Errors });
        }

        // ensure role exists & assign
        if (!await _roles.RoleExistsAsync("Instructor"))
            await _roles.CreateAsync(new IdentityRole("Instructor"));
        await _users.AddToRoleAsync(user, "Instructor");

        // ─── link teaching categories ───
        foreach (var id in validIds.Distinct())
            _db.ApplicationUserTeachingCategories.Add(
                new ApplicationUserTeachingCategory { UserId = user.Id, TeachingCategoryId = id });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Created(
            $"api/schooladmin/autoschool/{schoolId}/getUser/{user.Id}",
            new { userId = user.Id, message = "Instructor created successfully" });
    }


    // ─────────── CREATE STUDENT ───────────
    /// <summary>
    /// Creates a new <c>Student</c> together with the related <c>File</c> and <c>Payment</c> records.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request body</strong></para>
    ///
    /// ```json
    /// {
    ///   "student": {
    ///     "firstName": "Ioana",
    ///     "lastName":  "Marin",
    ///     "email":     "ioana.marin@student.ro",
    ///     "cnp":       "2990101223344",
    ///     "phone":     "0734567890",
    ///     "password":  "*Studentpass1"
    ///   },
    ///   "payment": {
    ///     "scholarshipBasePayment": true,
    ///     "sessionsPayed": 0
    ///   },
    ///   "file": {
    ///     "scholarshipStartDate":    "2024-04-25",
    ///     "criminalRecordExpiryDate": "2025-04-25",
    ///     "medicalRecordExpiryDate":  "2024-10-25",
    ///     "status":        "Draft",
    ///     "instructorId":  null,
    ///     "vehicleId":     null,
    ///     "teachingCategoryId": null
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="dto">Composite payload (<see cref="StudentCreateDto"/>).</param>
    /// <response code="201">Student created successfully.</response>
    /// <response code="400">Validation failed (duplicate CNP, negative sessions, etc.).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user does not belong to the specified school.</response>

    [HttpPost("create/student")]
    public async Task<IActionResult> CreateStudentAsync(
    int schoolId,
    [FromBody] StudentCreateDto dto)
    {
        // ─── quick checks ───
        if (dto.Payment.SessionsPayed < 0)
            return BadRequest(new { message = "sessionsPayed must be zero or a positive number." });

        if (dto.Student.Cnp?.Length != 13 || !dto.Student.Cnp.All(char.IsDigit))
            return BadRequest(new { message = "CNP must contain exactly 13 numeric digits." });

        // uniqueness checks
        if (await _users.Users.AnyAsync(u => u.Email == dto.Student.Email))
            return BadRequest(new { message = "A user with the given email address already exists." });

        if (await _users.Users.AnyAsync(u => u.Cnp == dto.Student.Cnp))
            return BadRequest(new { message = "A student with the given CNP already exists." });

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // ─── create user ───
            var user = new ApplicationUser
            {
                FirstName = dto.Student.FirstName,
                LastName = dto.Student.LastName,
                Email = dto.Student.Email,
                UserName = dto.Student.Email,
                PhoneNumber = dto.Student.Phone,
                Cnp = dto.Student.Cnp,
                AutoSchoolId = schoolId
            };

            var idRes = await _users.CreateAsync(user, dto.Student.Password);
            if (!idRes.Succeeded)
            {
                await tx.RollbackAsync();
                return BadRequest(new { message = "Identity creation failed", errors = idRes.Errors });
            }

            // ensure Student role exists & assign
            if (!await _roles.RoleExistsAsync("Student"))
                await _roles.CreateAsync(new IdentityRole("Student"));

            await _users.AddToRoleAsync(user, "Student");

            // ─── parse file status ───
            if (!Enum.TryParse<FileStatus>(dto.File.Status, true, out var statusEnum))
                return BadRequest(new
                {
                    message = $"Invalid file status. Allowed values: {string.Join(", ", Enum.GetNames(typeof(FileStatus)))}."
                });

            // convert instructor id
            string? instructorId = dto.File.InstructorId?.ToString();

            var studentFile = new File
            {
                ScholarshipStartDate = dto.File.ScholarshipStartDate,
                CriminalRecordExpiryDate = dto.File.CriminalRecordExpiryDate,
                MedicalRecordExpiryDate = dto.File.MedicalRecordExpiryDate,
                Status = statusEnum,
                InstructorId = instructorId,
                VehicleId = dto.File.VehicleId,
                TeachingCategoryId = dto.File.TeachingCategoryId,
                StudentId = user.Id
            };
            _db.Files.Add(studentFile);
            await _db.SaveChangesAsync();   // generates FileId

            var payment = new Payment
            {
                ScholarshipBasePayment = dto.Payment.ScholarshipBasePayment,
                SessionsPayed = dto.Payment.SessionsPayed,
                FileId = studentFile.FileId
            };
            _db.Payments.Add(payment);

            await _db.SaveChangesAsync();   // generates PaymentId
            await tx.CommitAsync();

            return Created(
                $"/api/autoschool/{schoolId}/students/{user.Id}",
                new
                {
                    userId = user.Id,
                    fileId = studentFile.FileId,
                    paymentId = payment.PaymentId,
                    message = "Student created successfully"
                });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    // ─────────── GET USER ───────────
    /// <summary>
    /// Returns a single user (Student or Instructor).  
    /// For instructors the response also includes all teaching categories.
    /// </summary>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="userId">
    /// The string ID of the user (exact value returned by the list-users endpoint).
    /// </param>
    /// <response code="200">User found and returned.</response>
    /// <response code="400">Invalid ID supplied or user has an unsupported role.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated admin does not belong to the specified school.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("getUser/{userId}")]
    public async Task<IActionResult> GetUserAsync(int schoolId, string userId)
    {
        // ─── validate & fetch target user ───
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { message = "userId cannot be empty." });

        var user = await _users.Users
                               .AsNoTracking()
                               .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return NotFound(new { message = "User not found" });

        if (user.AutoSchoolId != schoolId) return Forbid();

        var roles = await _users.GetRolesAsync(user);

        // ─── Instructor branch ───
        if (roles.Contains("Instructor"))
        {
            var categories = await (
                from utc in _db.ApplicationUserTeachingCategories
                join tc in _db.TeachingCategories on utc.TeachingCategoryId equals tc.TeachingCategoryId
                join lic in _db.Licenses on tc.LicenseId equals lic.LicenseId
                where utc.UserId == user.Id
                select new TeachingCategoryDto
                {
                    TeachingCategoryId = tc.TeachingCategoryId,
                    SessionCost = tc.SessionCost,
                    SessionDuration = tc.SessionDuration,
                    ScholarshipPrice = tc.ScholarshipPrice,
                    MinDrivingLessonsReq = tc.MinDrivingLessonsReq,
                    LicenseType = lic.Type
                }).ToListAsync();

            return Ok(new InstructorUserDto
            {
                UserId = user.Id,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                Role = "Instructor",
                AutoSchoolId = user.AutoSchoolId ?? 0,
                TeachingCategories = categories
            });
        }

        // ─── Student branch ───
        if (roles.Contains("Student"))
        {
            return Ok(new StudentUserDto
            {
                UserId = user.Id,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                Cnp = user.Cnp ?? string.Empty,
                Role = "Student",
                AutoSchoolId = user.AutoSchoolId ?? 0
            });
        }

        return BadRequest(new { message = "User is neither Student nor Instructor" });
    }



    // ─────────── LIST USERS ───────────
    /// <summary>
    /// Lists every user (Student or Instructor) that belongs to the given school.
    /// </summary>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <response code="200">Array with all users in the school.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated admin does not belong to the specified school.</response>
    [HttpGet("getUsers")]
    public async Task<IActionResult> GetUsersAsync(int schoolId)
    {
        // ─── fetch all users from this school ───
        var users = await _users.Users
            .AsNoTracking()
            .Where(u => u.AutoSchoolId == schoolId)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var allowedRoles = new[] { "Student", "Instructor" };

        // map userId → role but keep only Student & Instructor
        var roleMap = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId) && allowedRoles.Contains(r.Name)
            select new { ur.UserId, r.Name }
        ).ToDictionaryAsync(x => x.UserId, x => x.Name);

        // build payload for users that have one of the allowed roles
        var payload = users
            .Where(u => roleMap.ContainsKey(u.Id))
            .Select(u => new UserListItemDto
            {
                UserId = u.Id,
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                Phone = u.PhoneNumber ?? string.Empty,
                Role = roleMap[u.Id],
                Cnp = roleMap[u.Id] == "Student" ? u.Cnp : null
            });

        return Ok(payload);
    }


    // ─────────── LIST USERS BY TYPE ───────────
    /// <summary>
    /// Returns all users of the specified type (<c>Student</c> or <c>Instructor</c>)
    /// that belong to the given school.
    /// </summary>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="type">Required role name: <c>Instructor</c> or <c>Student</c>.</param>
    /// <response code="200">Array of users matching the requested role.</response>
    /// <response code="400">Missing or invalid type parameter.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated admin does not belong to the specified school.</response>
    [HttpGet("getUsers/{type}")]
    public async Task<IActionResult> GetUsersByTypeAsync(int schoolId, string type)
    {
        // ─── validate "type" input ───
        if (string.IsNullOrWhiteSpace(type))
            return BadRequest(new { message = "User type is required. Use 'Instructor' or 'Student'." });

        type = type.Trim();
        var isValid = type.Equals("Instructor", StringComparison.OrdinalIgnoreCase) ||
                      type.Equals("Student", StringComparison.OrdinalIgnoreCase);

        if (!isValid)
            return BadRequest(new { message = "Type must be 'Instructor' or 'Student'." });

        // ─── query users by role & school ───
        var users = await (
            from u in _users.Users.AsNoTracking()
            join ur in _db.UserRoles on u.Id equals ur.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            where r.Name == type && u.AutoSchoolId == schoolId
            select new UserListItemDto
            {
                UserId = u.Id,
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                Phone = u.PhoneNumber ?? string.Empty,
                Role = type,
                Cnp = type.Equals("Student", StringComparison.OrdinalIgnoreCase) ? u.Cnp : null
            }).ToListAsync();

        return Ok(users);
    }


    // ─────────── UPDATE STUDENT ───────────
    /// <summary>
    /// Updates basic data for an existing student.  
    /// If <c>password</c> is supplied, it is reset via Identity.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request body</strong></para>
    /// 
    /// ```json
    /// {
    ///   "firstName": "Ioana",
    ///   "lastName":  "Marin",
    ///   "email":     "ioana.updated@student.ro",
    ///   "cnp":       "2990101223344",
    ///   "phone":     "0734567899",
    ///   "password":  "*Newpass123"
    /// }
    /// ```
    /// </remarks>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="userId">Student's GUID string from the route.</param>
    /// <param name="dto">Fields to update.</param>
    /// <response code="200">Student updated successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Student belongs to a different school.</response>
    /// <response code="404">Student not found.</response>
    [HttpPut("update/student/{userId}")]
    public async Task<IActionResult> UpdateStudentAsync(
      int schoolId,
      string userId,
      [FromBody] UpdateStudentDto dto)
    {
        // quick validation
        if (dto.Cnp?.Length != 13 || !dto.Cnp.All(char.IsDigit))
            return BadRequest(new { message = "CNP must contain exactly 13 numeric digits." });

        var user = await _users.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return NotFound(new { message = "Student not found" });

        if (user.AutoSchoolId != schoolId) return Forbid();

        var roles = await _users.GetRolesAsync(user);
        if (!roles.Contains("Student"))
            return BadRequest(new { message = "User is not a student" });

        // uniqueness checks (exclude current user)
        if (await _users.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId))
            return BadRequest(new { message = "E-mail already used by another user" });

        if (await _users.Users.AnyAsync(u => u.Cnp == dto.Cnp && u.Id != userId))
            return BadRequest(new { message = "CNP already used by another user" });

        // update scalar fields
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.UserName = dto.Email;
        user.PhoneNumber = dto.Phone;
        user.Cnp = dto.Cnp;

        // password reset if provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var passRes = await _users.ResetPasswordAsync(user, token, dto.Password);
            if (!passRes.Succeeded)
                return BadRequest(new { message = "Password reset failed", errors = passRes.Errors });
        }

        var res = await _users.UpdateAsync(user);
        if (!res.Succeeded)
            return BadRequest(new { message = "Identity update failed", errors = res.Errors });

        return Ok(new { message = "Student updated successfully" });
    }



    // ─────────── UPDATE INSTRUCTOR ───────────
    /// <summary>
    /// Updates an instructor's personal data and replaces their teaching-category list.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request body</strong></para>
    ///
    /// ```json
    /// {
    ///   "firstName": "Vasile",
    ///   "lastName":  "Alexandru",
    ///   "email":     "andrei.popescu@school.ro",
    ///   "phone":     "0723456789",
    ///   "password":  "*Parola123",
    ///   "teachingCategoryIds": [1]
    /// }
    /// ```
    /// Any scalar field you omit (e.g. <c>phone</c>) is left unchanged.  
    /// <c>teachingCategoryIds</c> is a definitive list: the existing links are fully replaced.
    /// </remarks>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="userId">Instructor's GUID string from the route.</param>
    /// <param name="dto">Fields to update.</param>
    /// <response code="200">Instructor updated successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Instructor belongs to a different school.</response>
    /// <response code="404">Instructor not found.</response>
    [HttpPut("update/instructor/{userId}")]
    public async Task<IActionResult> UpdateInstructorAsync(
    int schoolId,
    string userId,
    [FromBody] UpdateInstructorDto dto)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return NotFound(new { message = "Instructor not found" });

        if (user.AutoSchoolId != schoolId) return Forbid();

        var roles = await _users.GetRolesAsync(user);
        if (!roles.Contains("Instructor"))
            return BadRequest(new { message = "User is not an instructor" });

        // validate teachingCategoryIds (can be null → keep current)
        if (dto.TeachingCategoryIds is { Count: > 0 })
        {
            var validIds = await _db.TeachingCategories
                .Where(tc => tc.AutoSchoolId == schoolId &&
                             dto.TeachingCategoryIds.Contains(tc.TeachingCategoryId))
                .Select(tc => tc.TeachingCategoryId)
                .ToListAsync();

            if (validIds.Count != dto.TeachingCategoryIds.Distinct().Count())
                return BadRequest(new { message = "One or more teachingCategoryIds are invalid for this school" });
        }

        // uniqueness check for new email (exclude current user)
        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            await _users.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId))
            return BadRequest(new { message = "E-mail already used by another user" });

        // apply scalar updates only if provided
        if (!string.IsNullOrWhiteSpace(dto.FirstName)) user.FirstName = dto.FirstName;
        if (!string.IsNullOrWhiteSpace(dto.LastName)) user.LastName = dto.LastName;
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            user.Email = dto.Email;
            user.UserName = dto.Email;
        }
        if (!string.IsNullOrWhiteSpace(dto.Phone)) user.PhoneNumber = dto.Phone;

        // password reset if supplied
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var pRes = await _users.ResetPasswordAsync(user, token, dto.Password);
            if (!pRes.Succeeded)
                return BadRequest(new { message = "Password reset failed", errors = pRes.Errors });
        }

        // start transaction for categories + user update
        await using var tx = await _db.Database.BeginTransactionAsync();

        var uRes = await _users.UpdateAsync(user);
        if (!uRes.Succeeded)
        {
            await tx.RollbackAsync();
            return BadRequest(new { message = "Identity update failed", errors = uRes.Errors });
        }

        if (dto.TeachingCategoryIds is { })
        {
            // remove existing links
            var oldLinks = _db.ApplicationUserTeachingCategories.Where(x => x.UserId == userId);
            _db.ApplicationUserTeachingCategories.RemoveRange(oldLinks);

            // add new links (if any)
            foreach (var id in dto.TeachingCategoryIds.Distinct())
                _db.ApplicationUserTeachingCategories.Add(
                    new ApplicationUserTeachingCategory { UserId = userId, TeachingCategoryId = id });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { message = "Instructor updated successfully" });
    }


    // ─────────── DELETE USER ───────────
    /// <summary>
    /// Deletes a student or instructor that belongs to the specified school.
    /// SchoolAdmin accounts cannot be removed.
    /// </summary>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="userId">User GUID returned by the list-users endpoint.</param>
    /// <response code="204">User deleted successfully.</response>
    /// <response code="400">Cannot delete SchoolAdmin or unsupported role / validation failed.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User belongs to a different school.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("deleteUser/{userId}")]
    public async Task<IActionResult> DeleteUserAsync(int schoolId, string userId)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return NotFound(new { message = "User not found" });

        if (user.AutoSchoolId != schoolId)
            return Forbid();

        var roles = await _users.GetRolesAsync(user);
        if (roles.Contains("SchoolAdmin"))
            return BadRequest(new { message = "Cannot delete a SchoolAdmin account" });

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (roles.Contains("Student"))
        {
            var files = _db.Files.Where(f => f.StudentId == userId).ToList();
            var fileIds = files.Select(f => f.FileId).ToList();
            var payments = _db.Payments.Where(p => fileIds.Contains(p.FileId));
            var appointments = _db.Appointments
                                  .Where(a => a.FileId != null && fileIds.Contains(a.FileId!.Value));

            _db.Payments.RemoveRange(payments);
            _db.Appointments.RemoveRange(appointments);
            _db.Files.RemoveRange(files);
        }
        else if (roles.Contains("Instructor"))
        {
            var utc = _db.ApplicationUserTeachingCategories.Where(t => t.UserId == userId);
            var avail = _db.InstructorAvailabilities.Where(a => a.InstructorId == userId);

            var files = _db.Files.Where(f => f.InstructorId == userId).ToList();
            var fileIds = files.Select(f => f.FileId).ToList();
            var appointments = _db.Appointments
                                  .Where(a => a.FileId != null && fileIds.Contains(a.FileId!.Value));

            _db.ApplicationUserTeachingCategories.RemoveRange(utc);
            _db.InstructorAvailabilities.RemoveRange(avail);
            _db.Appointments.RemoveRange(appointments);
            _db.Files.RemoveRange(files);
        }
        else
        {
            return BadRequest(new { message = "User role not supported for deletion" });
        }

        var deleteResult = await _users.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            await tx.RollbackAsync();
            return BadRequest(new { message = "Identity deletion failed", errors = deleteResult.Errors });
        }

        await tx.CommitAsync();
        return NoContent();   
    }

}

// ─────────────────────── DTOs ───────────────────────

/// <summary>
/// Payload sent to <see cref="SchoolAdminController.CreateInstructorAsync"/>.
/// </summary>
public sealed class InstructorCreateDto
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Password { get; init; } = default!;
    public List<int> TeachingCategoryIds { get; init; } = new();
}

/// <summary>
/// Composite payload sent to <see cref="SchoolAdminController.CreateStudentAsync"/>.
/// Contains nested objects for the student, the first file, and the initial payment.
/// </summary>
public sealed class StudentCreateDto
{
    public StudentDto Student { get; init; } = new();
    public PaymentDto Payment { get; init; } = new();
    public FileDto File { get; init; } = new();
}

/// <summary>
/// Personal data for a student inside <see cref="StudentCreateDto"/>.
/// </summary>
public sealed class StudentDto
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Cnp { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Password { get; init; } = default!;
}

/// <summary>
/// Payment section of <see cref="StudentCreateDto"/>.
/// </summary>
public sealed class PaymentDto
{
    public bool ScholarshipBasePayment { get; init; }
    public int SessionsPayed { get; init; }
}

/// <summary>
/// Initial file section of <see cref="StudentCreateDto"/>.
/// </summary>
public sealed class FileDto
{
    public DateTime ScholarshipStartDate { get; init; }
    public DateTime CriminalRecordExpiryDate { get; init; }
    public DateTime MedicalRecordExpiryDate { get; init; }
    public string Status { get; init; } = default!;
    public int? InstructorId { get; init; }
    public int? VehicleId { get; init; }
    public int? TeachingCategoryId { get; init; }
}

/// <summary>
/// Response shape when the fetched user has role <c>Student</c>.
/// </summary>
public sealed class StudentUserDto
{
    public required string UserId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Cnp { get; init; }
    public required string Role { get; init; }
    public required int AutoSchoolId { get; init; }
}

/// <summary>
/// Response shape when the fetched user has role <c>Instructor</c>.
/// Includes the categories they teach.
/// </summary>
public sealed class InstructorUserDto
{
    public required string UserId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Role { get; init; }
    public required int AutoSchoolId { get; init; }
    public List<TeachingCategoryDto> TeachingCategories { get; init; } = new();
}

/// <summary>
/// Details for a single teaching category returned in <see cref="InstructorUserDto"/>.
/// </summary>
public sealed class TeachingCategoryDto
{
    public int TeachingCategoryId { get; init; }
    public decimal SessionCost { get; init; }
    public int SessionDuration { get; init; }
    public decimal ScholarshipPrice { get; init; }
    public int MinDrivingLessonsReq { get; init; }
    public string LicenseType { get; init; } = default!;
}


/// <summary>
/// Lightweight representation used by <see cref="SchoolAdminController.GetUsersAsync"/>.
/// <c>Cnp</c> is included only for students (null for instructors).
/// </summary>
public sealed class UserListItemDto
{
    public string UserId { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Role { get; init; } = default!;
    public string? Cnp { get; init; }        // null for instructors
}

/// <summary>
/// Body used by <see cref="SchoolAdminController.UpdateStudentAsync"/>.
/// </summary>
public sealed class UpdateStudentDto
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Cnp { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string? Password { get; init; }
}



/// <summary>
/// Body used by <see cref="SchoolAdminController.UpdateInstructorAsync"/>.
/// Omitted properties are ignored (current value kept).
/// </summary>
public sealed class UpdateInstructorDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Password { get; init; }
    public List<int>? TeachingCategoryIds { get; init; }
}