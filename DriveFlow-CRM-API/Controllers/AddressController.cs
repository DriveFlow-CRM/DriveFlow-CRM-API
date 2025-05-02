using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DriveFlow_CRM_API.Models;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AddressController : ControllerBase
{
    // ───────────────────────────── fields & ctor ─────────────────────────────
    private readonly ApplicationDbContext _db;

    /// <summary>Constructor invoked per request by DI.</summary>
    public AddressController(ApplicationDbContext db) => _db = db;

    // ────────────────────────────── GET ADDRESSES ──────────────────────────────
    /// <summary>
    /// Returns the list of addresses ordered alphabetically by <c>streetName</c>.  
    /// If <c>cityId</c> is provided, only the addresses that belong to that city are returned;
    /// otherwise <strong>all</strong> addresses are returned.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response (filtered)</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "addressId": 30,
    ///     "streetName": "Dorobanților",
    ///     "addressNumber": "24",
    ///     "postcode": "400117",
    ///     "city": {
    ///       "cityId": 10,
    ///       "name": "Cluj-Napoca",
    ///       "county": { "countyId": 1, "name": "Cluj", "abbreviation": "CJ" }
    ///     }
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="cityId">
    /// Optional filter – returns only addresses that belong to the specified city.
    /// </param>
    /// <response code="200">Array of addresses returned successfully.</response>
    /// <response code="400">Invalid <c>cityId</c> supplied (must be positive).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">
    /// Authenticated user does not have one of the allowed roles
    /// (<c>SuperAdmin</c> or <c>SchoolAdmin</c>).
    /// </response>
    /// <response code="404">The specified city does not exist.</response>
    [HttpGet("get")]                       
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> GetAddressesAsync([FromQuery] int? cityId = null)
    {
        // ─── validate input ───
        if (cityId is < 0)
            return BadRequest(new { message = "cityId must be a positive integer." });

        if (cityId is not null && !await _db.Cities.AnyAsync(c => c.CityId == cityId))
            return NotFound(new { message = "City not found." });

        // ─── query & project ───
        var addresses = await (
            from a in _db.Addresses.AsNoTracking()
            join c in _db.Cities.AsNoTracking() on a.CityId equals c.CityId
            join k in _db.Counties.AsNoTracking() on c.CountyId equals k.CountyId
            where cityId == null || a.CityId == cityId
            orderby a.StreetName
            select new AddressDto
            {
                AddressId = a.AddressId,
                StreetName = a.StreetName,
                AddressNumber = a.AddressNumber,
                Postcode = a.Postcode,
                City = new CityDto
                {
                    CityId = c.CityId,
                    Name = c.Name,
                    County = new CountyDto
                    {
                        CountyId = k.CountyId,
                        Name = k.Name,
                        Abbreviation = k.Abbreviation
                    }
                }
            }).ToListAsync();

        return Ok(addresses);
    }

    // ────────────────────────────── POST ADDRESS ──────────────────────────────
    /// <summary>
    /// Creates a new address entry (SuperAdmin only).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// {
    ///   "streetName": "Avram Iancu",
    ///   "addressNumber": "15A",
    ///   "postcode": "410001",
    ///   "cityId": 21
    /// }
    /// ```
    /// </remarks>
    /// <response code="201">Address created successfully.</response>
    /// <response code="400">
    /// <em>streetName</em>, <em>addressNumber</em>, <em>postcode</em> and a positive <em>cityId</em> are required,
    /// and the city must exist.
    /// </response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user is not a <c>SuperAdmin</c>.</response>
    [HttpPost("create")]                   // POST api/address/create
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateAddressAsync([FromBody] AddressCreateDto dto)
    {
        // ─── validation ───
        if (string.IsNullOrWhiteSpace(dto.StreetName) ||
            string.IsNullOrWhiteSpace(dto.AddressNumber) ||
            string.IsNullOrWhiteSpace(dto.Postcode) ||
            dto.CityId <= 0)
        {
            return BadRequest(new
            {
                message = "All of 'streetName', 'addressNumber', 'postcode' and a positive 'cityId' are required."
            });
        }

        var cityExists = await _db.Cities.AnyAsync(c => c.CityId == dto.CityId);
        if (!cityExists)
            return BadRequest(new { message = "City with the specified ID does not exist." });


        // ─── insert ───
        var address = new Address
        {
            StreetName = dto.StreetName.Trim(),
            AddressNumber = dto.AddressNumber.Trim(),
            Postcode = dto.Postcode.Trim(),
            CityId = dto.CityId
        };

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync();

        return Created(
            $"/api/address/{address.AddressId}",
            new { addressId = address.AddressId, message = "Address created successfully" });
    }

    // ────────────────────────────── DELETE ADDRESS ──────────────────────────────
    /// <summary>
    /// Deletes an existing address (SuperAdmin only).
    /// </summary>
    /// <response code="204">Address deleted successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Authenticated user is not a <c>SuperAdmin</c>.</response>
    /// <response code="404">Address not found.</response>
    [HttpDelete("delete/{addressId:int}")]    // DELETE api/address/delete/42
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteAddressAsync(int addressId)
    {
        var address = await _db.Addresses.FindAsync(addressId);
        if (address is null)
            return NotFound(new { message = "Address not found" });

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();

        return NoContent();                   
    }
}

// ─────────────────────── DTOs ───────────────────────

/// <summary>
/// Lightweight representation of an address returned by <c>GET api/address</c>.
/// </summary>
public sealed class AddressDto
{
    public int AddressId { get; init; }
    public string StreetName { get; init; } = default!;
    public string AddressNumber { get; init; } = default!;
    public string Postcode { get; init; } = default!;
    public CityDto? City { get; init; } = default!;
}

/// <summary>
/// Payload used when creating a new address.
/// </summary>
public sealed class AddressCreateDto
{
    public string StreetName { get; init; } = default!;
    public string AddressNumber { get; init; } = default!;
    public string Postcode { get; init; } = default!;
    public int CityId { get; init; }
}
