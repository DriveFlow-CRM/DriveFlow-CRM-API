using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests that exercise the *negative paths* of <see cref="CountyController"/>:
/// • 400 Bad Request on invalid or duplicate data  
/// • 404 Not Found when attempting to delete a non-existent county.  
/// Authorization is ignored; methods are invoked directly.
/// </summary>
public sealed class CountyNegativeTest
{
    // helper: isolated in-memory DbContext
    private static ApplicationDbContext CreateInMemoryDb()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"CountyNegTests_{Guid.NewGuid()}")
               .Options);

    // ───────────────────── POST /api/county – empty name/abbr ─────────────────────
    [Fact]
    public async Task CreateCountyAsync_ShouldReturn400_When_NameOrAbbreviationMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new CountyController(db);
        var dto = new CountyCreateDto { Name = "  ", Abbreviation = "" };

        var result = await controller.CreateCountyAsync(dto);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        // ensure message exists
        var msgProp = badReq.Value!.GetType().GetProperty("message");
        msgProp.Should().NotBeNull();
        ((string)msgProp!.GetValue(badReq.Value)!).Should().Contain("required");
        db.Counties.Should().BeEmpty();
    }

    // ───────────────────── POST /api/county – duplicate name/abbr ─────────────────────
    [Fact]
    public async Task CreateCountyAsync_ShouldReturn400_When_DuplicateExists()
    {
        await using var db = CreateInMemoryDb();
        db.Counties.Add(new County { Name = "Cluj", Abbreviation = "CJ" });
        await db.SaveChangesAsync();

        var controller = new CountyController(db);
        var dto = new CountyCreateDto { Name = "CLuJ", Abbreviation = "cj" }; // duplicates (case-insensitive)

        var result = await controller.CreateCountyAsync(dto);

        var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badReq.StatusCode.Should().Be(400);

        var msgProp = badReq.Value!.GetType().GetProperty("message");
        ((string)msgProp!.GetValue(badReq.Value)!).Should().Contain("already exists");

        db.Counties.Should().HaveCount(1); // nothing added
    }

    // ───────────────────── DELETE /api/county/{id} – not found ─────────────────────
    [Fact]
    public async Task DeleteCountyAsync_ShouldReturn404_When_EntityMissing()
    {
        await using var db = CreateInMemoryDb();
        var controller = new CountyController(db);

        var result = await controller.DeleteCountyAsync(countyId: 999);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);

        var msgProp = notFound.Value!.GetType().GetProperty("message");
        ((string)msgProp!.GetValue(notFound.Value)!).Should().Be("County not found");
    }
}
