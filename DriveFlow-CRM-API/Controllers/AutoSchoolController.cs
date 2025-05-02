using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DriveFlow_CRM_API.Models;
using System.Data;


namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutoSchoolController : ControllerBase
{
    // ───────────────────────────── fields & ctor ─────────────────────────────
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    /// <summary>Constructor invoked per request by DI.</summary>
    public AutoSchoolController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }
    // ────────────────────────────── GET AUTO-SCHOOLS ──────────────────────────────
    /// <summary>
    /// Returns every driving-school together with its <c>SchoolAdmin</c> and full
    /// address (SuperAdmin only).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "autoSchoolId": 1,
    ///     "name": "DriveFlow",
    ///     "description": "Headquarters – central branch",
    ///     "website": "https://driveflow.ro",
    ///     "phoneNumber": "0723111222",
    ///     "email": "contact@driveflow.ro",
    ///     "status": "Active",
    ///     "address": {
    ///       "addressId": 30,
    ///       "streetName": "Dorobanților",
    ///       "addressNumber": "24",
    ///       "postcode": "400117",
    ///       "city": {
    ///         "cityId": 10,
    ///         "name": "Cluj-Napoca",
    ///         "county": {
    ///           "countyId": 1,
    ///           "name": "Cluj",
    ///           "abbreviation": "CJ"
    ///         }
    ///       }
    ///     },
    ///     "schoolAdmin": {
    ///       "userId": "71",
    ///       "firstName": "Ana",
    ///       "lastName": "Pop",
    ///       "email": "ana.pop@driveflow.ro",
    ///       "phone": "0744555666"
    ///     }
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <response code="200">
    /// Array of auto-schools returned successfully.
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">
    /// Authenticated user is not a <c>SuperAdmin</c>.
    /// </response>
    // ────────────────────────────── GET AUTO-SCHOOLS ──────────────────────────────
    [HttpGet("get")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAutoSchoolsAsync()
    {
        var schoolAdminRole = await _roles.FindByNameAsync("SchoolAdmin");
        if (schoolAdminRole is null)
            return Ok(Array.Empty<AutoSchoolDto>());   

        var roleId = schoolAdminRole.Id;

        var schools = await (
            from s in _db.AutoSchools
                           .Include(a => a.Address)
                                .ThenInclude(ad => ad!.City)
                                    .ThenInclude(ci => ci!.County)
                           .AsNoTracking()

            join u in _db.Users.AsNoTracking() on s.AutoSchoolId equals u.AutoSchoolId
            join ur in _db.UserRoles on u.Id equals ur.UserId
            where ur.RoleId == roleId                     
            orderby s.Name

            select new AutoSchoolDto
            {
                AutoSchoolId = s.AutoSchoolId,
                Name = s.Name,
                Description = s.Description,
                WebSite = s.WebSite,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email ?? string.Empty,
                Status = s.Status.ToString().ToLowerInvariant(),

                Address = s.Address == null ? null : new AddressDto
                {
                    AddressId = s.Address.AddressId,
                    StreetName = s.Address.StreetName,
                    AddressNumber = s.Address != null && s.Address.AddressNumber != null? s.Address.AddressNumber: string.Empty,
                    Postcode = s.Address != null && s.Address.Postcode != null ? s.Address.Postcode : string.Empty,
                    City = s.Address == null || s.Address.City == null? null: new CityDto
                    {
                    CityId = s.Address.City.CityId,
                    Name = s.Address.City.Name,
                    County = s.Address.City.County == null ? null : new CountyDto
                    {
                        CountyId = s.Address.City.County.CountyId,
                        Name = s.Address.City.County.Name,
                        Abbreviation = s.Address.City.County.Abbreviation
                    }
                    },

                },

                SchoolAdmin = new SchoolAdminInfoDto
                {
                    UserId = u.Id,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Phone = u.PhoneNumber ?? string.Empty
                }
            })
            .Distinct()    
            .ToListAsync();

        return Ok(schools);
    }
    // ────────────────────────────── POST AUTO-SCHOOL ──────────────────────────────
    /// <summary>
    /// Creates a new driving-school together with its <c>SchoolAdmin</c> account
    /// (SuperAdmin only).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request body</strong></para>
    ///
    /// ```json
    /// {
    ///   "autoSchool": {
    ///     "name": "Start-Drive",
    ///     "description": "New branch",
    ///     "website": "https://startdrive.ro",
    ///     "phoneNumber": "0733000111",
    ///     "email": "office@startdrive.ro",
    ///     "status": "Restricted",
    ///     "addressId": 42
    ///   },
    ///   "schoolAdmin": {
    ///     "firstName": "Mihai",
    ///     "lastName":  "Ionescu",
    ///     "email":     "mihai.ionescu@startdrive.ro",
    ///     "phone":     "0722333444",
    ///     "password":  "AdminPass1!"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <response code="201">Auto-school created successfully.</response>
    /// <response code="400">
    /// Validation failed (duplicates, missing fields, invalid <c>addressId</c>, bad status).
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user is not a <c>SuperAdmin</c>.</response>
    [HttpPost("create")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateAutoSchoolAsync([FromBody] CreateAutoSchoolDto dto)
    {
        // ─────────────── basic validation ───────────────
        if (dto is null ||
            dto.AutoSchool is null ||
            dto.SchoolAdmin is null)
            return BadRequest(new { message = "Request body must contain both autoSchool and schoolAdmin objects." });

        var s = dto.AutoSchool;
        var a = dto.SchoolAdmin;

        // required scalar fields
        if (string.IsNullOrWhiteSpace(s.Name) || string.IsNullOrWhiteSpace(s.Email) ||
            string.IsNullOrWhiteSpace(s.PhoneNumber) || string.IsNullOrWhiteSpace(s.Status) ||
            string.IsNullOrWhiteSpace(a.FirstName) || string.IsNullOrWhiteSpace(a.LastName) ||
            string.IsNullOrWhiteSpace(a.Email) || string.IsNullOrWhiteSpace(a.Phone) ||
            string.IsNullOrWhiteSpace(a.Password) || s.AddressId <= 0)
        {
            return BadRequest(new { message = "All required fields must be supplied and addressId must be positive." });
        }

        // parse status
        if (!Enum.TryParse<AutoSchoolStatus>(s.Status, true, out var statusEnum))
            return BadRequest(new
            {
                message = $"Invalid status. Allowed values: {string.Join(", ", Enum.GetNames(typeof(AutoSchoolStatus)))}"
            });

        // duplicates
        if (await _db.AutoSchools.AnyAsync(x => x.Email == s.Email))
            return BadRequest(new { message = "A driving school with the given e-mail already exists." });

        if (await _users.Users.AnyAsync(x => x.Email == a.Email))
            return BadRequest(new { message = "A user with the given admin e-mail already exists." });

        // address must exist
        if (!await _db.Addresses.AnyAsync(ad => ad.AddressId == s.AddressId))
            return BadRequest(new { message = "addressId does not reference an existing address." });

        // ─────────────── transaction ───────────────
        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            // insert school
            var school = new AutoSchool
            {
                Name = s.Name.Trim(),
                Description = s.Description?.Trim(),
                WebSite = s.WebSite?.Trim(),
                PhoneNumber = s.PhoneNumber.Trim(),
                Email = s.Email.Trim(),
                Status = statusEnum,
                AddressId = s.AddressId
            };

            _db.AutoSchools.Add(school);
            await _db.SaveChangesAsync();   // generates AutoSchoolId

            // create admin user
            var admin = new ApplicationUser
            {
                FirstName = a.FirstName.Trim(),
                LastName = a.LastName.Trim(),
                Email = a.Email.Trim(),
                UserName = a.Email.Trim(),
                PhoneNumber = a.Phone.Trim(),
                AutoSchoolId = school.AutoSchoolId
            };

            var idRes = await _users.CreateAsync(admin, a.Password);
            if (!idRes.Succeeded)
            {
                await tx.RollbackAsync();
                return BadRequest(new { message = "Identity creation failed", errors = idRes.Errors });
            }

            // ensure SchoolAdmin role exists & assign
            if (!await _roles.RoleExistsAsync("SchoolAdmin"))
                await _roles.CreateAsync(new IdentityRole("SchoolAdmin"));

            await _users.AddToRoleAsync(admin, "SchoolAdmin");

            await tx.CommitAsync();

            return Created(
                $"/api/autoschool/{school.AutoSchoolId}",
                new
                {
                    autoSchoolId = school.AutoSchoolId,
                    schoolAdminUserId = admin.Id,
                    message = "Auto school created successfully"
                });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
    // ────────────────────────────── PUT AUTO-SCHOOL ──────────────────────────────
    /// <summary>
    /// Updates an existing driving-school.  
    /// Accessible to a <c>SuperAdmin</c> **or** that school’s own <c>SchoolAdmin</c>.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request body</strong></para>
    ///
    /// ```json
    /// {
    ///   "name":        "DriveFlow HQ",
    ///   "description": "Renovated branch – more classrooms",
    ///   "website":     "https://driveflow.ro",
    ///   "phoneNumber": "0723111333",
    ///   "email":       "hq@driveflow.ro",
    ///   "status":      "Demo",
    ///   "addressId":  2
    /// }
    /// ```
    ///
    /// <ul>
    ///   <li>If the caller is a <c>SuperAdmin</c>, all fields above may be modified.</li>
    ///   <li>If the caller is the school’s own <c>SchoolAdmin</c>, any values supplied
    ///       for <c>addressId</c> or <c>status</c> are <em>ignored</em>.</li>
    ///   <li>Omit a property (or set it to <c>null</c>) to keep the current value.</li>
    /// </ul>
    /// </remarks>
    /// <param name="autoSchoolId">Identifier of the school to update (from the route).</param>
    /// <param name="dto">Patch-style body containing only the fields to change.</param>
    /// <response code="200">Auto-school updated successfully.</response>
    /// <response code="400">
    /// Validation failed (duplicates, invalid status, or <c>SchoolAdmin</c> attempted to
    /// change <c>addressId</c>/<c>status</c>).
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">
    /// Caller is a <c>SchoolAdmin</c> of another school, or is missing required role.
    /// </response>
    /// <response code="404">Auto-school not found.</response>
    [HttpPut("update/{autoSchoolId:int}")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> UpdateAutoSchoolAsync(
    int autoSchoolId,
    [FromBody] AutoSchoolUpdateDto dto)
    {
        if (dto is null)
            return BadRequest(new { message = "Request body cannot be empty." });

        // ─── fetch target ───
        var school = await _db.AutoSchools.FindAsync(autoSchoolId);
        if (school is null)
            return NotFound(new { message = "Auto-school not found" });

        var callerIsSuperAdmin = User.IsInRole("SuperAdmin");
        var callerIsSchoolAdmin = User.IsInRole("SchoolAdmin");

        // ─── SchoolAdmin may update only its own school ───
        if (callerIsSchoolAdmin)
        {
            var callerId = User.FindFirst("userId")?.Value;
            var caller = await _users.Users.AsNoTracking()
                                               .FirstOrDefaultAsync(u => u.Id == callerId);

            if (caller?.AutoSchoolId != autoSchoolId)
                return Forbid();
        }

        // ─── uniqueness check for e-mail ───
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var duplicate = await _db.AutoSchools.AnyAsync(a =>
                a.AutoSchoolId != autoSchoolId && a.Email == dto.Email.Trim());

            if (duplicate)
                return BadRequest(new { message = "Another auto-school already uses the given e-mail." });
        }

        // ─── patch scalar fields ───
        if (!string.IsNullOrWhiteSpace(dto.Name)) school.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Description)) school.Description = dto.Description.Trim();
        if (!string.IsNullOrWhiteSpace(dto.WebSite)) school.WebSite = dto.WebSite.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) school.PhoneNumber = dto.PhoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email)) school.Email = dto.Email.Trim();

        // ─── Status & AddressId (SuperAdmin only) ───
        if (callerIsSuperAdmin)
        {
            // status
            if (dto.Status is { })
            {
                if (!Enum.TryParse<AutoSchoolStatus>(dto.Status, true, out var st))
                    return BadRequest(new { message = "Invalid status value." });

                school.Status = st;
            }

            // address
            if (dto.AddressId is { })
            {
                var addrExists = await _db.Addresses.AnyAsync(a => a.AddressId == dto.AddressId.Value);
                if (!addrExists)
                    return BadRequest(new { message = "Address with the specified ID does not exist." });

                school.AddressId = dto.AddressId.Value;
            }
        }
        // SchoolAdmin callers: any supplied status/addressId are silently ignored.

        await _db.SaveChangesAsync();
        return Ok(new { message = "Auto school updated successfully" });
    }

    // ────────────────────────────── DELETE AUTO-SCHOOL ──────────────────────────────
    /// <summary>
    /// Deletes a driving-school (SuperAdmin only).
    /// </summary>
    /// <response code="204">Auto-school deleted successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user is not a <c>SuperAdmin</c>.</response>
    /// <response code="404">Auto-school not found.</response>
    [HttpDelete("delete/{autoSchoolId:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteAutoSchoolAsync(int autoSchoolId)
    {
        var school = await _db.AutoSchools.FindAsync(autoSchoolId);
        if (school is null)
            return NotFound(new { message = "Auto-school not found" });

        _db.AutoSchools.Remove(school);
        await _db.SaveChangesAsync();

        return NoContent();   
    }

    // ─────────────────── PUT – UPDATE SCHOOL ADMIN USER ───────────────────
    /// <summary>
    /// Updates the personal data (and optionally the password) of an existing
    /// <c>SchoolAdmin</c> account.  
    /// Accessible only to a user with the <c>SuperAdmin</c> role.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request body</strong></para>
    ///
    /// ```json
    /// {
    ///   "firstName": "Ana-Maria",
    ///   "lastName":  "Pop",
    ///   "email":     "ana.pop@driveflow.ro",
    ///   "phone":     "0744555666",
    ///   "password":  "NewPass2025!"
    /// }
    /// ```
    ///
    /// <ul>
    ///   <li>Omit <c>password</c> (or set it to <c>null</c>) if you don’t want to
    ///       reset the current credential.</li>
    ///   <li>The <c>email</c> must be unique across all users.</li>
    /// </ul>
    /// </remarks>
    /// <param name="userId">
    /// Identifier of the SchoolAdmin to update (GUID string from the route).
    /// </param>
    /// <param name="dto">
    /// Patch body containing only the fields you want to change; unspecified
    /// properties remain unchanged.
    /// </param>
    /// <response code="200">School admin updated successfully.</response>
    /// <response code="400">
    /// Validation failed (duplicate e-mail, weak password, etc.).
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">
    /// Authenticated user is not a <c>SuperAdmin</c>.
    /// </response>
    /// <response code="404">
    /// Target user not found or does not have the <c>SchoolAdmin</c> role.
    /// </response>
    // ─────────────────── PUT – UPDATE SCHOOL ADMIN USER ───────────────────
    [HttpPut("updateSchoolAdmin/{userId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateSchoolAdminAsync(
        string userId,
        [FromBody] SchoolAdminUpdateDto dto)
    {
        if (dto is null)
            return BadRequest(new { message = "Request body cannot be empty." });

        // ─── target user must exist & be SchoolAdmin ───
        var target = await _users.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (target is null)
            return NotFound(new { message = "User not found" });

        var roles = await _users.GetRolesAsync(target);
        if (!roles.Contains("SchoolAdmin"))
            return NotFound(new { message = "User is not a SchoolAdmin" });

        // ─── e-mail must stay unique (exclude current user) ───
        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            await _users.Users.AnyAsync(u => u.Email == dto.Email.Trim() && u.Id != userId))
            return BadRequest(new { message = "E-mail already used by another user" });

        // ─── patch scalar fields ───
        if (!string.IsNullOrWhiteSpace(dto.FirstName)) target.FirstName = dto.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.LastName)) target.LastName = dto.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            target.Email = dto.Email.Trim();
            target.UserName = dto.Email.Trim();
        }
        if (!string.IsNullOrWhiteSpace(dto.Phone)) target.PhoneNumber = dto.Phone.Trim();

        // ─── optional password reset ───
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(target);
            var pRes = await _users.ResetPasswordAsync(target, token, dto.Password);
            if (!pRes.Succeeded)
                return BadRequest(new { message = "Password reset failed", errors = pRes.Errors });
        }

        // ─── persist ───
        var res = await _users.UpdateAsync(target);
        if (!res.Succeeded)
            return BadRequest(new { message = "Identity update failed", errors = res.Errors });

        return Ok(new { message = "School admin updated successfully" });
    }

}

// ─────────────────────── DTOs ───────────────────────
/// <summary>
/// Lightweight SchoolAdmin info included inside <see cref="AutoSchoolDto"/>.
/// </summary>
public sealed class SchoolAdminInfoDto
{
    public string UserId { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Phone { get; init; } = default!;
}
/// <summary>
/// Response shape of <c>GET&#160;api/autoschool/get</c>
/// (school details + address + its SchoolAdmin).
/// </summary>
public sealed class AutoSchoolDto
{
    public int AutoSchoolId { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string? WebSite { get; init; }
    public string? PhoneNumber { get; init; }
    public string Email { get; init; } = default!;
    public string Status { get; init; } = default!;
    public AddressDto? Address { get; init; } = default!;
    public SchoolAdminInfoDto? SchoolAdmin { get; init; } = default!;
}

/// <summary>
/// Body for <c>POST&#160;api/autoschool/create</c>: driving-school data plus its initial SchoolAdmin.
/// </summary>
public sealed class AutoSchoolCreateDto
{
    public AutoSchoolInnerDto AutoSchool { get; init; } = new();
    public SchoolAdminCreateDto SchoolAdmin { get; init; } = new();
}

/// <summary>
/// Business data sent in the <c>autoSchool</c> section of
/// <see cref="AutoSchoolCreateDto"/> when creating a new driving-school.
/// No identifier field is included; the primary key is generated by the database.
/// </summary>

public sealed class AutoSchoolInnerDto
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string? WebSite { get; init; }
    public string? PhoneNumber { get; init; }
    public string Email { get; init; } = default!;
    public string? Status { get; init; }
    public int AddressId { get; init; }
}

/// <summary>
/// Data used to create the initial <c>SchoolAdmin</c> account for a new school.
/// Appears as the <c>schoolAdmin</c> node inside <see cref="AutoSchoolCreateDto"/>.
/// </summary>

public sealed class SchoolAdminCreateDto
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Password { get; init; } = default!;
}

/// <summary>
/// Patch body for <c>PUT&#160;api/autoschool/update/{id}</c>.
/// Send only fields to change; others stay as-is.  
/// <c>AddressId</c> and <c>Status</c> are ignored if caller is a SchoolAdmin.
/// </summary>
public sealed class AutoSchoolUpdateDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? WebSite { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Status { get; set; }  
    public int? AddressId { get; set; }
}
/// <summary>
/// Patch body for <c>PUT&#160;api/user/updateSchoolAdmin/{id}</c>;  
/// include only the fields you want updated.  
/// Setting <c>Password</c> performs a reset.
/// </summary>
public sealed class SchoolAdminUpdateDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Password { get; init; }
}
/// <summary>
/// Composite payload for <c>POST&#160;api/autoschool/create</c>.  
/// It nests:
/// <list type="bullet">
///   <item><c>AutoSchool</c> – the business data of the new driving-school.</item>
///   <item><c>SchoolAdmin</c> – the first <c>SchoolAdmin</c> account to be created.</item>
/// </list>
/// </summary>
public sealed class CreateAutoSchoolDto
{
    public NewAutoSchoolDto AutoSchool { get; init; } = new();
    public NewSchoolAdminDto SchoolAdmin { get; init; } = new();
}

/// <summary>
/// Fields inside the <c>AutoSchool</c> node of <see cref="CreateAutoSchoolDto"/>.  
/// No primary-key value is supplied; <c>autoSchoolId</c> is generated by the database
/// when the record is inserted.
/// </summary>
public sealed class NewAutoSchoolDto
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string? WebSite { get; init; }
    public string PhoneNumber { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int AddressId { get; init; }
}

/// <summary>
/// Contact details and credentials used to provision the initial
/// <c>SchoolAdmin</c> user for a newly created driving-school.  
/// Appears as the <c>SchoolAdmin</c> node inside <see cref="CreateAutoSchoolDto"/>.
/// </summary>
public sealed class NewSchoolAdminDto
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Password { get; init; } = default!;
}
