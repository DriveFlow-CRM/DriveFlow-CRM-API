using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DriveFlow_CRM_API.Controllers;
using License = DriveFlow_CRM_API.Models.License;
namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Positive-path integration tests for <see cref="LicenseController"/>:
/// GET / POST / PUT / DELETE.  
/// Tests run against an in-memory EF Core database; authentication is faked via claims.
/// </summary>
public sealed class LicensePositiveTest
{
    // ───────── helpers ─────────
    private static ApplicationDbContext InMemDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"License_Pos_{Guid.NewGuid()}")
               .Options);

    private static void AttachIdentity(
        ControllerBase controller,
        string role,
        string userId = "sa1")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role,           role),
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, authenticationType: "mock");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    // ───────── GET /api/license/get ─────────
    [Fact]
    public async Task GetLicensesAsync_ShouldReturn200_And_OrderedList()
    {
        await using var db = InMemDb();
        db.Licenses.AddRange(
            new License { LicenseId = 2, Type = "B" },
            new License { LicenseId = 1, Type = "C" });
        await db.SaveChangesAsync();

        var controller = new LicenseController(db);
        AttachIdentity(controller, role: "SuperAdmin");

        var result = await controller.GetLicensesAsync();

        var list = result.Should().BeOfType<OkObjectResult>().Subject
                         .Value.Should().BeAssignableTo<IEnumerable<LicenseDto>>().Subject.ToList();

        list.Should().HaveCount(2);
        list.Select(l => l.LicenseId).Should().ContainInOrder(1, 2);
        list.Select(l => l.Type).Should().ContainInOrder("C", "B");
    }

    // ───────── POST /api/license/create ─────────
    [Fact]
    public async Task CreateLicenseAsync_ShouldReturn201_And_PersistEntity()
    {
        await using var db = InMemDb();

        var controller = new LicenseController(db);
        AttachIdentity(controller, role: "SuperAdmin");

        var dto = new LicenseCreateDto { Type = "  a  " };

        var result = await controller.CreateLicenseAsync(dto);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Location.Should().Be("/api/license/getLicenses");

        var payload = created.Value!;
        var t = payload.GetType();
        var idVal = (int)t.GetProperty("licenseId")!.GetValue(payload)!;
        var msgVal = (string)t.GetProperty("message")!.GetValue(payload)!;

        idVal.Should().BeGreaterThan(0);
        msgVal.Should().Be("License created successfully");

        db.Licenses.Should().ContainSingle(l => l.LicenseId == idVal && l.Type == "A");
    }

    // ───────── PUT /api/license/update/{id} ─────────
    [Fact]
    public async Task UpdateLicenseAsync_ShouldReturn200_And_ModifyType()
    {
        await using var db = InMemDb();
        db.Licenses.Add(new License { LicenseId = 3, Type = "B" });
        await db.SaveChangesAsync();

        var controller = new LicenseController(db);
        AttachIdentity(controller, role: "SuperAdmin");

        var dto = new LicenseUpdateDto { Type = "  c " };

        var result = await controller.UpdateLicenseAsync(3, dto);

        result.Should().BeOfType<OkObjectResult>();
        (await db.Licenses.FindAsync(3))!.Type.Should().Be("C");
    }

    // ───────── DELETE /api/license/delete/{id} ─────────
    [Fact]
    public async Task DeleteLicenseAsync_ShouldReturn204_And_RemoveEntity()
    {
        await using var db = InMemDb();
        db.Licenses.Add(new License { LicenseId = 4, Type = "D" });
        await db.SaveChangesAsync();

        var controller = new LicenseController(db);
        AttachIdentity(controller, role: "SuperAdmin");

        var result = await controller.DeleteLicenseAsync(4);

        result.Should().BeOfType<NoContentResult>();
        db.Licenses.Should().BeEmpty();
    }
}
