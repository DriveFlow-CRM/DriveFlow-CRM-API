using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/schoolspage")]
public class AutoSchoolPageController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AutoSchoolPageController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
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

    /// <summary>
    /// Returns detailed information about a specific auto school, including vehicles, teaching categories,
    /// student/instructor counts, and address information.
    /// This endpoint is publicly accessible (no authentication required).
    /// </summary>
    /// <param name="schoolId">The ID of the auto school to retrieve</param>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// {
    ///   "autoSchoolId": 1,
    ///   "name": "DriveFlow",
    ///   "description": "Headquarters – central branch",
    ///   "website": "https://driveflow.ro",
    ///   "phoneNumber": "0723111222",
    ///   "email": "contact@driveflow.ro",
    ///   "status": "active",
    ///   "studentCount": 42,
    ///   "instructorCount": 8,
    ///   "address": {
    ///     "streetName": "Dorobanților",
    ///     "addressNumber": "24",
    ///     "postcode": "400117",
    ///     "city": "Cluj-Napoca",
    ///     "county": "Cluj",
    ///     "countyAbbreviation": "CJ"
    ///   },
    ///   "vehicles": [
    ///     {
    ///       "licensePlateNumber": "CJ-01-DRV",
    ///       "transmissionType": "manual",
    ///       "color": "Silver",
    ///       "licenseType": "B"
    ///     }
    ///   ],
    ///   "teachingCategories": [
    ///     {
    ///       "licenseType": "B",
    ///       "sessionDuration": 60,
    ///       "minDrivingLessonsReq": 15
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">Auto school details returned successfully.</response>
    /// <response code="404">Auto school with the specified ID was not found.</response>
    [HttpGet("schools/{schoolId:int}")]
    public async Task<IActionResult> GetAutoSchoolDetails(int schoolId)
    {
        // Get the roles for students and instructors
        var studentRole = await _roleManager.FindByNameAsync("Student");
        var instructorRole = await _roleManager.FindByNameAsync("Instructor");

        if (studentRole == null || instructorRole == null)
        {
            return NotFound("Required roles not found in the system.");
        }

        var studentRoleId = studentRole.Id;
        var instructorRoleId = instructorRole.Id;

        // Get the auto school with all required details
        var school = await _db.AutoSchools
            .Where(s => s.AutoSchoolId == schoolId)
            .Select(s => new
            {
                School = s,
                Address = s.Address == null ? null : new
                {
                    s.Address.StreetName,
                    s.Address.AddressNumber,
                    s.Address.Postcode,
                    CityName = s.Address.City == null ? null : s.Address.City.Name,
                    CountyName = s.Address.City == null || s.Address.City.County == null ? null : s.Address.City.County.Name,
                    CountyAbbreviation = s.Address.City == null || s.Address.City.County == null ? null : s.Address.City.County.Abbreviation
                },
                Vehicles = s.Vehicles
                    .Select(v => new
                    {
                        v.LicensePlateNumber,
                        TransmissionType = v.TransmissionType.ToString().ToLowerInvariant(),
                        v.Color,
                        LicenseType = v.License == null ? null : v.License.Type
                    })
                    .ToList(),
                TeachingCategories = s.TeachingCategories
                    .Select(tc => new
                    {
                        LicenseType = tc.License == null ? null : tc.License.Type,
                        tc.SessionDuration,
                        tc.MinDrivingLessonsReq
                    })
                    .ToList(),
                // Count students
                StudentCount = _db.Users
                    .Where(u => u.AutoSchoolId == s.AutoSchoolId)
                    .Join(_db.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                    .Count(x => x.ur.RoleId == studentRoleId),
                // Count instructors
                InstructorCount = _db.Users
                    .Where(u => u.AutoSchoolId == s.AutoSchoolId)
                    .Join(_db.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                    .Count(x => x.ur.RoleId == instructorRoleId)
            })
            .FirstOrDefaultAsync();

        if (school == null)
        {
            return NotFound($"Auto school with ID {schoolId} not found.");
        }

        // Map to the response DTO
        var result = new AutoSchoolDetailsDto
        {
            AutoSchoolId = school.School.AutoSchoolId,
            Name = school.School.Name,
            Description = school.School.Description,
            WebSite = school.School.WebSite,
            PhoneNumber = school.School.PhoneNumber,
            Email = school.School.Email,
            Status = school.School.Status.ToString().ToLowerInvariant(),
            StudentCount = school.StudentCount,
            InstructorCount = school.InstructorCount,
            Address = school.Address == null ? null : new AddressDetailsDto
            {
                StreetName = school.Address.StreetName,
                AddressNumber = school.Address.AddressNumber,
                Postcode = school.Address.Postcode,
                City = school.Address.CityName,
                County = school.Address.CountyName,
                CountyAbbreviation = school.Address.CountyAbbreviation
            },
            Vehicles = school.Vehicles.Select(v => new SchoolVehicleDto
            {
                LicensePlateNumber = v.LicensePlateNumber,
                TransmissionType = v.TransmissionType,
                Color = v.Color,
                LicenseType = v.LicenseType
            }).ToList(),
            TeachingCategories = school.TeachingCategories.Select(tc => new TeachingCategoryDetailsDto
            {
                LicenseType = tc.LicenseType,
                SessionDuration = tc.SessionDuration,
                MinDrivingLessonsReq = tc.MinDrivingLessonsReq
            }).ToList()
        };

        return Ok(result);
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

/// <summary>
/// DTO for detailed auto school information
/// </summary>
public class AutoSchoolDetailsDto
{
    /// <summary>Auto school ID</summary>
    public int AutoSchoolId { get; set; }
    
    /// <summary>School name</summary>
    public string Name { get; set; } = null!;
    
    /// <summary>School description (optional)</summary>
    public string? Description { get; set; }
    
    /// <summary>Website URL (optional)</summary>
    public string? WebSite { get; set; }
    
    /// <summary>Contact phone number (optional)</summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>Contact email address (optional)</summary>
    public string? Email { get; set; }
    
    /// <summary>Operational status</summary>
    public string Status { get; set; } = null!;
    
    /// <summary>Number of students enrolled in the school</summary>
    public int StudentCount { get; set; }
    
    /// <summary>Number of instructors working at the school</summary>
    public int InstructorCount { get; set; }
    
    /// <summary>School address information (optional)</summary>
    public AddressDetailsDto? Address { get; set; }
    
    /// <summary>Vehicles owned by the school</summary>
    public List<SchoolVehicleDto> Vehicles { get; set; } = new();
    
    /// <summary>Teaching categories offered by the school</summary>
    public List<TeachingCategoryDetailsDto> TeachingCategories { get; set; } = new();
}

/// <summary>
/// DTO for address information
/// </summary>
public class AddressDetailsDto
{
    /// <summary>Street name</summary>
    public string? StreetName { get; set; }
    
    /// <summary>Street number</summary>
    public string? AddressNumber { get; set; }
    
    /// <summary>Postal code</summary>
    public string? Postcode { get; set; }
    
    /// <summary>City name</summary>
    public string? City { get; set; }
    
    /// <summary>County name</summary>
    public string? County { get; set; }
    
    /// <summary>County abbreviation</summary>
    public string? CountyAbbreviation { get; set; }
}

/// <summary>
/// DTO for vehicle information in the auto school details
/// </summary>
public class SchoolVehicleDto
{
    /// <summary>License plate number</summary>
    public string LicensePlateNumber { get; set; } = null!;
    
    /// <summary>Transmission type (manual/automatic)</summary>
    public string? TransmissionType { get; set; }
    
    /// <summary>Vehicle color</summary>
    public string? Color { get; set; }
    
    /// <summary>License type required to drive the vehicle</summary>
    public string? LicenseType { get; set; }
}

/// <summary>
/// DTO for teaching category information
/// </summary>
public class TeachingCategoryDetailsDto
{
    /// <summary>License type</summary>
    public string? LicenseType { get; set; }
    
    /// <summary>Duration of one session in minutes</summary>
    public int SessionDuration { get; set; }
    
    /// <summary>Minimum number of driving lessons required</summary>
    public int MinDrivingLessonsReq { get; set; }
}
