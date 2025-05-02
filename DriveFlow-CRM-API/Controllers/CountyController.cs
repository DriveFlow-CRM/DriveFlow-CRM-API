using Microsoft.AspNetCore.Mvc;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/[controller]")]

public class CountyController : ControllerBase
{
    // ───────────────────────────── fields & ctor ─────────────────────────────
    private readonly ApplicationDbContext _db;

    /// <summary>Constructor invoked per request by DI.</summary>
    public CountyController(
        ApplicationDbContext db)
    {
        _db = db;
    }

    // ────────────────────────────── GET COUNTIES ──────────────────────────────
    /// <summary>
    /// Returns the list of all counties ordered alphabetically by <c>name</c>.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   { "countyId": 1, "name": "Cluj",  "abbreviation": "CJ" },
    ///   { "countyId": 2, "name": "Bihor", "abbreviation": "BH" }
    /// ]
    /// ```
    /// </remarks>
    /// <response code="200">
    /// Array of counties returned successfully.
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">
    /// Authenticated user does not have one of the allowed roles
    /// (<c>SuperAdmin</c> or <c>SchoolAdmin</c>).
    /// </response>
    [HttpGet("get")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> GetCountiesAsync()
    {
        var counties = await _db.Counties
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CountyDto
            {
                CountyId = c.CountyId,
                Name = c.Name,
                Abbreviation = c.Abbreviation
            })
            .ToListAsync();

        return Ok(counties);
    }


    /// <summary>
    /// Creates a new county entry (SuperAdmin only).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// {
    ///   "name": "Timis",
    ///   "abbreviation": "TM"
    /// }
    /// ```
    /// </remarks>
    /// <response code="201">County created successfully.</response>
    /// <response code="400">
    /// Both <em>name</em> and <em>abbreviation</em> are required, or a county with the same
    /// name/abbreviation already exists.
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user is not a <c>SuperAdmin</c>.</response>
    [HttpPost]                     
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateCountyAsync([FromBody] CountyCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Abbreviation))
            return BadRequest(new { message = "Both 'name' and 'abbreviation' are required." });

        var name = dto.Name.Trim();
        var abbreviation = dto.Abbreviation.Trim().ToUpperInvariant();

        var duplicate = await _db.Counties.AnyAsync(c =>
            c.Name.ToLower() == name.ToLower() ||
            c.Abbreviation.ToLower() == abbreviation.ToLower());

        if (duplicate)
            return BadRequest(new { message = "A county with the same name or abbreviation already exists." });

        var county = new County { Name = name, Abbreviation = abbreviation };
        _db.Counties.Add(county);
        await _db.SaveChangesAsync();

        return Created(
            $"/api/county/{county.CountyId}",
            new { countyId = county.CountyId, message = "County created successfully" });
    }
    // ────────────────────────────── DELETE COUNTY ──────────────────────────────
    /// <summary>
    /// Deletes an existing county (SuperAdmin only).
    /// </summary>
    /// <response code="204">County deleted successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user is not a <c>SuperAdmin</c>.</response>
    /// <response code="404">County not found.</response>
    [HttpDelete("{countyId:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteCountyAsync(int countyId)
    {
        var county = await _db.Counties.FindAsync(countyId);
        if (county is null)
            return NotFound(new { message = "County not found" });

        _db.Counties.Remove(county);
        await _db.SaveChangesAsync();

        return NoContent();  
    }


}
// ─────────────────────── DTOs ───────────────────────

/// <summary>
/// Payload used when creating a new county.
/// </summary>
public sealed class CountyCreateDto
{
    public string Name { get; init; } = default!;
    public string Abbreviation { get; init; } = default!;
}

/// <summary>
/// Lightweight representation of a county returned by <c>GET api/county</c>.
/// </summary>
public sealed class CountyDto
{
    public int CountyId { get; init; }
    public string Name { get; init; } = default!;
    public string Abbreviation { get; init; } = default!;
}