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
/// Positive-path integration tests for <see cref="VehicleController"/>:
/// GET / POST / PUT / DELETE.
/// EF Core runs in-memory, and the <c>UserManager</c> is mocked.
/// </summary>
public sealed class VehiclePositiveTest
{
    // ───────── helpers ─────────
    private static ApplicationDbContext InMemDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"Vehicle_Pos_{Guid.NewGuid()}")
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

    // ───────── GET /api/vehicle/get/{schoolId} ─────────
    [Fact]
    public async Task GetVehiclesAsync_ShouldReturn200_And_AlphabeticList()
    {
        await using var db = InMemDb();

        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "DriveFlow" });
        db.Licenses.Add(new License { LicenseId = 1, Type = "B" });
        db.Vehicles.AddRange(
            new Vehicle { LicensePlateNumber = "B-200-BBB", TransmissionType = TransmissionType.AUTOMATIC, AutoSchoolId = 1, LicenseId = 1 },
            new Vehicle { LicensePlateNumber = "B-100-AAA", TransmissionType = TransmissionType.MANUAL, AutoSchoolId = 1, LicenseId = 1 },
            new Vehicle { LicensePlateNumber = "B-150-CCC", TransmissionType = TransmissionType.MANUAL, AutoSchoolId = 1, LicenseId = 1 });
        db.Users.Add(new ApplicationUser { Id = "sa1" });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SuperAdmin", userId: "sa1");

        var result = await controller.GetVehiclesAsync(1);

        var list = result.Should().BeOfType<OkObjectResult>().Subject
                         .Value.Should().BeAssignableTo<IEnumerable<VehicleDto>>().Subject.ToList();

        list.Should().HaveCount(3);
        list.Select(v => v.LicensePlateNumber)
            .Should().ContainInOrder("B-100-AAA", "B-150-CCC", "B-200-BBB");
    }

    // ───────── POST /api/vehicle/create/{schoolId} ─────────
    [Fact]
    public async Task CreateVehicleAsync_ShouldReturn201_And_PersistEntity()
    {
        await using var db = InMemDb();

        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "DriveFlow" });
        db.Licenses.Add(new License { LicenseId = 2, Type = "B" });
        db.Users.Add(new ApplicationUser { Id = "adm1", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm1");

        var dto = new VehicleCreateDto
        {
            LicensePlateNumber = "CJ-123-XYZ",
            TransmissionType = "manual",
            Color = "red",
            LicenseId = 2
        };

        var result = await controller.CreateVehicleAsync(1, dto);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Location.Should().Be("/api/vehicle/get/1");

        var payload = created.Value!;
        var t = payload.GetType();
        var idVal = (int)t.GetProperty("vehicleId")!.GetValue(payload)!;
        var msgVal = (string)t.GetProperty("message")!.GetValue(payload)!;

        idVal.Should().BeGreaterThan(0);
        msgVal.Should().Be("Vehicle created successfully");

        db.Vehicles.Should().ContainSingle(v => v.VehicleId == idVal &&
                                                v.LicensePlateNumber == "CJ-123-XYZ");
    }

    // ───────── PUT /api/vehicle/update/{id} ─────────
    [Fact]
    public async Task UpdateVehicleAsync_ShouldReturn200_And_ModifyFields()
    {
        await using var db = InMemDb();

        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "DriveFlow" });
        db.Licenses.AddRange(
            new License { LicenseId = 3, Type = "B" },
            new License { LicenseId = 4, Type = "C" });

        db.Vehicles.Add(new Vehicle
        {
            VehicleId = 5,
            LicensePlateNumber = "CJ-000-AAA",
            TransmissionType = TransmissionType.MANUAL,
            AutoSchoolId = 1,
            LicenseId = 3
        });
        db.Users.Add(new ApplicationUser { Id = "adm2", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm2");

        var dto = new VehicleUpdateDto
        {
            LicensePlateNumber = "CJ-555-BBB",
            TransmissionType = "automatic",
            Color = "blue",
            LicenseId = 4
        };

        var result = await controller.UpdateVehicleAsync(5, dto);

        result.Should().BeOfType<OkObjectResult>();

        var veh = await db.Vehicles.FindAsync(5);
        veh!.LicensePlateNumber.Should().Be("CJ-555-BBB");
        veh.TransmissionType.Should().Be(TransmissionType.AUTOMATIC);
        veh.Color.Should().Be("blue");
        veh.LicenseId.Should().Be(4);
    }

    // ───────── DELETE /api/vehicle/delete/{id} ─────────
    [Fact]
    public async Task DeleteVehicleAsync_ShouldReturn204_And_RemoveEntity()
    {
        await using var db = InMemDb();

        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "DriveFlow" });
        db.Vehicles.Add(new Vehicle
        {
            VehicleId = 9,
            LicensePlateNumber = "B-999-ZZZ",
            TransmissionType = TransmissionType.MANUAL,
            AutoSchoolId = 1
        });
        db.Users.Add(new ApplicationUser { Id = "adm3", AutoSchoolId = 1 });
        await db.SaveChangesAsync();

        var controller = new VehicleController(db, UM(db.Users).Object);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "adm3");

        var result = await controller.DeleteVehicleAsync(9);

        result.Should().BeOfType<NoContentResult>();
        db.Vehicles.Should().BeEmpty();
    }
}
