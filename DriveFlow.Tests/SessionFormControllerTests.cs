using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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

namespace DriveFlow.Tests;

public class SessionFormControllerTests
{
    private static ApplicationDbContext InMemDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static void AttachIdentity(
        ControllerBase controller,
        string role,
        string userId = "user1")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static UserManager<ApplicationUser> GetMockedUserManager(ApplicationDbContext db)
    {
        var store = new UserStore<ApplicationUser>(db);
        var hasher = new PasswordHasher<ApplicationUser>();
        return new UserManager<ApplicationUser>(
            store,
            null,
            hasher,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    [Fact]
    public async Task StartSessionForm_ShouldReturn201_WhenValid()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            FirstName = "John",
            LastName = "Doe",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var student = new ApplicationUser
        {
            Id = "student1",
            UserName = "student@test.com",
            Email = "student@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(student, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1,
            SessionCost = 100,
            SessionDuration = 60,
            ScholarshipPrice = 1000,
            MinDrivingLessonsReq = 20
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today.AddDays(1),
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.StartSessionForm(100);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>();
        var createdResult = result.Result as CreatedResult;
        var dto = createdResult.Value as SessionFormDto;

        dto.Should().NotBeNull();
        dto.id_app.Should().Be(100);
        dto.id_formular.Should().Be(1);
        dto.isLocked.Should().BeFalse();
        dto.mistakesJson.Should().Be("[]");
        dto.finalizedAt.Should().BeNull();
        dto.totalPoints.Should().BeNull();
        dto.result.Should().BeNull();

        var sessionForm = await db.SessionForms.FirstOrDefaultAsync(sf => sf.AppointmentId == 100);
        sessionForm.Should().NotBeNull();
    }

    [Fact]
    public async Task StartSessionForm_ShouldReturn409_WhenFormAlreadyExists()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var student = new ApplicationUser
        {
            Id = "student1",
            UserName = "student@test.com",
            Email = "student@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(student, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1,
            SessionCost = 100,
            SessionDuration = 60,
            ScholarshipPrice = 1000,
            MinDrivingLessonsReq = 20
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today.AddDays(1),
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        // Pre-existing session form
        var existingForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(existingForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.StartSessionForm(100);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task StartSessionForm_ShouldReturn404_WhenAppointmentNotFound()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com"
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.StartSessionForm(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task StartSessionForm_ShouldReturn403_WhenInstructorNotOwner()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var student = new ApplicationUser
        {
            Id = "student1",
            UserName = "student@test.com",
            Email = "student@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(student, "Password123!");

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "other_instructor",
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today.AddDays(1),
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.StartSessionForm(100);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task StartSessionForm_ShouldReturn400_WhenInvalidAppointmentId()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.StartSessionForm(0);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateItem_ShouldReturn200_WhenIncrementingMistake()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Test Item",
            PenaltyPoints = 3,
            OrderIndex = 1
        };
        db.ExamItems.Add(examItem);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new UpdateMistakeRequest(id_item: 1, delta: 1);

        // Act
        var result = await controller.UpdateItem(1, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UpdateMistakeResponse>().Subject;
        response.id_item.Should().Be(1);
        response.count.Should().Be(1);

        // Verify in database
        var updated = await db.SessionForms.FindAsync(1);
        updated.MistakesJson.Should().Contain("\"id_item\":1");
        updated.MistakesJson.Should().Contain("\"count\":1");
    }

    [Fact]
    public async Task UpdateItem_ShouldReturn200_WhenDecrementingMistake()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Test Item",
            PenaltyPoints = 3,
            OrderIndex = 1
        };
        db.ExamItems.Add(examItem);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[{\"id_item\":1,\"count\":3}]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new UpdateMistakeRequest(id_item: 1, delta: -1);

        // Act
        var result = await controller.UpdateItem(1, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UpdateMistakeResponse>().Subject;
        response.id_item.Should().Be(1);
        response.count.Should().Be(2);
    }

    [Fact]
    public async Task UpdateItem_ShouldReturn200_WithCountZero_WhenDecrementingFromZero()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Test Item",
            PenaltyPoints = 3,
            OrderIndex = 1
        };
        db.ExamItems.Add(examItem);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new UpdateMistakeRequest(id_item: 1, delta: -1);

        // Act
        var result = await controller.UpdateItem(1, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UpdateMistakeResponse>().Subject;
        response.id_item.Should().Be(1);
        response.count.Should().Be(0);
    }

    [Fact]
    public async Task UpdateItem_ShouldReturn423_WhenFormIsLocked()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Test Item",
            PenaltyPoints = 3,
            OrderIndex = 1
        };
        db.ExamItems.Add(examItem);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = true,  // Form is locked
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new UpdateMistakeRequest(id_item: 1, delta: 1);

        // Act
        var result = await controller.UpdateItem(1, request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(423);
    }

    [Fact]
    public async Task UpdateItem_ShouldReturn403_WhenInstructorNotOwner()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Test Item",
            PenaltyPoints = 3,
            OrderIndex = 1
        };
        db.ExamItems.Add(examItem);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "other_instructor",  // Different instructor
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new UpdateMistakeRequest(id_item: 1, delta: 1);

        // Act
        var result = await controller.UpdateItem(1, request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateItem_ShouldReturn400_WhenItemNotInExamForm()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Test Item",
            PenaltyPoints = 3,
            OrderIndex = 1
        };
        db.ExamItems.Add(examItem);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new UpdateMistakeRequest(id_item: 999, delta: 1);  // Non-existent item

        // Act
        var result = await controller.UpdateItem(1, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Finalize_ShouldReturn200_WithOKResult_When21Points()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        db.ExamItems.AddRange(
            new ExamItem { ItemId = 1, FormId = 1, Description = "Item 1", PenaltyPoints = 3, OrderIndex = 1 },
            new ExamItem { ItemId = 2, FormId = 1, Description = "Item 2", PenaltyPoints = 5, OrderIndex = 2 }
        );

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        // Mistakes: 3*3 + 4*3 = 21 points (edge case - exactly at limit)
        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[{\"id_item\":1,\"count\":7}]",  // 7*3=21
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.Finalize(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FinalizeResponse>().Subject;
        response.id.Should().Be(1);
        response.totalPoints.Should().Be(21);
        response.maxPoints.Should().Be(21);
        response.result.Should().Be("OK");

        // Verify in database
        var updated = await db.SessionForms.FindAsync(1);
        updated.Should().NotBeNull();
        updated!.IsLocked.Should().BeTrue();
        updated.TotalPoints.Should().Be(21);
        updated.Result.Should().Be("OK");
        updated.FinalizedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Finalize_ShouldReturn200_WithFAILEDResult_When22Points()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        db.ExamItems.AddRange(
            new ExamItem { ItemId = 1, FormId = 1, Description = "Item 1", PenaltyPoints = 3, OrderIndex = 1 },
            new ExamItem { ItemId = 2, FormId = 1, Description = "Item 2", PenaltyPoints = 5, OrderIndex = 2 }
        );

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        // Mistakes: 5*3 + 2*5 = 25 points (over limit)
        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[{\"id_item\":1,\"count\":5},{\"id_item\":2,\"count\":2}]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.Finalize(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FinalizeResponse>().Subject;
        response.id.Should().Be(1);
        response.totalPoints.Should().Be(25);
        response.maxPoints.Should().Be(21);
        response.result.Should().Be("FAILED");

        // Verify in database
        var updated = await db.SessionForms.FindAsync(1);
        updated.Should().NotBeNull();
        updated!.IsLocked.Should().BeTrue();
        updated.TotalPoints.Should().Be(25);
        updated.Result.Should().Be("FAILED");
    }

    [Fact]
    public async Task Finalize_ShouldReturn200_WithZeroPoints_WhenNoMistakes()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        db.ExamItems.Add(new ExamItem { ItemId = 1, FormId = 1, Description = "Item 1", PenaltyPoints = 3, OrderIndex = 1 });

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",  // No mistakes
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.Finalize(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FinalizeResponse>().Subject;
        response.totalPoints.Should().Be(0);
        response.result.Should().Be("OK");
    }

    [Fact]
    public async Task Finalize_ShouldReturn423_WhenAlreadyLocked()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = true,  // Already locked
            TotalPoints = 15,
            Result = "OK",
            CreatedAt = DateTime.UtcNow,
            FinalizedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.Finalize(1);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(423);
    }

    [Fact]
    public async Task Finalize_ShouldReturn403_WhenInstructorNotOwner()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "other_instructor",  // Different instructor
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.Finalize(1);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Finalize_AndThenUpdateItem_ShouldReturn423()
    {
        // Arrange
        var db = InMemDb();
        var userManager = GetMockedUserManager(db);

        var instructor = new ApplicationUser
        {
            Id = "instructor1",
            UserName = "instructor@test.com",
            Email = "instructor@test.com",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(instructor, "Password123!");

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            Code = "B",
            AutoSchoolId = 1
        };
        db.TeachingCategories.Add(category);

        var examForm = new ExamForm
        {
            FormId = 1,
            TeachingCategoryId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        db.ExamItems.Add(new ExamItem { ItemId = 1, FormId = 1, Description = "Item 1", PenaltyPoints = 3, OrderIndex = 1 });

        var file = new DriveFlow_CRM_API.Models.File
        {
            FileId = 1,
            StudentId = "student1",
            InstructorId = "instructor1",
            TeachingCategoryId = 1,
            Status = FileStatus.APPROVED
        };
        db.Files.Add(file);

        var appointment = new Appointment
        {
            AppointmentId = 100,
            FileId = 1,
            Date = DateTime.Today,
            StartHour = new TimeSpan(10, 0, 0),
            EndHour = new TimeSpan(11, 0, 0)
        };
        db.Appointments.Add(appointment);

        var sessionForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[{\"id_item\":1,\"count\":2}]",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act - Finalize first
        var finalizeResult = await controller.Finalize(1);
        finalizeResult.Result.Should().BeOfType<OkObjectResult>();

        // Try to update after finalization
        var updateRequest = new UpdateMistakeRequest(id_item: 1, delta: 1);
        var updateResult = await controller.UpdateItem(1, updateRequest);

        // Assert - Should be locked
        var statusResult = updateResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(423);
    }
}
