using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using System.Linq;
using System.Threading.Tasks;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/landing")]
public class AutoSchoolPageController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AutoSchoolPageController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns all active or demo auto schools with basic information.
    /// This endpoint is publicly accessible (no authentication required).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "id": 1,
    ///     "name": "DriveFlow",
    ///     "description": "Headquarters – central branch",
    ///     "status": "active"
    ///   },
    ///   {
    ///     "id": 2,
    ///     "name": "Start-Drive",
    ///     "description": "Professional driving school with experienced instructors",
    ///     "status": "demo"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <response code="200">
    /// Array of auto schools returned successfully. Empty array if no schools match criteria.
    /// </response>
    [HttpGet("schools")]
    public async Task<IActionResult> GetAutoSchoolsForLanding()
    {
        var schools = await _db.AutoSchools
            .Where(s => s.Status == AutoSchoolStatus.Active || s.Status == AutoSchoolStatus.Demo)
            .Select(s => new AutoSchoolLandingDto
            {
                Id = s.AutoSchoolId,
                Name = s.Name,
                Description = s.Description,
                Status = s.Status.ToString().ToLowerInvariant()
            })
            .ToListAsync();

        return Ok(schools);
    }
}

/// <summary>
/// DTO for basic auto school information on the landing page
/// </summary>
public class AutoSchoolLandingDto
{
    /// <summary>Auto school ID</summary>
    public int Id { get; set; }
    
    /// <summary>School name</summary>
    public string Name { get; set; } = null!;
    
    /// <summary>School description (optional)</summary>
    public string? Description { get; set; }
    
    /// <summary>Operational status (active or demo)</summary>
    public string Status { get; set; } = null!;
}
