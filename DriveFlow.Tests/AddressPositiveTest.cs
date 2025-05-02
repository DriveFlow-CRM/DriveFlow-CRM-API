using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="AddressController"/>.  
/// The suite confirms HTTP 200/201/204 responses, alphabetical ordering,
/// correct city filtering, and persistence/deletion when valid data is supplied.  
/// Authorization is bypassed by invoking the controller methods directly.
/// </summary>
public sealed class AddressPositiveTest
{
    // helper: isolated in-memory DbContext
    private static ApplicationDbContext CreateInMemoryDb()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"AddrTests_{Guid.NewGuid()}")
               .Options);

    // ───────────────────── GET /api/address/get – all addresses ─────────────────────
    [Fact]
    public async Task GetAddressesAsync_ShouldReturn200_And_AlphabeticList()
    {
        await using var db = CreateInMemoryDb();

        var cluj = new County { CountyId = 1, Name = "Cluj", Abbreviation = "CJ" };
        var bihor = new County { CountyId = 2, Name = "Bihor", Abbreviation = "BH" };
        db.Counties.AddRange(cluj, bihor);

        var cnp = new City { CityId = 10, Name = "Cluj-Napoca", CountyId = 1 };
        var apah = new City { CityId = 11, Name = "Apahida", CountyId = 1 };
        var orad = new City { CityId = 20, Name = "Oradea", CountyId = 2 };
        db.Cities.AddRange(cnp, apah, orad);

        db.Addresses.AddRange(
            new Address { StreetName = "Dorobanților", AddressNumber = "24", Postcode = "400117", CityId = 10 },
            new Address { StreetName = "Avram Iancu", AddressNumber = "15A", Postcode = "400120", CityId = 11 },
            new Address { StreetName = "Decebal", AddressNumber = "9", Postcode = "410001", CityId = 20 });
        await db.SaveChangesAsync();

        var controller = new AddressController(db);

        var result = await controller.GetAddressesAsync();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<AddressDto>>().Subject.ToList();

        list.Should().HaveCount(3);
        list.Select(a => a.StreetName).Should().ContainInOrder("Avram Iancu", "Decebal", "Dorobanților");
        list.All(a => a.City is not null).Should().BeTrue();
    }

    // ───────────────────── GET /api/address/get?cityId=10 ─────────────────────
    [Fact]
    public async Task GetAddressesAsync_ShouldReturn200_And_FilterByCity()
    {
        await using var db = CreateInMemoryDb();

        db.Counties.Add(new County { CountyId = 1, Name = "Cluj", Abbreviation = "CJ" });
        db.Cities.AddRange(
            new City { CityId = 10, Name = "Cluj-Napoca", CountyId = 1 },
            new City { CityId = 11, Name = "Apahida", CountyId = 1 });
        db.Addresses.AddRange(
            new Address { StreetName = "Dorobanților", CityId = 10 },
            new Address { StreetName = "Avram Iancu", CityId = 11 });
        await db.SaveChangesAsync();

        var controller = new AddressController(db);

        var result = await controller.GetAddressesAsync(cityId: 10);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<AddressDto>>().Subject.ToList();

        list.Should().HaveCount(1);
        list.Single().City!.CityId.Should().Be(10);
    }

    // ───────────────────── POST /api/address/create ─────────────────────
    [Fact]
    public async Task CreateAddressAsync_ShouldReturn201_And_PersistEntity()
    {
        await using var db = CreateInMemoryDb();
        db.Counties.Add(new County { CountyId = 3, Name = "Timis", Abbreviation = "TM" });
        db.Cities.Add(new City { CityId = 30, Name = "Lugoj", CountyId = 3 });
        await db.SaveChangesAsync();

        var controller = new AddressController(db);

        var dto = new AddressCreateDto
        {
            StreetName = " Morilor ",
            AddressNumber = " 7B ",
            Postcode = "305500",
            CityId = 30
        };

        var result = await controller.CreateAddressAsync(dto);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Location.Should().StartWith("/api/address/");

        // inspect anonymous payload via reflection
        var payload = created.Value!;
        var t = payload.GetType();
        var idVal = (int)t.GetProperty("addressId")!.GetValue(payload)!;
        var msgVal = (string)t.GetProperty("message")!.GetValue(payload)!;

        idVal.Should().BeGreaterThan(0);
        msgVal.Should().Be("Address created successfully");

        db.Addresses.Should().ContainSingle(a =>
            a.StreetName == "Morilor" &&
            a.AddressNumber == "7B" &&
            a.Postcode == "305500" &&
            a.CityId == 30);
    }

    // ───────────────────── DELETE /api/address/delete/{id} ─────────────────────
    [Fact]
    public async Task DeleteAddressAsync_ShouldReturn204_And_RemoveEntity()
    {
        await using var db = CreateInMemoryDb();
        db.Counties.Add(new County { CountyId = 4, Name = "Sibiu", Abbreviation = "SB" });
        db.Cities.Add(new City { CityId = 40, Name = "Sibiu", CountyId = 4 });
        var addr = new Address { StreetName = "Bălcescu", AddressNumber = "1", CityId = 40 };
        db.Addresses.Add(addr);
        await db.SaveChangesAsync();

        var controller = new AddressController(db);

        var result = await controller.DeleteAddressAsync(addr.AddressId);

        result.Should().BeOfType<NoContentResult>();
        db.Addresses.Should().BeEmpty();
    }
}
