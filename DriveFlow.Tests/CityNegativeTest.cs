using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests that cover the *negative paths* of <see cref="CityController"/>:
/// • 400 Bad Request for invalid payloads or duplicates  
/// • 404 Not Found for missing county or city entities.  
/// Authorization is bypassed by calling the controller methods directly.
/// </summary>
public sealed class CityNegativeTest
{
    // helper: isolated in-memory DbContext
    private static ApplicationDbContext CreateInMemoryDb()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"CityNegTests_{Guid.NewGuid()}")
               .Options);

    // ───────────────────── GET /api/city?countyId=-5 ─────────────────────
    [Fact]
    public async Task GetCitiesAsync_ShouldReturn400_When_CountyIdNegative()
    {
        await using var db = CreateInMemoryDb();
        var controller = new CityController(db);

        var result = await controller.GetCitiesAsync(countyId: -5);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        var msg = (string)badReq.Value!.GetType().GetProperty("message")!.GetValue(badReq.Value)!;
        msg.Should().Contain("countyId");
    }

    // ───────────────────── GET /api/city?countyId=99 (county missing) ─────────────────────
    [Fact]
    public async Task GetCitiesAsync_ShouldReturn404_When_CountyMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new CityController(db);

        var result = await controller.GetCitiesAsync(countyId: 99);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);

        var msg = (string)notFound.Value!.GetType().GetProperty("message")!.GetValue(notFound.Value)!;
        msg.Should().Be("County not found.");
    }

    // ───────────────────── POST /api/city/create – empty name / bad countyId ─────────────────────
    [Fact]
    public async Task CreateCityAsync_ShouldReturn400_When_NameOrCountyIdInvalid()
    {
        await using var db = CreateInMemoryDb();
        var controller = new CityController(db);
        var dto = new CityCreateDto { Name = "  ", CountyId = 0 };

        var result = await controller.CreateCityAsync(dto);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        var msg = (string)badReq.Value!.GetType().GetProperty("message")!.GetValue(badReq.Value)!;
        msg.Should().Contain("required");
    }

    // ───────────────────── POST /api/city/create – county does not exist ─────────────────────
    [Fact]
    public async Task CreateCityAsync_ShouldReturn400_When_CountyDoesNotExist()
    {
        await using var db = CreateInMemoryDb();
        var controller = new CityController(db);
        var dto = new CityCreateDto { Name = "Lugoj", CountyId = 42 };

        var result = await controller.CreateCityAsync(dto);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        var msg = (string)badReq.Value!.GetType().GetProperty("message")!.GetValue(badReq.Value)!;
        msg.Should().Contain("does not exist");
    }

    // ───────────────────── POST /api/city/create – duplicate name in county ─────────────────────
    [Fact]
    public async Task CreateCityAsync_ShouldReturn400_When_DuplicateNameInSameCounty()
    {
        await using var db = CreateInMemoryDb();
        db.Counties.Add(new County { CountyId = 5, Name = "Cluj", Abbreviation = "CJ" });
        db.Cities.Add(new City { Name = "Apahida", CountyId = 5 });
        await db.SaveChangesAsync();

        var controller = new CityController(db);
        var dto = new CityCreateDto { Name = " APAHIDA ", CountyId = 5 };

        var result = await controller.CreateCityAsync(dto);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        var msg = (string)badReq.Value!.GetType().GetProperty("message")!.GetValue(badReq.Value)!;
        msg.Should().Contain("already exists");
        db.Cities.Should().HaveCount(1);
    }

    // ───────────────────── DELETE /api/city/{id} – not found ─────────────────────
    [Fact]
    public async Task DeleteCityAsync_ShouldReturn404_When_EntityMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new CityController(db);

        var result = await controller.DeleteCityAsync(cityId: 1234);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);

        var msg = (string)notFound.Value!.GetType().GetProperty("message")!.GetValue(notFound.Value)!;
        msg.Should().Be("City not found");
    }
}
