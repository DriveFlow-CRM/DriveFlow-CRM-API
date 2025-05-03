using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/request")]
public class RequestController : ControllerBase
{

    // ───────────────────────────── fields & actor ─────────────────────────────
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




    // ────────────────────────────── CREATE REQUEST ──────────────────────────────

    /// <summary>Create a new enrollment Request for someone wishing to start their courses
    /// (Student, SchoolAdmin, SuperAdmin only).</summary>
    /// <remarks> SchoolAdmin's SchoolId must match the AutoSchoolId given as method paramether.
    /// <para> <strong>Sample Request body</strong> </para> 
    /// ```json
    ///
    ///{
    ///  "firstName": "Maria",
    ///  "lastName": "Ionescu",
    ///  "phoneNr": "0721234234",
    ///  "drivingCategory": "A2",
    ///}
    /// ```
    /// </remarks>
    /// <response code="200">Request sent succesffully.</response>
    /// <response code="400">Empty request</response>>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is forbidden from seeing the requests of this auto school.</response>

    [HttpPost("school/{schoolId}/createRequest")]
    public async Task<IActionResult> CreateRequest(int schoolId, [FromBody] RequestDto requestDto)
    {
        if(schoolId <= 0)
            return BadRequest("Invalid school ID.");
        if (requestDto == null)
            return BadRequest("Request data is required.");

        //if (User.IsInRole("SchoolAdmin") &&  user.AutoSchoolId != requestDto.AutoSchoolId)
        //    return Forbid("You are not authorized to create requests for this auto school.");


        var newRequest = new Request
        {
            FirstName = requestDto.FirstName,
            LastName =requestDto.LastName,
            PhoneNumber = requestDto.PhoneNr,
            DrivingCategory = requestDto.DrivingCategory,
            RequestDate = DateTime.Today,
            Status = "PENDING",
            AutoSchoolId = schoolId,
        };
        await _db.Requests.AddAsync(newRequest);
        await _db.SaveChangesAsync();
        return Ok("Request was sent successfully!");
    }







    /// ────────────────────────────── FETCH SCHOOL REQUESTS ──────────────────────────────
    /// <summary>Returns all student enrollment requests for the appropriate school id, (SchoolAdmin, SuperAdmin only).</summary>
    /// <remarks>
    /// If the user is a SchoolAdmin, then his SchoolId must match the parameter SchoolId.
    /// 
    /// <para> <strong>Sample output</strong> </para> 
    /// ```json
    ///
    ///  [
    ///    {
    ///    "requestId": 91249,
    ///    "firstName": "Maria",
    ///    "lastName": "Ionescu",
    ///    "phoneNr": "0721234567",
    ///    "drivingCategory": "A2",
    ///    "requestDate": "2025-10-12",
    ///    "status": "PENDING"
    ///  },
    ///  {
    ///    "requestId": 23523,
    ///    "firstName": "Ion",
    ///    "lastName": "Popescu",
    ///    "phoneNr": "0729876543",
    ///    "drivingCategory": "B",
    ///    "requestDate": "2025-10-15",
    ///    "status": "APPROVED"
    ///  },
    ///  {
    ///    "requestId": 34567,
    ///    "firstName": "Elena",
    ///    "lastName": "Georgescu",
    ///    "phoneNr": "0734567890",
    ///    "drivingCategory": "C",
    ///    "requestDate": "2025-10-20",
    ///    "status": "REJECTED"
    ///  }
    ///]
    /// ```
    /// </remarks>
    /// <returns>
    /// A list of all the requested RequestDTO items.
    /// </returns>
    /// <response code="200">Requests Array returned successfully.</response>
    /// <response code="400">School id was not a valid value</response>>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is forbidden from seeing the requests of this auto school.</response>

    [HttpGet("school/{AutoSchoolId}/fetchSchoolRequests")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> FetchSchoolRequests(int AutoSchoolId)
    {
        if (AutoSchoolId <= 0)
            return BadRequest();

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
                PhoneNr = r.PhoneNumber,
                DrivingCategory = r.DrivingCategory,
                RequestDate = r.RequestDate,
                Status = r.Status
            })
            .ToListAsync();
        return Ok(Requests);
    }


    // ────────────────────────────── UPDATE REQUEST ──────────────────────────────
    /// <summary>Update the status of a request (SchoolAdmin, SuperAdmin only).</summary>
    /// <remarks>
    /// If the user is a SchoolAdmin, then his SchoolId must match the parameter SchoolId.
    /// The only status values allowed are: APPROVED, REJECTED, PENDING.
    /// That's the only thing that is going to be changed.
    /// <para> <strong>Sample request body</strong> </para> 
    /// ```json
    ///
    ///  {
    ///    "firstName": "Maria",
    ///    "lastName": "Ionescu",
    ///    "phoneNr": "0721234234",
    ///    "drivingCategory": "A2",
    ///    "requestDate": "2025-10-12",
    ///    "status": "APPROVED"
    ///  }
    /// 
    /// ```
    /// </remarks>
    /// <response code="200">Request updated successfully.</response>
    /// <response code="400">RequestId or the new Status was not a valid value</response>>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is forbidden from seeing the requests of this auto school.</response>


    [HttpPut("update/{requestId}/updateRequestStatus")]
    public async Task<IActionResult> UpdateRequestStatus(int requestId,[FromBody] RequestDto requestDto)
    {
        if (requestId <= 0)
            return BadRequest("Invalid request ID.");

        if( requestDto== null)
        {
            return BadRequest("Request data is required.");
        }

        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");
        if (!(User.IsInRole("SchoolAdmin") && user.AutoSchoolId == requestId))
            return Forbid("You are not authorized to update this request.");

        var request = await _db.Requests.FindAsync(requestId);
        if (request == null)
            return NotFound("Request not found.");

        request.Status = requestDto.Status; // Update the status of the request, that's all we do here.

        await _db.SaveChangesAsync();
        return Ok("Request status updated successfully.");
    }


    // ────────────────────────────── DELETE REQUEST ──────────────────────────────
    /// <summary>Delete a request (SchoolAdmin, SuperAdmin only).</summary>
    /// </remarks>
    /// <response code="200">Requests deleted successfully.</response>
    /// <response code="400">Request does not exist</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is forbidden from seeing the requests of this auto school.</response>

    [HttpDelete("delete/{requestId}/deleteRequest")]
    [Authorize(Roles = "SchoolAdmin,SuperAdmin")]

    public async Task<IActionResult> DeleteRequest(int requestId)
    {
        if (requestId <= 0)
            return BadRequest("Invalid request ID.");
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");
        if (!(User.IsInRole("SchoolAdmin") && user.AutoSchoolId == requestId))
            return Forbid("You are not authorized to delete this request.");
        var request = await _db.Requests.FindAsync(requestId);
        if (request == null)
            return NotFound("Request not found.");
        _db.Requests.Remove(request);

        await _db.SaveChangesAsync();

        return Ok("Request deleted successfully.");
    }
}


public sealed class RequestDto
{
    public int? RequestId { get; init; }
    public string? FirstName { get; init; } = default!;

    public string? LastName { get; init; } = default!;

    public string PhoneNr { get; init; } = default!;    
    public string? DrivingCategory { get; init; } = default!;
    public DateTime? RequestDate { get; init; } = default;
    public string? Status { get; init; } = default!;
}
