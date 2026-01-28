using System;
using System.Collections.Generic;
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

    // ────────────────────────────── SUBMIT SESSION FORM TESTS ──────────────────────────────

    [Fact]
    public async Task SubmitSessionForm_ShouldReturn201_WhenValid_WithOKResult()
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

        var license = new DriveFlow_CRM_API.Models.License { LicenseId = 1, Type = "B" };
        db.Licenses.Add(license);

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
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
            LicenseId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem1 = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Semnalizare",
            PenaltyPoints = 3
        };
        var examItem2 = new ExamItem
        {
            ItemId = 2,
            FormId = 1,
            Description = "Depasire",
            PenaltyPoints = 5
        };
        db.ExamItems.AddRange(examItem1, examItem2);

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

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new SubmitSessionFormRequest(
            Mistakes: new List<MistakeItemDto>
            {
                new MistakeItemDto(IdItem: 1, Count: 2),  // 2 * 3 = 6 points
                new MistakeItemDto(IdItem: 2, Count: 1)   // 1 * 5 = 5 points
            },
            MaxPoints: 21
        );

        // Act
        var result = await controller.SubmitSessionForm(100, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<SubmitSessionFormResponse>().Subject;

        response.TotalPoints.Should().Be(11);  // 6 + 5 = 11
        response.MaxPoints.Should().Be(21);
        response.Result.Should().Be("OK");  // 11 <= 21, so OK

        // Verify in database
        var savedForm = await db.SessionForms.FirstOrDefaultAsync();
        savedForm.Should().NotBeNull();
        savedForm!.TotalPoints.Should().Be(11);
        savedForm.Result.Should().Be("OK");
        savedForm.FinalizedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitSessionForm_ShouldReturn201_WhenValid_WithFAILEDResult()
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

        var license = new DriveFlow_CRM_API.Models.License { LicenseId = 1, Type = "B" };
        db.Licenses.Add(license);

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
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
            LicenseId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Semnalizare",
            PenaltyPoints = 5
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

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new SubmitSessionFormRequest(
            Mistakes: new List<MistakeItemDto>
            {
                new MistakeItemDto(IdItem: 1, Count: 5)  // 5 * 5 = 25 points
            },
            MaxPoints: 21
        );

        // Act
        var result = await controller.SubmitSessionForm(100, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<SubmitSessionFormResponse>().Subject;

        response.TotalPoints.Should().Be(25);
        response.MaxPoints.Should().Be(21);
        response.Result.Should().Be("FAILED");  // 25 > 21, so FAILED
    }

    [Fact]
    public async Task SubmitSessionForm_ShouldReturn400_WhenAppointmentIdInvalid()
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

        var request = new SubmitSessionFormRequest(
            Mistakes: new List<MistakeItemDto>(),
            MaxPoints: 21
        );

        // Act
        var result = await controller.SubmitSessionForm(0, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitSessionForm_ShouldReturn404_WhenAppointmentNotFound()
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

        var request = new SubmitSessionFormRequest(
            Mistakes: new List<MistakeItemDto>(),
            MaxPoints: 21
        );

        // Act
        var result = await controller.SubmitSessionForm(999, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SubmitSessionForm_ShouldReturn403_WhenInstructorNotOwner()
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

        var license = new DriveFlow_CRM_API.Models.License { LicenseId = 1, Type = "B" };
        db.Licenses.Add(license);

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
            Code = "B",
            AutoSchoolId = 1,
            SessionCost = 100,
            SessionDuration = 60,
            ScholarshipPrice = 1000,
            MinDrivingLessonsReq = 20
        };
        db.TeachingCategories.Add(category);

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

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new SubmitSessionFormRequest(
            Mistakes: new List<MistakeItemDto>(),
            MaxPoints: 21
        );

        // Act
        var result = await controller.SubmitSessionForm(100, request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SubmitSessionForm_ShouldReturn409_WhenFormAlreadyExists()
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

        var license = new DriveFlow_CRM_API.Models.License { LicenseId = 1, Type = "B" };
        db.Licenses.Add(license);

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
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
            LicenseId = 1,
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

        // Existing session form
        var existingForm = new SessionForm
        {
            SessionFormId = 1,
            AppointmentId = 100,
            FormId = 1,
            MistakesJson = "[]",
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(existingForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new SubmitSessionFormRequest(
            Mistakes: new List<MistakeItemDto>(),
            MaxPoints: 21
        );

        // Act
        var result = await controller.SubmitSessionForm(100, request);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task SubmitSessionForm_ShouldReturn400_WhenItemNotInExamForm()
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

        var license = new DriveFlow_CRM_API.Models.License { LicenseId = 1, Type = "B" };
        db.Licenses.Add(license);

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
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
            LicenseId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        // No exam items added!

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

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new SubmitSessionFormRequest(
            Mistakes: new List<MistakeItemDto>
            {
                new MistakeItemDto(IdItem: 999, Count: 1)  // Non-existent item
            },
            MaxPoints: 21
        );

        // Act
        var result = await controller.SubmitSessionForm(100, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitSessionForm_ShouldReturn201_WithEmptyMistakes()
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

        var license = new DriveFlow_CRM_API.Models.License { LicenseId = 1, Type = "B" };
        db.Licenses.Add(license);

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
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
            LicenseId = 1,
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

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        var request = new SubmitSessionFormRequest(
            Mistakes: new List<MistakeItemDto>(),  // No mistakes
            MaxPoints: 21
        );

        // Act
        var result = await controller.SubmitSessionForm(100, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<SubmitSessionFormResponse>().Subject;

        response.TotalPoints.Should().Be(0);
        response.Result.Should().Be("OK");  // 0 <= 21, so OK
    }

    // ────────────────────────────── GET SESSION FORM TESTS ──────────────────────────────

    [Fact]
    public async Task Get_ShouldReturn200_WithCorrectData_ForInstructor()
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
            FirstName = "Jane",
            LastName = "Smith",
            AutoSchoolId = 1
        };
        await userManager.CreateAsync(student, "Password123!");

        var license = new DriveFlow_CRM_API.Models.License { LicenseId = 1, Type = "B" };
        db.Licenses.Add(license);

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
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
            LicenseId = 1,
            MaxPoints = 21
        };
        db.ExamForms.Add(examForm);

        var examItem = new ExamItem
        {
            ItemId = 1,
            FormId = 1,
            Description = "Semnalizare",
            PenaltyPoints = 3
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
            MistakesJson = "[{\"id_item\":1,\"count\":2}]",
            CreatedAt = DateTime.UtcNow,
            TotalPoints = 6,
            Result = "OK"
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.Get(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<SessionFormViewDto>().Subject;

        dto.id.Should().Be(1);
        dto.totalPoints.Should().Be(6);
        dto.maxPoints.Should().Be(21);
        dto.result.Should().Be("OK");
        dto.studentName.Should().Be("Jane Smith");
        dto.instructorName.Should().Be("John Doe");
        dto.mistakes.Should().HaveCount(1);
        dto.mistakes.First().id_item.Should().Be(1);
        dto.mistakes.First().count.Should().Be(2);
    }

    [Fact]
    public async Task Get_ShouldReturn403_WhenInstructorNotOwner()
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

        var license = new DriveFlow_CRM_API.Models.License { LicenseId = 1, Type = "B" };
        db.Licenses.Add(license);

        var category = new TeachingCategory
        {
            TeachingCategoryId = 1,
            LicenseId = 1,
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
            LicenseId = 1,
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
            CreatedAt = DateTime.UtcNow
        };
        db.SessionForms.Add(sessionForm);

        await db.SaveChangesAsync();

        var controller = new SessionFormController(db, userManager);
        AttachIdentity(controller, "Instructor", "instructor1");

        // Act
        var result = await controller.Get(1);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Get_ShouldReturn404_WhenSessionFormNotFound()
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
        var result = await controller.Get(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
