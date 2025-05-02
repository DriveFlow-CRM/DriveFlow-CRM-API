using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Positive-path integration tests for <see cref="AutoSchoolController"/>:
/// GET / POST / PUT / DELETE.  
/// EF Core rulează in-memory, iar UserManager / RoleManager sunt mock-uite.
/// </summary>
public sealed class AutoSchoolPositiveTest
{
    // ───────── helpers ─────────
    private static ApplicationDbContext InMemDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"AutoSchool_Pos_{Guid.NewGuid()}")
               .Options);

    private static Mock<UserManager<ApplicationUser>> UM(IQueryable<ApplicationUser> users)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var m = new Mock<UserManager<ApplicationUser>>(store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

        m.SetupGet(x => x.Users).Returns(users);
        m.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
         .ReturnsAsync(IdentityResult.Success);
        m.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "SchoolAdmin"))
         .ReturnsAsync(IdentityResult.Success);
        return m;
    }

    private static Mock<RoleManager<IdentityRole>> RM(IdentityRole? role = null)
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        var m = new Mock<RoleManager<IdentityRole>>(store.Object,
                Array.Empty<IRoleValidator<IdentityRole>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object);

        m.Setup(r => r.FindByNameAsync("SchoolAdmin")).ReturnsAsync(role);
        m.Setup(r => r.RoleExistsAsync("SchoolAdmin")).ReturnsAsync(role != null);
        m.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);
        return m;
    }

    private static void AttachSuperAdmin(ControllerBase c)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim("userId", "sa1")
        }, "mock");
        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    // ───────── GET /api/autoschool/get ─────────
    [Fact]
    public async Task GetAutoSchoolsAsync_ShouldReturn200_WithExpectedData()
    {
        await using var db = InMemDb();

        db.Counties.Add(new County { CountyId = 1, Name = "Cluj", Abbreviation = "CJ" });
        db.Cities.Add(new City { CityId = 10, Name = "Cluj-Napoca", CountyId = 1 });
        db.Addresses.Add(new Address { AddressId = 30, StreetName = "Dorobanților", CityId = 10 });
        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 1, Name = "DriveFlow", Email = "contact@driveflow.ro", AddressId = 30 });
        db.Users.Add(new ApplicationUser { Id = "admin1", AutoSchoolId = 1, Email = "ana.pop@driveflow.ro", UserName = "ana.pop@driveflow.ro" });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = "admin1", RoleId = "r1" });
        await db.SaveChangesAsync();

        var controller = new AutoSchoolController(
            db,
            UM(db.Users).Object,
            RM(new IdentityRole("SchoolAdmin") { Id = "r1" }).Object);

        var result = await controller.GetAutoSchoolsAsync();

        var list = result.Should().BeOfType<OkObjectResult>().Subject
                         .Value.Should().BeAssignableTo<IEnumerable<AutoSchoolDto>>().Subject.ToList();

        list.Should().HaveCount(1);
        list.Single().Address!.StreetName.Should().Be("Dorobanților");
    }

    // ───────── POST /api/autoschool/create ─────────
    [Fact]
    public async Task CreateAutoSchoolAsync_ShouldReturn201_AndPersistEntities()
    {
        // use an in-memory DB that ignores the “transactions not supported” warning
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                     .UseInMemoryDatabase($"AutoSchool_Pos_{Guid.NewGuid()}")
                     .ConfigureWarnings(w => w.Ignore(
                         Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                     .Options;

        await using var db = new ApplicationDbContext(opts);
        db.Addresses.Add(new Address { AddressId = 50, StreetName = "Principală", CityId = 0 });
        await db.SaveChangesAsync();

        var userMgr = UM(db.Users);
        var roleMgr = RM(new IdentityRole("SchoolAdmin") { Id = "roleX" });
        var controller = new AutoSchoolController(db, userMgr.Object, roleMgr.Object);
        AttachSuperAdmin(controller);

        var dto = new CreateAutoSchoolDto
        {
            AutoSchool = new NewAutoSchoolDto
            {
                Name = "Start-Drive",
                PhoneNumber = "0700000000",
                Email = "office@startdrive.ro",
                Status = "Active",
                AddressId = 50
            },
            SchoolAdmin = new NewSchoolAdminDto
            {
                FirstName = "Mihai",
                LastName = "Ionescu",
                Email = "mihai@startdrive.ro",
                Phone = "0711000000",
                Password = "Passw0rd!"
            }
        };

        var result = await controller.CreateAutoSchoolAsync(dto);

        result.Should().BeOfType<CreatedResult>();
        db.AutoSchools.Should().ContainSingle(s => s.Name == "Start-Drive");
        userMgr.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Passw0rd!"), Times.Once);
    }


    // ───────── PUT /api/autoschool/update/{id} ─────────
    [Fact]
    public async Task UpdateAutoSchoolAsync_ShouldReturn200_AndModifyFields()
    {
        await using var db = InMemDb();
        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 2, Name = "Old", Email = "old@mail.com" });
        await db.SaveChangesAsync();

        var controller = new AutoSchoolController(db, UM(db.Users).Object, RM().Object);
        AttachSuperAdmin(controller);

        var dto = new AutoSchoolUpdateDto { Name = "New", Email = "new@mail.com" };

        var result = await controller.UpdateAutoSchoolAsync(2, dto);

        result.Should().BeOfType<OkObjectResult>();
        (await db.AutoSchools.FindAsync(2))!.Email.Should().Be("new@mail.com");
    }

    // ───────── DELETE /api/autoschool/delete/{id} ─────────
    [Fact]
    public async Task DeleteAutoSchoolAsync_ShouldReturn204_AndRemoveEntity()
    {
        await using var db = InMemDb();
        db.AutoSchools.Add(new AutoSchool { AutoSchoolId = 5, Name = "To-Delete" });
        await db.SaveChangesAsync();

        var controller = new AutoSchoolController(db, UM(db.Users).Object, RM().Object);
        AttachSuperAdmin(controller);

        var result = await controller.DeleteAutoSchoolAsync(5);

        result.Should().BeOfType<NoContentResult>();
        db.AutoSchools.Should().BeEmpty();
    }
}
