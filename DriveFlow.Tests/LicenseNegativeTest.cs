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
/// Integration tests that cover the negative paths of <see cref="LicenseController"/>:
/// • 400 Bad Request for missing / duplicate type values  
/// • 404 Not Found for a non-existent license.  
/// 
/// Note — when an action is invoked directly on the controller instance,
/// ASP-NET Core filters (including <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/>)
/// are not executed, so no 403 <see cref="ForbidResult"/> can be produced.
/// </summary>
public sealed class LicenseNegativeTest
{
    // ───────── helpers ─────────
    private static ApplicationDbContext InMemDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"License_Neg_{Guid.NewGuid()}")
               .Options);

    private static void AttachIdentity(
        ControllerBase controller,
        string role = "SuperAdmin",
        string userId = "u1")
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

    // ───────── POST /api/license/create – missing type ─────────
    [Fact]
    public async Task CreateLicenseAsync_ShouldReturn400_When_TypeMissing()
    {
        await using var db = InMemDb();
        var controller = new LicenseController(db);
        AttachIdentity(controller);

        var dto = new LicenseCreateDto { Type = "   " };

        var result = await controller.CreateLicenseAsync(dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("type");
    }

    // ───────── POST /api/license/create – duplicate type ─────────
    [Fact]
    public async Task CreateLicenseAsync_ShouldReturn400_When_DuplicateType()
    {
        await using var db = InMemDb();
        db.Licenses.Add(new License { LicenseId = 1, Type = "B" });
        await db.SaveChangesAsync();

        var controller = new LicenseController(db);
        AttachIdentity(controller);

        var dto = new LicenseCreateDto { Type = "  b  " };   // same as existing, different casing

        var result = await controller.CreateLicenseAsync(dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("already exists");
    }

    // ───────── PUT /api/license/update/{id} – license not found ─────────
    [Fact]
    public async Task UpdateLicenseAsync_ShouldReturn404_When_LicenseMissing()
    {
        await using var db = InMemDb();
        var controller = new LicenseController(db);
        AttachIdentity(controller);

        var dto = new LicenseUpdateDto { Type = "A" };

        var result = await controller.UpdateLicenseAsync(99, dto);

        var nf = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        nf.StatusCode.Should().Be(404);

        var msg = (string)nf.Value!.GetType().GetProperty("message")!.GetValue(nf.Value)!;
        msg.Should().Be("License not found.");
    }

    // ───────── PUT /api/license/update/{id} – duplicate type ─────────
    [Fact]
    public async Task UpdateLicenseAsync_ShouldReturn400_When_DuplicateType()
    {
        await using var db = InMemDb();
        db.Licenses.AddRange(
            new License { LicenseId = 1, Type = "A" },
            new License { LicenseId = 2, Type = "B" });
        await db.SaveChangesAsync();

        var controller = new LicenseController(db);
        AttachIdentity(controller);

        var dto = new LicenseUpdateDto { Type = "  a " };    // duplicates License 1

        var result = await controller.UpdateLicenseAsync(2, dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("already has");
    }

    // ───────── DELETE /api/license/delete/{id} – license not found ─────────
    [Fact]
    public async Task DeleteLicenseAsync_ShouldReturn404_When_LicenseMissing()
    {
        await using var db = InMemDb();
        var controller = new LicenseController(db);
        AttachIdentity(controller);

        var result = await controller.DeleteLicenseAsync(123);

        var nf = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        nf.StatusCode.Should().Be(404);

        var msg = (string)nf.Value!.GetType().GetProperty("message")!.GetValue(nf.Value)!;
        msg.Should().Be("License not found.");
    }
}
