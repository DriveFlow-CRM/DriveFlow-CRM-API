using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests that exercise the negative paths of <see cref="AutoSchoolController"/>:  
/// 400 for bad payload / duplicates / invalid references, 404 for missing resources.
/// Identity services are mocked; authorization is ignored.
/// </summary>
public sealed class AutoSchoolNegativeTest
{
    // in-memory DbContext
    private static ApplicationDbContext CreateInMemoryDb()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"AutoSchoolNeg_{Guid.NewGuid()}")
               .Options);

    // UserManager mock
    private static Mock<UserManager<ApplicationUser>> MockUserManager(IQueryable<ApplicationUser> users)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        mgr.SetupGet(x => x.Users).Returns(users);
        mgr.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
           .ReturnsAsync(IdentityResult.Success);
        return mgr;
    }

    // RoleManager mock (minimal – only methods used by controller)
    private static Mock<RoleManager<IdentityRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        var validators = Array.Empty<IRoleValidator<IdentityRole>>();
        var normalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = new Mock<ILogger<RoleManager<IdentityRole>>>().Object;

        return new Mock<RoleManager<IdentityRole>>(store.Object, validators, normalizer, errors, logger);
    }

    // ───────── POST /api/autoschool/create – body missing sections ─────────
    [Fact]
    public async Task CreateAutoSchoolAsync_ShouldReturn400_When_BodyIncomplete()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AutoSchoolController(
            db,
            MockUserManager(db.Users).Object,
            MockRoleManager().Object);

        var dto = new CreateAutoSchoolDto { AutoSchool = null!, SchoolAdmin = null! };

        var result = await controller.CreateAutoSchoolAsync(dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("autoSchool");
    }

    // ───────── POST /api/autoschool/create – addressId invalid ─────────
    [Fact]
    public async Task CreateAutoSchoolAsync_ShouldReturn400_When_AddressMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AutoSchoolController(
            db,
            MockUserManager(db.Users).Object,
            MockRoleManager().Object);

        var dto = new CreateAutoSchoolDto
        {
            AutoSchool = new NewAutoSchoolDto
            {
                Name = "Test",
                PhoneNumber = "0700000000",
                Email = "test@school.ro",
                Status = "Active",
                AddressId = 99                   // no such address
            },
            SchoolAdmin = new NewSchoolAdminDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@school.ro",
                Phone = "0711000000",
                Password = "Passw0rd!"
            }
        };

        var result = await controller.CreateAutoSchoolAsync(dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("addressId");
    }

    // ───────── DELETE /api/autoschool/delete/{id} – not found ─────────
    [Fact]
    public async Task DeleteAutoSchoolAsync_ShouldReturn404_When_SchoolMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AutoSchoolController(
            db,
            MockUserManager(db.Users).Object,
            MockRoleManager().Object);

        var result = await controller.DeleteAutoSchoolAsync(777);

        var nf = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        nf.StatusCode.Should().Be(404);

        var msg = (string)nf.Value!.GetType().GetProperty("message")!.GetValue(nf.Value)!;
        msg.Should().Be("Auto-school not found");
    }

    // ───────── PUT /api/autoschool/update/{id} – school not found ─────────
    [Fact]
    public async Task UpdateAutoSchoolAsync_ShouldReturn404_When_SchoolMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AutoSchoolController(
            db,
            MockUserManager(db.Users).Object,
            MockRoleManager().Object);

        var dto = new AutoSchoolUpdateDto { Name = "NewName" };

        var result = await controller.UpdateAutoSchoolAsync(123, dto);

        var nf = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        nf.StatusCode.Should().Be(404);

        var msg = (string)nf.Value!.GetType().GetProperty("message")!.GetValue(nf.Value)!;
        msg.Should().Be("Auto-school not found");
    }
    private static void AttachSuperAdmin(ControllerBase c)
    {
        var identity = new ClaimsIdentity(
            new[] {
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim("userId", "superadmin1")
            },
            authenticationType: "mock");

        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    // ───────── PUT /api/autoschool/update/{id} – duplicate email ─────────
    [Fact]
    public async Task UpdateAutoSchoolAsync_ShouldReturn400_When_EmailDuplicate()
    {
        await using var db = CreateInMemoryDb();
        db.AutoSchools.AddRange(
            new AutoSchool { AutoSchoolId = 1, Name = "A1", Email = "dup@school.ro" },
            new AutoSchool { AutoSchoolId = 2, Name = "A2", Email = "original@school.ro" });
        await db.SaveChangesAsync();

        var controller = new AutoSchoolController(
            db,
            MockUserManager(db.Users).Object,
            MockRoleManager().Object);

        AttachSuperAdmin(controller);                 // <<— fix

        var dto = new AutoSchoolUpdateDto { Email = "dup@school.ro" };

        var result = await controller.UpdateAutoSchoolAsync(2, dto);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);

        var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
        msg.Should().Contain("e-mail");
    }

}
