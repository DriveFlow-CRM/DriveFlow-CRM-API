using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;
using DriveFlow_CRM_API.Models.DTOs;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Positive-path integration tests for <see cref="ExamFormController"/>.
/// Tests focus on GET /api/forms/by-category/{id_categ} and by-license/{licenseId} endpoints.
/// Runs against an in-memory EF Core database; authentication is faked via claims.
/// </summary>
public sealed class ExamFormPositiveTest
{
    // ─────────────────────── helpers ───────────────────────
    private static ApplicationDbContext InMemDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"ExamForm_Pos_{Guid.NewGuid()}")
               .Options);

    private static void AttachIdentity(
        ControllerBase controller,
        string role,
        string userId = "user1")
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

    // ─────────────────────── GET /api/forms/by-category/{id_categ} – Happy Path ───────────────────────
    [Fact]
    public async Task GetFormByCategory_ShouldReturn200_WithFormAndOrderedItems()
    {
        await using var db = InMemDb();

        // Setup: Create a license
        var license = new DriveFlow_CRM_API.Models.License
        {
            LicenseId = 1,
            Type = "B"
        };
        db.Licenses.Add(license);

        // Setup: Create a teaching category linked to the license
        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
            Code = "B",
            AutoSchoolId = 1,
            SessionCost = 100,
            SessionDuration = 60,
            ScholarshipPrice = 2500,
            MinDrivingLessonsReq = 30
        };
        db.TeachingCategories.Add(category);
        await db.SaveChangesAsync();

        // Setup: Create exam form with items (linked to license)
        var form = new ExamForm
        {
            FormId = 1,
            LicenseId = 1,
            MaxPoints = 21,
            Items = new List<ExamItem>
            {
                new ExamItem { ItemId = 1, Description = "Semnalizare", PenaltyPoints = 3, OrderIndex = 1 },
                new ExamItem { ItemId = 2, Description = "Neasigurare", PenaltyPoints = 3, OrderIndex = 2 },
                new ExamItem { ItemId = 3, Description = "Depășire", PenaltyPoints = 5, OrderIndex = 3 }
            }
        };
        db.ExamForms.Add(form);
        await db.SaveChangesAsync();

        // Act
        var userManager = GetMockedUserManager(db);
        var controller = new ExamFormController(db, userManager);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "user1");

        var result = await controller.GetFormByCategory(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var formDto = okResult.Value.Should().BeOfType<ExamFormDto>().Subject;
        formDto.id_formular.Should().Be(1);
        formDto.licenseId.Should().Be(1);
        formDto.licenseType.Should().Be("B");
        formDto.maxPoints.Should().Be(21);

        var items = formDto.items.ToList();
        items.Should().HaveCount(3);
        items[0].id_item.Should().Be(1);
        items[0].description.Should().Be("Semnalizare");
        items[0].penaltyPoints.Should().Be(3);
        items[0].orderIndex.Should().Be(1);

        items[1].description.Should().Be("Neasigurare");
        items[2].description.Should().Be("Depășire");
    }

    // ─────────────────────── GET /api/forms/by-category/{id_categ} – Not Found ───────────────────────
    [Fact]
    public async Task GetFormByCategory_ShouldReturn404_WhenCategoryNotFound()
    {
        await using var db = InMemDb();

        var userManager = GetMockedUserManager(db);
        var controller = new ExamFormController(db, userManager);
        AttachIdentity(controller, role: "SchoolAdmin");

        var result = await controller.GetFormByCategory(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    // ─────────────────────── GET /api/forms/by-category/{id_categ} – Invalid ID ───────────────────────
    [Fact]
    public async Task GetFormByCategory_ShouldReturn400_WhenCategoryIdInvalid()
    {
        await using var db = InMemDb();

        var userManager = GetMockedUserManager(db);
        var controller = new ExamFormController(db, userManager);
        AttachIdentity(controller, role: "SchoolAdmin");

        var result = await controller.GetFormByCategory(-1);

        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.StatusCode.Should().Be(400);
    }

    // ─────────────────────── GET /api/forms/by-license/{licenseId} – Happy Path ───────────────────────
    [Fact]
    public async Task GetFormByLicense_ShouldReturn200_WithFormAndOrderedItems()
    {
        await using var db = InMemDb();

        // Setup: Create a license
        var license = new DriveFlow_CRM_API.Models.License
        {
            LicenseId = 6,
            Type = "B"
        };
        db.Licenses.Add(license);

        // Setup: Create exam form with items (linked to license)
        var form = new ExamForm
        {
            FormId = 6,
            LicenseId = 6,
            MaxPoints = 21,
            Items = new List<ExamItem>
            {
                new ExamItem { ItemId = 1, Description = "Semnalizare", PenaltyPoints = 3, OrderIndex = 1 },
                new ExamItem { ItemId = 2, Description = "Neasigurare", PenaltyPoints = 3, OrderIndex = 2 }
            }
        };
        db.ExamForms.Add(form);
        await db.SaveChangesAsync();

        // Act
        var userManager = GetMockedUserManager(db);
        var controller = new ExamFormController(db, userManager);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "user1");

        var result = await controller.GetFormByLicense(6);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var formDto = okResult.Value.Should().BeOfType<ExamFormDto>().Subject;
        formDto.id_formular.Should().Be(6);
        formDto.licenseId.Should().Be(6);
        formDto.licenseType.Should().Be("B");
        formDto.maxPoints.Should().Be(21);
    }

    // ─────────────────────── Helper to mock UserManager ───────────────────────
    private static UserManager<ApplicationUser> GetMockedUserManager(ApplicationDbContext db)
    {
        var store = new UserStore<ApplicationUser>(db);
        var options = new Microsoft.Extensions.Options.OptionsWrapper<IdentityOptions>(new IdentityOptions());
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        var userManager = new UserManager<ApplicationUser>(
            store,
            options,
            new PasswordHasher<ApplicationUser>(),
            new[] { new UserValidator<ApplicationUser>() },
            new[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            loggerFactory.CreateLogger<UserManager<ApplicationUser>>()
        );
        return userManager;
    }
}
