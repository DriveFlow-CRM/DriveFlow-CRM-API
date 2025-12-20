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
using File = DriveFlow_CRM_API.Models.File;

namespace DriveFlow.Tests.Controllers;

/// <summary>
/// Integration tests for GET /api/students/{id_student}/session-forms endpoint.
/// Tests student history with filtering, sorting, and pagination.
/// </summary>
public sealed class SessionFormHistoryTest
{
    private static ApplicationDbContext InMemDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseInMemoryDatabase($"SessionFormHistory_{Guid.NewGuid()}")
               .Options);

    private static void AttachIdentity(
        ControllerBase controller,
        string role,
        string userId = "user1")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, role),
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

    private static UserManager<ApplicationUser> GetMockedUserManager(ApplicationDbContext db)
    {
        var store = new UserStore<ApplicationUser>(db);
        var options = new Microsoft.Extensions.Options.OptionsWrapper<IdentityOptions>(new IdentityOptions());
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        return new UserManager<ApplicationUser>(
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
    }

    private async Task<string> SetupTestData(ApplicationDbContext db, string studentId = "student1", string instructorId = "instructor1")
    {
        // Setup users
        var student = new ApplicationUser { Id = studentId, UserName = $"{studentId}@test.com", AutoSchoolId = 1 };
        var instructor = new ApplicationUser { Id = instructorId, UserName = $"{instructorId}@test.com", AutoSchoolId = 1 };
        db.Users.AddRange(student, instructor);

        // Setup category
        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1,
            SessionCost = 100,
            SessionDuration = 60,
            ScholarshipPrice = 2500,
            MinDrivingLessonsReq = 30
        };
        db.TeachingCategories.Add(category);

        // Setup exam form
        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21,
            Items = new List<ExamItem>
            {
                new ExamItem { ItemId = 1, Description = "Semnalizare", PenaltyPoints = 3, OrderIndex = 1 }
            }
        };
        db.ExamForms.Add(examForm);

        // Setup file
        var file = new File
        {
            FileId = 1,
            StudentId = studentId,
            TeachingCategoryId = 1,
            InstructorId = instructorId,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        await db.SaveChangesAsync();
        return studentId;
    }

    private async Task AddSessionForm(ApplicationDbContext db, int appointmentId, DateTime date, int? totalPoints, string? result)
    {
        var appointment = new Appointment
        {
            AppointmentId = appointmentId,
            FileId = 1,
            Date = date,
            StartHour = TimeSpan.FromHours(10),
            EndHour = TimeSpan.FromHours(11)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            AppointmentId = appointmentId,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = totalPoints.HasValue,
            CreatedAt = DateTime.UtcNow,
            TotalPoints = totalPoints,
            Result = result,
            FinalizedAt = totalPoints.HasValue ? DateTime.UtcNow : null
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();
    }

    // ????????????? Happy Path: Student views own forms ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturn200_ForOwnStudent()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        // Add 3 session forms with different dates
        await AddSessionForm(db, 1, DateTime.Today.AddDays(-10), 18, "OK");
        await AddSessionForm(db, 2, DateTime.Today.AddDays(-5), 24, "FAILED");
        await AddSessionForm(db, 3, DateTime.Today.AddDays(-1), null, null); // Not finalized

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Student", userId: studentId);

        var result = await controller.ListStudentForms(studentId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<SessionFormListItemDto>>().Subject;
        pagedResult.total.Should().Be(3);
        pagedResult.page.Should().Be(1);
        pagedResult.pageSize.Should().Be(20);

        var items = pagedResult.items.ToList();
        items.Should().HaveCount(3);

        // Verify sorted by date descending
        items[0].date.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));
        items[1].date.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(-5)));
        items[2].date.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(-10)));

        // Verify not finalized form has null values
        items[0].totalPoints.Should().BeNull();
        items[0].result.Should().BeNull();

        // Verify finalized forms
        items[1].totalPoints.Should().Be(24);
        items[1].result.Should().Be("FAILED");
        items[2].totalPoints.Should().Be(18);
        items[2].result.Should().Be("OK");
    }

    // ????????????? Date Filtering: from parameter ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldFilterByFromDate()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        await AddSessionForm(db, 1, DateTime.Today.AddDays(-10), 18, "OK");
        await AddSessionForm(db, 2, DateTime.Today.AddDays(-5), 24, "FAILED");
        await AddSessionForm(db, 3, DateTime.Today.AddDays(-1), 20, "OK");

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Student", userId: studentId);

        var fromDate = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd");
        var result = await controller.ListStudentForms(studentId, from: fromDate);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<SessionFormListItemDto>>().Subject;

        pagedResult.total.Should().Be(2); // Only forms from -5 and -1
        pagedResult.items.ToList()[0].date.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));
        pagedResult.items.ToList()[1].date.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(-5)));
    }

    // ????????????? Date Filtering: to parameter ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldFilterByToDate()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        await AddSessionForm(db, 1, DateTime.Today.AddDays(-10), 18, "OK");
        await AddSessionForm(db, 2, DateTime.Today.AddDays(-5), 24, "FAILED");
        await AddSessionForm(db, 3, DateTime.Today.AddDays(-1), 20, "OK");

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Student", userId: studentId);

        var toDate = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd");
        var result = await controller.ListStudentForms(studentId, to: toDate);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<SessionFormListItemDto>>().Subject;

        pagedResult.total.Should().Be(1); // Only form from -10
        pagedResult.items.ToList()[0].date.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(-10)));
    }

    // ????????????? Pagination: page 2 ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldPaginateCorrectly()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        // Add 5 session forms
        for (int i = 0; i < 5; i++)
        {
            await AddSessionForm(db, i + 1, DateTime.Today.AddDays(-i), 18, "OK");
        }

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Student", userId: studentId);

        // Get page 2 with pageSize=2
        var result = await controller.ListStudentForms(studentId, page: 2, pageSize: 2);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<SessionFormListItemDto>>().Subject;

        pagedResult.total.Should().Be(5);
        pagedResult.page.Should().Be(2);
        pagedResult.pageSize.Should().Be(2);
        pagedResult.items.ToList().Should().HaveCount(2);
    }

    // ????????????? Instructor can view student forms ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturn200_ForInstructorWithActiveFile()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db, studentId: "student1", instructorId: "instructor1");

        await AddSessionForm(db, 1, DateTime.Today.AddDays(-1), 18, "OK");

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Instructor", userId: "instructor1");

        var result = await controller.ListStudentForms(studentId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    // ????????????? Instructor forbidden for other students ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturn403_ForInstructorWithoutFile()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db, studentId: "student1", instructorId: "instructor1");

        // Add instructor2 who has no file with student1
        var instructor2 = new ApplicationUser { Id = "instructor2", UserName = "instructor2@test.com", AutoSchoolId = 1 };
        db.Users.Add(instructor2);
        await db.SaveChangesAsync();

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Instructor", userId: "instructor2"); // Different instructor

        var result = await controller.ListStudentForms(studentId);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ????????????? SchoolAdmin can view school students ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturn200_ForSchoolAdminSameSchool()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        var admin = new ApplicationUser { Id = "admin1", UserName = "admin@test.com", AutoSchoolId = 1 };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        await AddSessionForm(db, 1, DateTime.Today.AddDays(-1), 18, "OK");

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "admin1");

        var result = await controller.ListStudentForms(studentId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    // ????????????? SchoolAdmin forbidden for different school ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturn403_ForSchoolAdminDifferentSchool()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        var admin = new ApplicationUser { Id = "admin1", UserName = "admin@test.com", AutoSchoolId = 2 }; // Different school
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "admin1");

        var result = await controller.ListStudentForms(studentId);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ????????????? Student forbidden to view other students ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturn403_ForDifferentStudent()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db, studentId: "student1");

        var student2 = new ApplicationUser { Id = "student2", UserName = "student2@test.com", AutoSchoolId = 1 };
        db.Users.Add(student2);
        await db.SaveChangesAsync();

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Student", userId: "student2");

        var result = await controller.ListStudentForms(studentId);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ????????????? Student not found ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturn404_WhenStudentNotFound()
    {
        await using var db = InMemDb();

        var admin = new ApplicationUser { Id = "admin1", UserName = "admin@test.com", AutoSchoolId = 1 };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "SchoolAdmin", userId: "admin1");

        var result = await controller.ListStudentForms("nonexistent");

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    // ????????????? Invalid pagination parameters ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturn400_WhenPageIsZero()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Student", userId: studentId);

        var result = await controller.ListStudentForms(studentId, page: 0);

        var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ListStudentForms_ShouldReturn400_WhenPageSizeExceedsMax()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Student", userId: studentId);

        var result = await controller.ListStudentForms(studentId, pageSize: 101);

        var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.StatusCode.Should().Be(400);
    }

    // ????????????? Empty result ?????????????
    [Fact]
    public async Task ListStudentForms_ShouldReturnEmptyList_WhenNoFormsExist()
    {
        await using var db = InMemDb();
        var studentId = await SetupTestData(db);

        var userManager = GetMockedUserManager(db);
        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, role: "Student", userId: studentId);

        var result = await controller.ListStudentForms(studentId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<SessionFormListItemDto>>().Subject;

        pagedResult.total.Should().Be(0);
        pagedResult.items.Should().BeEmpty();
    }
}
