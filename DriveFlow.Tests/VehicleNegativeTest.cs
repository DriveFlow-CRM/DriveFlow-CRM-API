using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using License = DriveFlow_CRM_API.Models.License;
using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests that cover the negative paths of <see cref="VehicleController"/>:
/// • 400 Bad Request for invalid payloads / duplicates / missing FK  
/// • 403 Forbid when a SchoolAdmin tries to access another school  
/// • 404 Not Found for a non-existent vehicle.
/// </summary>
public sealed class VehicleNegativeTest
{
    // ───────── helpers ─────────
    private static ApplicationDbContext InMemDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"Vehicle_Neg_{Guid.NewGuid()}")
               .Options);

    private static Mock<UserManager<ApplicationUser>> UM(IQueryable<ApplicationUser> users)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var m = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        m.SetupGet(x => x.Users).Returns(users);
        m.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
         .Returns((ClaimsPrincipal p) => p.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return m;
    }

    private static void AttachIdentity(
        ControllerBase c,
        string role,
        string userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role,           role),
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, authenticationType: "mock");

        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    // ───────── GET /api/vehicle/get/{schoolId} – bad id ─────────
    [Fact]
    public async Task GetVehiclesAsync_ShouldReturn400_When_SchoolIdNegative()
    {
        await using var db = InMemDb();
        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SuperAdmin", userId: "sa");

        var result = await controller.GetVehiclesAsync(-1);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("schoolId");
    }

    // ───────── GET /api/vehicle/get/{schoolId} – admin of another school ─────────
    [Fact]
    public async Task GetVehiclesAsync_ShouldReturn403_When_SchoolAdminOtherSchool()
    {
        await using var db = InMemDb();
        db.AutoSchools.AddRange(
            new AutoSchool { AutoSchoolId = 1, Name = "A" },
            new AutoSchool { AutoSchoolId = 2, Name = "B" });
        db.Users.Add(new ApplicationUser { Id = "adm", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm");

        var result = await controller.GetVehiclesAsync(2);

        result.Should().BeOfType<ForbidResult>();
    }

    // ───────── POST /api/vehicle/create – missing fields ─────────
    [Fact]
    public async Task CreateVehicleAsync_ShouldReturn400_When_FieldsInvalid()
    {
        await using var db = InMemDb();
        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "X" });
        db.Users.Add(new ApplicationUser { Id = "adm1", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm1");

        var dto = new VehicleCreateDto
        {
            LicensePlateNumber = " ",
            TransmissionType = "",
            LicenseId = 0
        };

        var result = await controller.CreateVehicleAsync(1, dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("required");
    }

    // ───────── POST /api/vehicle/create – transmission invalid ─────────
    [Fact]
    public async Task CreateVehicleAsync_ShouldReturn400_When_TransmissionInvalid()
    {
        await using var db = InMemDb();
        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "X" });
        db.Licenses.Add(new License { LicenseId = 2, Type = "B" });
        db.Users.Add(new ApplicationUser { Id = "adm1", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm1");

        var dto = new VehicleCreateDto
        {
            LicensePlateNumber = "CJ-00-ABC",
            TransmissionType = "semi",
            LicenseId = 2
        };

        var result = await controller.CreateVehicleAsync(1, dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().ContainAll("MANUAL", "AUTOMATIC"); // Changed to uppercase
    }

    // ───────── POST /api/vehicle/create – duplicate plate ─────────
    [Fact]
    public async Task CreateVehicleAsync_ShouldReturn400_When_DuplicatePlate()
    {
        await using var db = InMemDb();
        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "X" });
        db.Licenses.Add(new License { LicenseId = 3, Type = "B" });
        db.Vehicles.Add(new Vehicle
        {
            LicensePlateNumber = "CJ-999-AAA",
            TransmissionType = TransmissionType.MANUAL,
            AutoSchoolId = 1,
            LicenseId = 3
        });
        db.Users.Add(new ApplicationUser { Id = "adm1", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm1");

        var dto = new VehicleCreateDto
        {
            LicensePlateNumber = " cj-999-aaa ",   // same plate, different casing/spaces
            TransmissionType = "manual",
            LicenseId = 3
        };

        var result = await controller.CreateVehicleAsync(1, dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("already exists");
    }

    // ───────── POST /api/vehicle/create – license FK absent ─────────
    [Fact]
    public async Task CreateVehicleAsync_ShouldReturn400_When_LicenseMissing()
    {
        await using var db = InMemDb();
        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "X" });
        db.Users.Add(new ApplicationUser { Id = "adm1", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm1");

        var dto = new VehicleCreateDto
        {
            LicensePlateNumber = "CJ-123-DEF",
            TransmissionType = "manual",
            LicenseId = 42                     // no such license
        };

        var result = await controller.CreateVehicleAsync(1, dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("licenseId");
    }

    // ───────── PUT /api/vehicle/update/{id} – not found ─────────
    [Fact]
    public async Task UpdateVehicleAsync_ShouldReturn404_When_VehicleMissing()
    {
        await using var db = InMemDb();
        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm");

        var dto = new VehicleUpdateDto
        {
            LicensePlateNumber = "AAA",
            TransmissionType = "manual",
            LicenseId = 1
        };

        var result = await controller.UpdateVehicleAsync(999, dto);

        var nf = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        nf.StatusCode.Should().Be(404);

        var msg = (string)nf.Value!.GetType().GetProperty("message")!.GetValue(nf.Value)!;
        msg.Should().Be("Vehicle not found.");
    }

    // ───────── DELETE /api/vehicle/delete/{id} – admin of other school ─────────
    [Fact]
    public async Task DeleteVehicleAsync_ShouldReturn403_When_SchoolAdminOtherSchool()
    {
        await using var db = InMemDb();

        db.AutoSchools.AddRange(
            new AutoSchool { AutoSchoolId = 1, Name = "A" },
            new AutoSchool { AutoSchoolId = 2, Name = "B" });
        db.Vehicles.Add(new Vehicle
        {
            VehicleId = 7,
            LicensePlateNumber = "B-000-AAA",
            TransmissionType = TransmissionType.MANUAL,
            AutoSchoolId = 2
        });
        db.Users.Add(new ApplicationUser { Id = "adm1", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm1");

        var result = await controller.DeleteVehicleAsync(7);

        result.Should().BeOfType<ForbidResult>();
    }
}
