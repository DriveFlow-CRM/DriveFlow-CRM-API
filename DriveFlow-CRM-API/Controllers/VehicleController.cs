using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public VehicleController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ────────────────────────────── GET VEHICLES ──────────────────────────────
    /// <summary>
    /// Returns every vehicle that belongs to the specified school
    /// (SuperAdmin for any school; SchoolAdmin only for their own school – otherwise 403).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "vehicleId": 101,
    ///     "licensePlateNumber": "B-123-XYZ",
    ///     "transmissionType": "manual",
    ///     "color": "rosu",
    ///     "brand": "Toyota",
    ///     "model": "Corolla", 
    ///     "yearOfProduction": 2020,
    ///     "fuelType": "BENZINA",
    ///     "engineSizeLiters": 1.8,
    ///     "powertrainType": "HIBRID",
    ///     "itpExpiryDate": "2025-05-30T21:43:29",
    ///     "insuranceExpiryDate": "2027-04-12",
    ///     "rcaExpiryDate": "2027-04-12",
    ///     "licenseId": 1
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <response code="200">List returned successfully (can be empty).</response>
    /// <response code="400"><paramref name="schoolId"/> is not positive.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is a <c>SchoolAdmin</c> of another school.</response>
    [HttpGet("get/{schoolId:int}")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
    public async Task<IActionResult> GetVehiclesAsync(int schoolId)
    {
        // Validate route parameter
        if (schoolId <= 0)
            return BadRequest(new { message = "schoolId must be a positive integer." });

        // Identify caller (id & associated school)
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users
                                   .AsNoTracking()
                                   .Select(u => new { u.Id, u.AutoSchoolId })
                                   .FirstOrDefaultAsync(u => u.Id == callerId);

        var isSchoolAdmin = User.IsInRole("SchoolAdmin");

        // A SchoolAdmin may access only their own school
        if (isSchoolAdmin && caller?.AutoSchoolId != schoolId)
            return Forbid();

        // Query and project vehicles
        var vehicles = await _db.Vehicles
                                .AsNoTracking()
                                .Where(v => v.AutoSchoolId == schoolId)
                                .OrderBy(v => v.LicensePlateNumber)
                                .Select(v => new VehicleDto
                                {
                                    VehicleId = v.VehicleId,
                                    LicensePlateNumber = v.LicensePlateNumber,
                                    TransmissionType = v.TransmissionType.ToString().ToLowerInvariant(),
                                    Color = v.Color,
                                    Brand = v.Brand,
                                    Model = v.Model,
                                    YearOfProduction = v.YearOfProduction,
                                    FuelType = v.FuelType.HasValue ? v.FuelType.ToString() : null,
                                    EngineSizeLiters = v.EngineSizeLiters,
                                    PowertrainType = v.PowertrainType.HasValue ? v.PowertrainType.ToString() : null,
                                    ItpExpiryDate = v.ItpExpiryDate,
                                    InsuranceExpiryDate = v.InsuranceExpiryDate,
                                    RcaExpiryDate = v.RcaExpiryDate,
                                    LicenseId = v.LicenseId ?? 0
                                })
                                .ToListAsync();

        return Ok(vehicles);
    }
    // ────────────────────────────── POST VEHICLE ──────────────────────────────
    /// <summary>
    /// Creates a new vehicle in the specified school (SchoolAdmin only, same school).
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// {
    ///   "licensePlateNumber": "CJ-456-ABC",
    ///   "transmissionType": "automatic",
    ///   "color": "blue",
    ///   "brand": "Toyota",
    ///   "model": "Corolla", 
    ///   "yearOfProduction": 2020,
    ///   "fuelType": "BENZINA",
    ///   "engineSizeLiters": 1.8,
    ///   "powertrainType": "HIBRID",
    ///   "itpExpiryDate": null,
    ///   "insuranceExpiryDate": null,
    ///   "rcaExpiryDate": null,
    ///   "licenseId": 1
    /// }
    /// ```
    /// </remarks>
    /// <param name="schoolId">School identifier from the route.</param>
    /// <param name="dto">Vehicle data (all attributes present, null-allowed).</param>
    /// <response code="201">Vehicle created successfully.</response>
    /// <response code="400">Validation failed (missing fields, duplicates, invalid enum or licenseId).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is SchoolAdmin of a different school.</response>
    [HttpPost("create/{schoolId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> CreateVehicleAsync(int schoolId, [FromBody] VehicleCreateDto dto)
    {
        // ─── caller must belong to this school ───
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users.AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.Id == callerId);

        if (caller?.AutoSchoolId != schoolId)
            return Forbid();

        // ─── basic validation ───
        if (string.IsNullOrWhiteSpace(dto.LicensePlateNumber) ||
            string.IsNullOrWhiteSpace(dto.TransmissionType) ||
            dto.LicenseId <= 0)
        {
            return BadRequest(new
            {
                message = "licensePlateNumber, transmissionType and a positive licenseId are required."
            });
        }

        if (!Enum.TryParse<TransmissionType>(dto.TransmissionType, true, out var transEnum))
            return BadRequest(new { message = "transmissionType must be 'manual' or 'automatic'." });

        // Validate FuelType and PowertrainType if provided
        TipCombustibil? fuelType = null;
        if (!string.IsNullOrWhiteSpace(dto.FuelType) && 
            !Enum.TryParse<TipCombustibil>(dto.FuelType, out var fuelEnum))
        {
            return BadRequest(new { message = "fuelType must be one of: BENZINA, MOTORINA, ELECTRIC, HIBRID, HIDROGEN, GNC." });
        }
        else if (!string.IsNullOrWhiteSpace(dto.FuelType))
        {
            fuelType = Enum.Parse<TipCombustibil>(dto.FuelType);
        }

        TipPropulsie? powertrainType = null;
        if (!string.IsNullOrWhiteSpace(dto.PowertrainType) && 
            !Enum.TryParse<TipPropulsie>(dto.PowertrainType, out var powertrainEnum))
        {
            return BadRequest(new { message = "powertrainType must be one of: COMBUSTIBIL, HIBRID, ELECTRIC." });
        }
        else if (!string.IsNullOrWhiteSpace(dto.PowertrainType))
        {
            powertrainType = Enum.Parse<TipPropulsie>(dto.PowertrainType);
        }

        // Validate consistency between FuelType and PowertrainType
        if (powertrainType == TipPropulsie.ELECTRIC && fuelType.HasValue && fuelType != TipCombustibil.ELECTRIC)
        {
            return BadRequest(new { message = "For ELECTRIC powertrain, fuelType must be ELECTRIC." });
        }

        // duplicate plate in same school
        var duplicate = await _db.Vehicles.AnyAsync(v =>
            v.AutoSchoolId == schoolId &&
            v.LicensePlateNumber.ToLower() == dto.LicensePlateNumber.Trim().ToLower());

        if (duplicate)
            return BadRequest(new { message = "A vehicle with this license plate already exists in the school." });

        // license must exist
        if (!await _db.Licenses.AnyAsync(l => l.LicenseId == dto.LicenseId))
            return BadRequest(new { message = "licenseId does not reference an existing license." });

        // ─── insert ───
        var vehicle = new Vehicle
        {
            LicensePlateNumber = dto.LicensePlateNumber.Trim(),
            TransmissionType = transEnum,
            Color = dto.Color?.Trim(),
            Brand = dto.Brand?.Trim(),
            Model = dto.Model?.Trim(),
            YearOfProduction = dto.YearOfProduction,
            FuelType = fuelType,
            EngineSizeLiters = dto.EngineSizeLiters,
            PowertrainType = powertrainType,
            ItpExpiryDate = dto.ItpExpiryDate,
            InsuranceExpiryDate = dto.InsuranceExpiryDate,
            RcaExpiryDate = dto.RcaExpiryDate,
            LicenseId = dto.LicenseId,
            AutoSchoolId = schoolId
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();   // generates VehicleId

        return Created(
            $"/api/vehicle/get/{schoolId}",
            new { vehicleId = vehicle.VehicleId, message = "Vehicle created successfully" });
    }
    // ────────────────────────────── PUT VEHICLE ──────────────────────────────
    /// <summary>Updates an existing vehicle (SchoolAdmin of the same school).</summary>
    /// <remarks>
    /// <para><strong>Sample request</strong></para>
    ///
    /// ```json
    /// {
    ///   "licensePlateNumber": "BV-476-DEF",
    ///   "transmissionType": "manual",
    ///   "color": "green",
    ///   "brand": "Toyota",
    ///   "model": "Corolla", 
    ///   "yearOfProduction": 2020,
    ///   "fuelType": "BENZINA",
    ///   "engineSizeLiters": 1.8,
    ///   "powertrainType": "HIBRID",
    ///   "itpExpiryDate": null,
    ///   "insuranceExpiryDate": null,
    ///   "rcaExpiryDate": null,
    ///   "licenseId": 4
    /// }
    /// ```
    /// </remarks>
    /// <param name="vehicleId">Vehicle identifier from the route.</param>
    /// <param name="dto">Updated data (every property present, null allowed).</param>
    /// <response code="200">Vehicle updated successfully.</response>
    /// <response code="400">Validation failed (enum, duplicates, bad licenseId).</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is SchoolAdmin of a different school.</response>
    /// <response code="404">Vehicle not found.</response>
    [HttpPut("update/{vehicleId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> UpdateVehicleAsync(int vehicleId, [FromBody] VehicleUpdateDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(vehicleId);
        if (vehicle is null)
            return NotFound(new { message = "Vehicle not found." });

        // caller must belong to the same school
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users.AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.Id == callerId);

        if (caller?.AutoSchoolId != vehicle.AutoSchoolId)
            return Forbid();

        // basic validation
        if (string.IsNullOrWhiteSpace(dto.LicensePlateNumber) ||
            string.IsNullOrWhiteSpace(dto.TransmissionType) ||
            dto.LicenseId <= 0)
        {
            return BadRequest(new { message = "licensePlateNumber, transmissionType and a positive licenseId are required." });
        }

        if (!Enum.TryParse<TransmissionType>(dto.TransmissionType, true, out var transEnum))
            return BadRequest(new { message = "transmissionType must be 'manual' or 'automatic'." });

        // Validate FuelType and PowertrainType if provided
        TipCombustibil? fuelType = null;
        if (!string.IsNullOrWhiteSpace(dto.FuelType) && 
            !Enum.TryParse<TipCombustibil>(dto.FuelType, out var fuelEnum))
        {
            return BadRequest(new { message = "fuelType must be one of: BENZINA, MOTORINA, ELECTRIC, HIBRID, HIDROGEN, GNC." });
        }
        else if (!string.IsNullOrWhiteSpace(dto.FuelType))
        {
            fuelType = Enum.Parse<TipCombustibil>(dto.FuelType);
        }

        TipPropulsie? powertrainType = null;
        if (!string.IsNullOrWhiteSpace(dto.PowertrainType) && 
            !Enum.TryParse<TipPropulsie>(dto.PowertrainType, out var powertrainEnum))
        {
            return BadRequest(new { message = "powertrainType must be one of: COMBUSTIBIL, HIBRID, ELECTRIC." });
        }
        else if (!string.IsNullOrWhiteSpace(dto.PowertrainType))
        {
            powertrainType = Enum.Parse<TipPropulsie>(dto.PowertrainType);
        }

        // Validate consistency between FuelType and PowertrainType
        if (powertrainType == TipPropulsie.ELECTRIC && fuelType.HasValue && fuelType != TipCombustibil.ELECTRIC)
        {
            return BadRequest(new { message = "For ELECTRIC powertrain, fuelType must be ELECTRIC." });
        }

        // duplicate plate check in the same school
        var dup = await _db.Vehicles.AnyAsync(v =>
            v.AutoSchoolId == vehicle.AutoSchoolId &&
            v.VehicleId != vehicleId &&
            v.LicensePlateNumber.ToLower() == dto.LicensePlateNumber.Trim().ToLower());

        if (dup)
            return BadRequest(new { message = "Another vehicle in this school already has that license plate." });

        // license must exist
        if (!await _db.Licenses.AnyAsync(l => l.LicenseId == dto.LicenseId))
            return BadRequest(new { message = "licenseId does not reference an existing license." });

        // apply updates
        vehicle.LicensePlateNumber = dto.LicensePlateNumber.Trim();
        vehicle.TransmissionType = transEnum;
        vehicle.Color = dto.Color?.Trim();
        vehicle.Brand = dto.Brand?.Trim();
        vehicle.Model = dto.Model?.Trim();
        vehicle.YearOfProduction = dto.YearOfProduction;
        vehicle.FuelType = fuelType;
        vehicle.EngineSizeLiters = dto.EngineSizeLiters;
        vehicle.PowertrainType = powertrainType;
        vehicle.ItpExpiryDate = dto.ItpExpiryDate;
        vehicle.InsuranceExpiryDate = dto.InsuranceExpiryDate;
        vehicle.RcaExpiryDate = dto.RcaExpiryDate;
        vehicle.LicenseId = dto.LicenseId;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Vehicle updated successfully" });
    }
    // ────────────────────────────── DELETE VEHICLE ──────────────────────────────
    /// <summary>Deletes an existing vehicle (SchoolAdmin of the same school).</summary>
    /// <param name="vehicleId">Vehicle identifier from the route.</param>
    /// <response code="204">Vehicle deleted successfully.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">Caller is SchoolAdmin of a different school.</response>
    /// <response code="404">Vehicle not found.</response>
    [HttpDelete("delete/{vehicleId:int}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> DeleteVehicleAsync(int vehicleId)
    {
        var vehicle = await _db.Vehicles.FindAsync(vehicleId);
        if (vehicle is null)
            return NotFound(new { message = "Vehicle not found." });

        // caller must belong to the same school
        var callerId = _users.GetUserId(User)!;
        var caller = await _users.Users.AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.Id == callerId);

        if (caller?.AutoSchoolId != vehicle.AutoSchoolId)
            return Forbid();

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();

        return NoContent();
    }


}

// ─────────────────────── DTO ───────────────────────

/// <summary>Representation of a vehicle, as returned by GET api/vehicle/get/{schoolId}.</summary>
public sealed class VehicleDto
{
    public int VehicleId { get; init; }
    public string LicensePlateNumber { get; init; } = default!;
    public string TransmissionType { get; init; } = default!;
    public string? Color { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? YearOfProduction { get; init; }
    public string? FuelType { get; init; }
    public decimal? EngineSizeLiters { get; init; }
    public string? PowertrainType { get; init; }
    public DateTime? ItpExpiryDate { get; init; }
    public DateTime? InsuranceExpiryDate { get; init; }
    public DateTime? RcaExpiryDate { get; init; }
    public int LicenseId { get; init; }
}

/// <summary>Payload used by POST api/vehicle/createVehicle/{schoolId}.</summary>
public sealed class VehicleCreateDto
{
    public string LicensePlateNumber { get; init; } = default!;
    public string TransmissionType { get; init; } = default!;
    public string? Color { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? YearOfProduction { get; init; }
    public string? FuelType { get; init; }
    public decimal? EngineSizeLiters { get; init; }
    public string? PowertrainType { get; init; }
    public DateTime? ItpExpiryDate { get; init; }
    public DateTime? InsuranceExpiryDate { get; init; }
    public DateTime? RcaExpiryDate { get; init; }
    public int LicenseId { get; init; }
}
/// <summary>Payload used by PUT api/vehicle/updateVehicle/{vehicleId}.</summary>
public sealed class VehicleUpdateDto
{
    public string LicensePlateNumber { get; init; } = default!;
    public string TransmissionType { get; init; } = default!;
    public string? Color { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? YearOfProduction { get; init; }
    public string? FuelType { get; init; }
    public decimal? EngineSizeLiters { get; init; }
    public string? PowertrainType { get; init; }
    public DateTime? ItpExpiryDate { get; init; }
    public DateTime? InsuranceExpiryDate { get; init; }
    public DateTime? RcaExpiryDate { get; init; }
    public int LicenseId { get; init; }
}