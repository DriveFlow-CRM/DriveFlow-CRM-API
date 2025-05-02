using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;
namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="CountyController"/>.  
/// The suite verifies HTTP 200/201/204 responses, alphabetical ordering, and
/// correct persistence/deletion when valid data is supplied.  
/// Authorization is bypassed by invoking the controller methods directly.
/// </summary>
public sealed class CountyPositiveTest
{
    // ───────── Helper: create an isolated in-memory ApplicationDbContext ─────────
    private static ApplicationDbContext CreateInMemoryDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CountyTests_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(opts);
    }

    // ───────────────────── GET /api/county/get ─────────────────────
    [Fact]
    public async Task GetCountiesAsync_ShouldReturn200_And_AlphabeticList()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        db.Counties.AddRange(
            new County { Name = "Bihor", Abbreviation = "BH" },
            new County { Name = "Cluj", Abbreviation = "CJ" },
            new County { Name = "Alba", Abbreviation = "AB" });
        await db.SaveChangesAsync();

        var controller = new CountyController(db);

        // Act
        var result = await controller.GetCountiesAsync();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<CountyDto>>().Subject.ToList();

        list.Should().HaveCount(3);
        list.Select(c => c.Name).Should().ContainInOrder("Alba", "Bihor", "Cluj");
        list.Select(c => c.Name).Should().BeInAscendingOrder();
    }

    // ───────────────────── POST /api/county ─────────────────────
    [Fact]
    public async Task CreateCountyAsync_ShouldReturn201_And_PersistEntity()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var controller = new CountyController(db);
        var dto = new CountyCreateDto { Name = " Timis ", Abbreviation = "tm" };

        // Act
        var result = await controller.CreateCountyAsync(dto);

        // Assert – HTTP 201 and Location header
        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Location.Should().StartWith("/api/county/");

        // Inspect the anonymous payload via reflection
        var value = created.Value!;
        var type = value.GetType();

        var idProp = type.GetProperty("countyId") ?? type.GetProperty("CountyId");
        var msgProp = type.GetProperty("message") ?? type.GetProperty("Message");

        idProp.Should().NotBeNull("payload should contain countyId");
        msgProp.Should().NotBeNull("payload should contain message");

        ((int)idProp!.GetValue(value)!).Should().BeGreaterThan(0);
        ((string)msgProp!.GetValue(value)!).Should().Be("County created successfully");

        // Assert – entity is persisted with trimmed / upper-case fields
        db.Counties.Should().ContainSingle(c => c.Name == "Timis" && c.Abbreviation == "TM");
    }
    // ───────────────────── DELETE /api/county/{id} ─────────────────────
    [Fact]
    public async Task DeleteCountyAsync_ShouldReturn204_And_RemoveEntity()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var county = new County { Name = "Sibiu", Abbreviation = "SB" };
        db.Counties.Add(county);
        await db.SaveChangesAsync();

        var controller = new CountyController(db);

        // Act
        var result = await controller.DeleteCountyAsync(county.CountyId);

        // Assert – HTTP 204 and entity removed
        result.Should().BeOfType<NoContentResult>();
        db.Counties.Should().BeEmpty();
    }
}
