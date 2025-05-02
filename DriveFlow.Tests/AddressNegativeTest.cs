using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DriveFlow_CRM_API.Controllers;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests that cover the **negative paths** of <see cref="AddressController"/>:
/// • 400 Bad Request for invalid payloads or when the parent city is missing  
/// • 404 Not Found for non-existent city/address entities.  
/// Authorization is bypassed by calling the controller methods directly.
/// </summary>
public sealed class AddressNegativeTest
{
    // helper: isolated in-memory DbContext
    private static ApplicationDbContext CreateInMemoryDb()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"AddrNegTests_{Guid.NewGuid()}")
               .Options);

    // ───────────────────── GET /api/address/get?cityId=-3 ─────────────────────
    [Fact]
    public async Task GetAddressesAsync_ShouldReturn400_When_CityIdNegative()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AddressController(db);

        var result = await controller.GetAddressesAsync(cityId: -3);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        var msg = (string)badReq.Value!.GetType().GetProperty("message")!.GetValue(badReq.Value)!;
        msg.Should().Contain("cityId");
    }

    // ───────────────────── GET /api/address/get?cityId=99 (city missing) ─────────────────────
    [Fact]
    public async Task GetAddressesAsync_ShouldReturn404_When_CityMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AddressController(db);

        var result = await controller.GetAddressesAsync(cityId: 99);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);

        var msg = (string)notFound.Value!.GetType().GetProperty("message")!.GetValue(notFound.Value)!;
        msg.Should().Be("City not found.");
    }

    // ───────────────────── POST /api/address/create – invalid fields ─────────────────────
    [Fact]
    public async Task CreateAddressAsync_ShouldReturn400_When_FieldsInvalid()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AddressController(db);

        var dto = new AddressCreateDto
        {
            StreetName = " ",
            AddressNumber = "",
            Postcode = null!,
            CityId = 0
        };

        var result = await controller.CreateAddressAsync(dto);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        var msg = (string)badReq.Value!.GetType().GetProperty("message")!.GetValue(badReq.Value)!;
        msg.Should().Contain("required");
    }

    // ───────────────────── POST /api/address/create – city does not exist ─────────────────────
    [Fact]
    public async Task CreateAddressAsync_ShouldReturn400_When_CityDoesNotExist()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AddressController(db);

        var dto = new AddressCreateDto
        {
            StreetName = "Morilor",
            AddressNumber = "7B",
            Postcode = "305500",
            CityId = 42
        };

        var result = await controller.CreateAddressAsync(dto);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        var msg = (string)badReq.Value!.GetType().GetProperty("message")!.GetValue(badReq.Value)!;
        msg.Should().Contain("does not exist");
    }

    // ───────────────────── DELETE /api/address/delete/{id} – not found ─────────────────────
    [Fact]
    public async Task DeleteAddressAsync_ShouldReturn404_When_AddressMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new AddressController(db);

        var result = await controller.DeleteAddressAsync(addressId: 555);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);

        var msg = (string)notFound.Value!.GetType().GetProperty("message")!.GetValue(notFound.Value)!;
        msg.Should().Be("Address not found");
    }
}
