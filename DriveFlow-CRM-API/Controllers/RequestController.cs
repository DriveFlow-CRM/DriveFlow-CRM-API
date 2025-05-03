using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestController : ControllerBase
{

    // ───────────────────────────── fields & ctor ─────────────────────────────
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    /// <summary>Constructor invoked per request by DI.</summary>
    public RequestController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }






    // ──────────────────────────────FETCH SCHOOL REQUESTS ──────────────────────────────
    /// <summary>Returns all student enrollment requests for the appropriate school id, (SchoolAdmin, SuperAdmin only).</summary>
    /// <remarks> SchoolAdmin's SchoolId must match the AutoSchoolId given as method paramether
    /// </remarks>
    /// <response code="200">Requests Array returned successfully.</response>
    /// <response code="400">School id was not a valid value</response>>
    /// <response code="401">User was not authorized</response>
    /// <response code="403">User is forbidden from seeing the requests of this auto school.</response>

    [HttpGet("get/{AutoSchoolId:int}")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> FetchSchoolRequests(int AutoSchoolId)
    {
        if (AutoSchoolId <= 0)
            return BadRequest("AutoSchoolId must be a positive integer.");

        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        if (!(User.IsInRole("SchoolAdmin") && user.AutoSchoolId == AutoSchoolId))
            return Forbid("You are not authorized to view this school's requests.");

        var Requests = await _db.Requests
            .AsNoTracking()
            .Where(r => r.AutoSchoolId == AutoSchoolId)
            .OrderBy(r => r.RequestId)
            .Select(r => new RequestDto
            {
                RequestId = r.RequestId,
                FirstName = r.FirstName,
                LastName = r.LastName,
                DrivingCategory = r.DrivingCategory,
                Status = r.Status,
            })
            .ToListAsync();
        return Ok(Requests);
    }


}


public sealed class RequestDto
{
    public int RequestId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string? DrivingCategory { get; init; } = default!;
    public string Status { get; init; } = default!;
}