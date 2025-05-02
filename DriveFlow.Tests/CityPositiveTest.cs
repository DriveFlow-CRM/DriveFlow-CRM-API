using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="CityController"/>.  
/// The suite confirms HTTP 200/201/204 responses, alphabetical ordering,
/// correct county filtering, and persistence/deletion when valid data is supplied.  
/// Authorization is bypassed by invoking the controller methods directly.
/// </summary>
public sealed class CityPositiveTest
{
    // helper: isolated in-memory DbContext
    private static ApplicationDbContext CreateInMemoryDb()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"CityTests_{Guid.NewGuid()}")
               .Options);

    // ───────────────────── GET /api/city – all cities ─────────────────────
    [Fact]
    public async Task GetCitiesAsync_ShouldReturn200_And_AlphabeticList()
    {
        await using var db = CreateInMemoryDb();

        var cluj = new County { CountyId = 1, Name = "Cluj", Abbreviation = "CJ" };
        var bihor = new County { CountyId = 2, Name = "Bihor", Abbreviation = "BH" };
        db.Counties.AddRange(cluj, bihor);

        db.Cities.AddRange(
            new City { Name = "Cluj-Napoca", CountyId = 1 },
            new City { Name = "Oradea", CountyId = 2 },
            new City { Name = "Apahida", CountyId = 1 });
        await db.SaveChangesAsync();

        var controller = new CityController(db);

        var result = await controller.GetCitiesAsync();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<CityDto>>().Subject.ToList();

        list.Should().HaveCount(3);
        list.Select(c => c.Name).Should().ContainInOrder("Apahida", "Cluj-Napoca", "Oradea");
        list.All(c => c.County is not null).Should().BeTrue();
    }

    // ───────────────────── GET /api/city?countyId=1 ─────────────────────
    [Fact]
    public async Task GetCitiesAsync_ShouldReturn200_And_FilterByCounty()
    {
        await using var db = CreateInMemoryDb();

        db.Counties.AddRange(
            new County { CountyId = 1, Name = "Cluj", Abbreviation = "CJ" },
            new County { CountyId = 2, Name = "Bihor", Abbreviation = "BH" });

        db.Cities.AddRange(
            new City { Name = "Cluj-Napoca", CountyId = 1 },
            new City { Name = "Apahida", CountyId = 1 },
            new City { Name = "Oradea", CountyId = 2 });
        await db.SaveChangesAsync();

        var controller = new CityController(db);

        var result = await controller.GetCitiesAsync(countyId: 1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<CityDto>>().Subject.ToList();

        list.Should().HaveCount(2);
        list.Should().OnlyContain(c => c.County!.CountyId == 1);
    }

    // ───────────────────── POST /api/city/create ─────────────────────
    [Fact]
    public async Task CreateCityAsync_ShouldReturn201_And_PersistEntity()
    {
        await using var db = CreateInMemoryDb();
        db.Counties.Add(new County { CountyId = 7, Name = "Timis", Abbreviation = "TM" });
        await db.SaveChangesAsync();

        var controller = new CityController(db);
        var dto = new CityCreateDto { Name = " Lugoj ", CountyId = 7 };

        var result = await controller.CreateCityAsync(dto);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Location.Should().StartWith("/api/city/");

        // inspect anonymous payload via reflection
        var payload = created.Value!;
        var t = payload.GetType();
        var idVal = (int)t.GetProperty("cityId")!.GetValue(payload)!;
        var msgVal = (string)t.GetProperty("message")!.GetValue(payload)!;

        idVal.Should().BeGreaterThan(0);
        msgVal.Should().Be("City created successfully");

        db.Cities.Should().ContainSingle(c => c.Name == "Lugoj" && c.CountyId == 7);
    }

    // ───────────────────── DELETE /api/city/{id} ─────────────────────
    [Fact]
    public async Task DeleteCityAsync_ShouldReturn204_And_RemoveEntity()
    {
        await using var db = CreateInMemoryDb();
        db.Counties.Add(new County { CountyId = 3, Name = "Sibiu", Abbreviation = "SB" });
        var city = new City { Name = "Sibiu", CountyId = 3 };
        db.Cities.Add(city);
        await db.SaveChangesAsync();

        var controller = new CityController(db);

        var result = await controller.DeleteCityAsync(city.CityId);

        result.Should().BeOfType<NoContentResult>();
        db.Cities.Should().BeEmpty();
    }
}
