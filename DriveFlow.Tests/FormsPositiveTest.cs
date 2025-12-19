using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;
using DriveFlow_CRM_API.Models.DTOs;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="FormsController"/>.
/// The suite confirms HTTP 200/404 responses and correct data retrieval.
/// Authorization is bypassed by invoking the controller methods directly.
/// </summary>
public sealed class FormsPositiveTest
{
    // helper: isolated in-memory DbContext
    private static ApplicationDbContext CreateInMemoryDb()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"FormsTests_{Guid.NewGuid()}")
               .Options);

    // ───────────────────── GET /api/forms/by-category/{id_categ} – Success ─────────────────────
    [Fact]
    public async Task GetFormByCategoryAsync_ShouldReturn200_And_FormWithItems()
    {
        await using var db = CreateInMemoryDb();

        // Setup: Create a teaching category, form, and items
        var autoSchool = new AutoSchool
        {
            AutoSchoolId = 1,
            Name = "Test School",
            Email = "test@school.com",
            PhoneNumber = "1234567890",
            Status = AutoSchoolStatus.Active
        };
        db.AutoSchools.Add(autoSchool);

        var teachingCategory = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1,
            SessionCost = 100,
            SessionDuration = 60,
            ScholarshipPrice = 2000,
            MinDrivingLessonsReq = 20
        };
        db.TeachingCategories.Add(teachingCategory);

        var formular = new Formular
        {
            FormularId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.Formulars.Add(formular);

        db.Items.AddRange(
            new Item
            {
                ItemId = 1,
                FormularId = 1,
                Description = "Semnalizare la schimbarea direcției",
                PenaltyPoints = 3,
                OrderIndex = 1
            },
            new Item
            {
                ItemId = 2,
                FormularId = 1,
                Description = "Neasigurare la plecarea de pe loc",
                PenaltyPoints = 3,
                OrderIndex = 2
            }
        );
        await db.SaveChangesAsync();

        var controller = new FormsController(db);

        // Act
        var result = await controller.GetFormByCategoryAsync(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var formDto = ok.Value.Should().BeAssignableTo<FormDto>().Subject;

        formDto.id_formular.Should().Be(1);
        formDto.id_categ.Should().Be(1);
        formDto.maxPoints.Should().Be(21);
        formDto.items.Should().HaveCount(2);

        var itemsList = formDto.items.ToList();
        itemsList[0].id_item.Should().Be(1);
        itemsList[0].description.Should().Be("Semnalizare la schimbarea direcției");
        itemsList[0].penaltyPoints.Should().Be(3);
        itemsList[0].orderIndex.Should().Be(1);

        itemsList[1].id_item.Should().Be(2);
        itemsList[1].description.Should().Be("Neasigurare la plecarea de pe loc");
        itemsList[1].penaltyPoints.Should().Be(3);
        itemsList[1].orderIndex.Should().Be(2);
    }

    // ───────────────────── GET /api/forms/by-category/{id_categ} – Not Found ─────────────────────
    [Fact]
    public async Task GetFormByCategoryAsync_ShouldReturn404_WhenFormNotFound()
    {
        await using var db = CreateInMemoryDb();

        var controller = new FormsController(db);

        // Act
        var result = await controller.GetFormByCategoryAsync(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ───────────────────── GET /api/forms/by-category/{id_categ} – Bad Request ─────────────────────
    [Fact]
    public async Task GetFormByCategoryAsync_ShouldReturn400_WhenInvalidCategoryId()
    {
        await using var db = CreateInMemoryDb();

        var controller = new FormsController(db);

        // Act
        var result = await controller.GetFormByCategoryAsync(-1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ───────────────────── GET /api/forms/by-category/{id_categ} – Items Ordered ─────────────────────
    [Fact]
    public async Task GetFormByCategoryAsync_ShouldReturnItemsOrderedByOrderIndex()
    {
        await using var db = CreateInMemoryDb();

        // Setup
        var autoSchool = new AutoSchool
        {
            AutoSchoolId = 2,
            Name = "Test School 2",
            Email = "test2@school.com",
            PhoneNumber = "0987654321",
            Status = AutoSchoolStatus.Active
        };
        db.AutoSchools.Add(autoSchool);

        var teachingCategory = new TeachingCategory
        {
            TeachingCategoryId = 2,
            Code = "A",
            AutoSchoolId = 2,
            SessionCost = 150,
            SessionDuration = 60,
            ScholarshipPrice = 2500,
            MinDrivingLessonsReq = 15
        };
        db.TeachingCategories.Add(teachingCategory);

        var formular = new Formular
        {
            FormularId = 2,
            TeachingCategoryId = 2,
            MaxPoints = 15
        };
        db.Formulars.Add(formular);

        // Add items in non-sequential order
        db.Items.AddRange(
            new Item { FormularId = 2, Description = "Item 3", PenaltyPoints = 1, OrderIndex = 3 },
            new Item { FormularId = 2, Description = "Item 1", PenaltyPoints = 1, OrderIndex = 1 },
            new Item { FormularId = 2, Description = "Item 2", PenaltyPoints = 1, OrderIndex = 2 }
        );
        await db.SaveChangesAsync();

        var controller = new FormsController(db);

        // Act
        var result = await controller.GetFormByCategoryAsync(2);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var formDto = ok.Value.Should().BeAssignableTo<FormDto>().Subject;

        var itemsList = formDto.items.ToList();
        itemsList.Should().HaveCount(3);
        itemsList[0].description.Should().Be("Item 1");
        itemsList[1].description.Should().Be("Item 2");
        itemsList[2].description.Should().Be("Item 3");
    }
}
