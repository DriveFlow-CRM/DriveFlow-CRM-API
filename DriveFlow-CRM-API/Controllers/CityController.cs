using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/[controller]")]   
public class CityController : ControllerBase
{
    // ───────────────────────────── fields & ctor ─────────────────────────────
    private readonly ApplicationDbContext _db;

    /// <summary>Constructor invoked per request by DI.</summary>
    public CityController(ApplicationDbContext db) => _db = db;

    // ────────────────────────────── GET CITIES ──────────────────────────────
    /// <summary>
    /// Returns the list of cities ordered alphabetically by <c>name</c>.  
    /// If <c>countyId</c> is provided, only the cities that belong to that county are returned;
    /// otherwise **all** cities are returned.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response (filtered)</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "cityId": 10,
    ///     "name": "Cluj-Napoca",
    ///     "county": { "countyId": 1, "name": "Cluj", "abbreviation": "CJ" }
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="countyId">
    /// Optional filter – returns only cities that belong to the specified county.
    /// </param>
    /// <response code="200">Array of cities returned successfully.</response>
    /// <response code="400">Invalid <c>countyId</c> supplied (must be positive).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">
    /// Authenticated user does not have one of the allowed roles
    /// (<c>SuperAdmin</c> or <c>SchoolAdmin</c>).
    /// </response>
    /// <response code="404">The specified county does not exist.</response>
    [HttpGet]                          
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> GetCitiesAsync([FromQuery] int? countyId = null)
    {
        // ─── validate input ───
        if (countyId is < 0)
            return BadRequest(new { message = "countyId must be a positive integer." });

        if (countyId is not null && !await _db.Counties.AnyAsync(c => c.CountyId == countyId))
            return NotFound(new { message = "County not found." });

        // ─── query & project ───
        var cities = await (
            from c in _db.Cities.AsNoTracking()
            join k in _db.Counties.AsNoTracking() on c.CountyId equals k.CountyId
            where countyId == null || c.CountyId == countyId
            orderby c.Name
            select new CityDto
            {
                CityId = c.CityId,
                Name = c.Name,
                County = new CountyDto
                {
                    CountyId = k.CountyId,
                    Name = k.Name,
                    Abbreviation = k.Abbreviation
                }
            }).ToListAsync();

        return Ok(cities);
    }

    // ────────────────────────────── POST CITY ──────────────────────────────
    /// <summary>
    /// Creates a new city entry (SuperAdmin only).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// {
    ///   "name": "Oradea",
    ///   "countyId": 2
    /// }
    /// ```
    /// </remarks>
    /// <response code="201">City created successfully.</response>
    /// <response code="400">
    /// <em>name</em> and a positive <em>countyId</em> are required, the county must exist,
    /// and the city name must be unique inside that county.
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user is not a <c>SuperAdmin</c>.</response>
    [HttpPost("create")]                   
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateCityAsync([FromBody] CityCreateDto dto)
    {
        // ─── validation ───
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.CountyId <= 0)
            return BadRequest(new { message = "Both 'name' and a positive 'countyId' are required." });

        var countyExists = await _db.Counties.AnyAsync(c => c.CountyId == dto.CountyId);
        if (!countyExists)
            return BadRequest(new { message = "County with the specified ID does not exist." });

        var name = dto.Name.Trim();
        var duplicate = await _db.Cities.AnyAsync(c =>
            c.CountyId == dto.CountyId &&
            c.Name.ToLower() == name.ToLower());

        if (duplicate)
            return BadRequest(new { message = "A city with the same name already exists in this county." });

        // ─── insert ───
        var city = new City { Name = name, CountyId = dto.CountyId };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync();

        return Created(
            $"/api/city/{city.CityId}",
            new { cityId = city.CityId, message = "City created successfully" });
    }

    // ────────────────────────────── DELETE CITY ──────────────────────────────
    /// <summary>
    /// Deletes an existing city (SuperAdmin only).
    /// </summary>
    /// <response code="204">City deleted successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user is not a <c>SuperAdmin</c>.</response>
    /// <response code="404">City not found.</response>
    [HttpDelete("{cityId:int}")]            // DELETE api/city/21
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteCityAsync(int cityId)
    {
        var city = await _db.Cities.FindAsync(cityId);
        if (city is null)
            return NotFound(new { message = "City not found" });

        _db.Cities.Remove(city);
        await _db.SaveChangesAsync();

        return NoContent();              
    }

}



// ─────────────────────── DTOs ───────────────────────

/// <summary>
/// Lightweight representation of a city returned by <c>GET api/city</c>.
/// </summary>
public sealed class CityDto
{
    public int CityId { get; init; }
    public string Name { get; init; } = default!;
    public CountyDto? County { get; init; } = default!;
}

/// <summary>
/// Payload used when creating a new city.
/// </summary>
public sealed class CityCreateDto
{
    public string Name { get; init; } = default!;
    public int CountyId { get; init; }
}
