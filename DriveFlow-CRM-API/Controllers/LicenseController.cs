using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public LicenseController(ApplicationDbContext db) => _db = db;

    // ────────────────────────────── GET LICENSES ──────────────────────────────
    /// <summary>Returns the list of all driving-license types(SuperAdmin and SchoolAdmin).</summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   { "licenseId": 1, "type": "C" },
    ///   { "licenseId": 2, "type": "B" }
    /// ]
    /// ```
    /// </remarks>
    /// <response code="200">Array returned successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    [HttpGet("get")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> GetLicensesAsync()
    {
        var licenses = await _db.Licenses
                                .AsNoTracking()
                                .OrderBy(l => l.LicenseId)
                                .Select(l => new LicenseDto
                                {
                                    LicenseId = l.LicenseId,
                                    Type = l.Type
                                })
                                .ToListAsync();

        return Ok(licenses);
    }

    // ────────────────────────────── POST LICENSE ──────────────────────────────
    /// <summary>Creates a new license entry (SuperAdmin only).</summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// { "type": "C" }
    /// ```
    /// </remarks>
    /// <response code="201">License created successfully.</response>
    /// <response code="400">Type missing or already exists.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is not SuperAdmin.</response>
    [HttpPost("create")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateLicenseAsync([FromBody] LicenseCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Type))
            return BadRequest(new { message = "type is required." });

        var typeTrimmed = dto.Type.Trim().ToUpperInvariant();

        if (await _db.Licenses.AnyAsync(l => l.Type.ToUpper() == typeTrimmed))
            return BadRequest(new { message = "A license with the given type already exists." });

        var lic = new License { Type = typeTrimmed };
        _db.Licenses.Add(lic);
        await _db.SaveChangesAsync();   // generates LicenseId

        return Created(
            $"/api/license/getLicenses",
            new { licenseId = lic.LicenseId, message = "License created successfully" });
    }

    // ────────────────────────────── PUT LICENSE ──────────────────────────────
    /// <summary>Updates an existing license (SuperAdmin only).</summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// { "type": "C" }
    /// ```
    /// </remarks>
    /// <param name="licenseId">Identifier of the license to update.</param>
    /// <param name="dto">Payload with the new type value.</param>
    /// <response code="200">License updated successfully.</response>
    /// <response code="400">Validation failed (missing or duplicate type).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is not SuperAdmin.</response>
    /// <response code="404">License not found.</response>
    [HttpPut("update/{licenseId:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateLicenseAsync(int licenseId, [FromBody] LicenseUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Type))
            return BadRequest(new { message = "type is required." });

        var lic = await _db.Licenses.FindAsync(licenseId);
        if (lic is null)
            return NotFound(new { message = "License not found." });

        var typeTrimmed = dto.Type.Trim().ToUpperInvariant();

        if (await _db.Licenses.AnyAsync(l => l.LicenseId != licenseId &&
                                             l.Type.ToUpper() == typeTrimmed))
            return BadRequest(new { message = "Another license already has that type." });

        lic.Type = typeTrimmed;
        await _db.SaveChangesAsync();

        return Ok(new { message = "License updated successfully" });
    }

    // ────────────────────────────── DELETE LICENSE ──────────────────────────────
    /// <summary>Deletes an existing license (SuperAdmin only).</summary>
    /// <param name="licenseId">Identifier of the license to delete.</param>
    /// <response code="204">License deleted successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is not SuperAdmin.</response>
    /// <response code="404">License not found.</response>
    [HttpDelete("delete/{licenseId:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteLicenseAsync(int licenseId)
    {
        var lic = await _db.Licenses.FindAsync(licenseId);
        if (lic is null)
            return NotFound(new { message = "License not found." });

        _db.Licenses.Remove(lic);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

// ─────────────────────── DTOs ───────────────────────

/// <summary>Representation returned by GET&#160;api/license/getLicenses.</summary>
public sealed class LicenseDto
{
    public int LicenseId { get; init; }
    public string Type { get; init; } = default!;
}
/// <summary>Payload used by POST&#160;api/license/createLicense.</summary>
public sealed class LicenseCreateDto
{
    public string Type { get; init; } = default!;
}

/// <summary>Payload used by PUT&#160;api/license/updateLicense/{licenseId}.</summary>
public sealed class LicenseUpdateDto
{
    public string Type { get; init; } = default!;
}
