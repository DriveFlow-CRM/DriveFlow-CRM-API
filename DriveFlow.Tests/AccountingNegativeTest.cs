using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using File = DriveFlow_CRM_API.Models.File;

namespace DriveFlow.Tests
{
    public class AccountingNegativeTest : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private ApplicationDbContext _context;

        public AccountingNegativeTest()
        {
            // Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Setup HttpClientFactory mock
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            // Setup Configuration mock
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDatabase_{Guid.NewGuid()}")
                .Options;
            
            _context = new ApplicationDbContext(options);
            
            // Set environment variable for testing with mock URL
            Environment.SetEnvironmentVariable("INVOICE_SERVICE_URL", "https://test-invoice-service/api/pdf");
        }

        public void Dispose()
        {
            // Clean up environment variables
            Environment.SetEnvironmentVariable("INVOICE_SERVICE_URL", null);
            
            // Dispose any resources if needed
            _context.Dispose();
        }

        [Fact]
        public async Task GetInvoice_FileNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupBasicUserData();
            
            var controller = new AccountingController(
                _context,
                _mockUserManager.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "student1"),
                        new Claim(ClaimTypes.Role, "Student")
                    }))
                }
            };

            // Act
            var result = await controller.GetInvoice(999); // Non-existent file ID

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetInvoice_StudentNotOwner_ReturnsForbid()
        {
            // Arrange
            SetupFileWithDifferentOwner();
            
            var controller = new AccountingController(
                _context,
                _mockUserManager.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "student1"),
                        new Claim(ClaimTypes.Role, "Student")
                    }))
                }
            };

            // Act
            var result = await controller.GetInvoice(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetInvoice_SchoolAdminDifferentSchool_ReturnsForbid()
        {
            // Arrange
            SetupFileFromDifferentSchool();
            
            var controller = new AccountingController(
                _context,
                _mockUserManager.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "admin1"),
                        new Claim(ClaimTypes.Role, "SchoolAdmin")
                    }))
                }
            };

            // Act
            var result = await controller.GetInvoice(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetInvoice_UnauthorizedRole_ReturnsForbid()
        {
            // Arrange
            SetupBasicUserData();
            
            var controller = new AccountingController(
                _context,
                _mockUserManager.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "instructor1"),
                        new Claim(ClaimTypes.Role, "Instructor")
                    }))
                }
            };

            // Act
            var result = await controller.GetInvoice(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetInvoice_NotEligible_ReturnsBadRequest()
        {
            // Arrange
            SetupIneligibleFile();
            
            var controller = new AccountingController(
                _context,
                _mockUserManager.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "student1"),
                        new Claim(ClaimTypes.Role, "Student")
                    }))
                }
            };

            // Act
            var result = await controller.GetInvoice(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invoice unavailable: tuition not fully paid.", badRequestResult.Value);
        }

        private void SetupBasicUserData()
        {
            // Create test school
            var school = new AutoSchool
            {
                AutoSchoolId = 1,
                Name = "Test School",
                WebSite = "http://test.com",
                PhoneNumber = "123456789",
                Email = "school@test.com"
            };
            _context.AutoSchools.Add(school);

            // Create test student
            var student = new ApplicationUser
            {
                Id = "student1",
                UserName = "student@test.com",
                Email = "student@test.com",
                FirstName = "Student",
                LastName = "Test",
                AutoSchoolId = 1
            };
            _context.Users.Add(student);

            // Create test admin
            var admin = new ApplicationUser
            {
                Id = "admin1",
                UserName = "admin@test.com",
                Email = "admin@test.com",
                FirstName = "Admin",
                LastName = "Test",
                AutoSchoolId = 1
            };
            _context.Users.Add(admin);

            // Create test instructor
            var instructor = new ApplicationUser
            {
                Id = "instructor1",
                UserName = "instructor@test.com",
                Email = "instructor@test.com",
                FirstName = "Instructor",
                LastName = "Test",
                AutoSchoolId = 1
            };
            _context.Users.Add(instructor);

            _context.SaveChanges();

            // Setup user manager mock
            _mockUserManager.Setup(x => x.GetUserId(It.Is<ClaimsPrincipal>(
                p => p.FindFirst(ClaimTypes.NameIdentifier)?.Value == "student1")))
                .Returns("student1");
            
            _mockUserManager.Setup(x => x.GetUserId(It.Is<ClaimsPrincipal>(
                p => p.FindFirst(ClaimTypes.NameIdentifier)?.Value == "admin1")))
                .Returns("admin1");
            
            _mockUserManager.Setup(x => x.GetUserId(It.Is<ClaimsPrincipal>(
                p => p.FindFirst(ClaimTypes.NameIdentifier)?.Value == "instructor1")))
                .Returns("instructor1");

            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _context.Users.Find(id));

            _mockUserManager.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == "student1"), "Student"))
                .ReturnsAsync(true);

            _mockUserManager.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == "admin1"), "SchoolAdmin"))
                .ReturnsAsync(true);
                
            _mockUserManager.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == "instructor1"), "Instructor"))
                .ReturnsAsync(true);
        }

        private void SetupFileWithDifferentOwner()
        {
            SetupBasicUserData();

            // Create test teaching category
            var category = new TeachingCategory
            {
                TeachingCategoryId = 1,
                Code = "B",
                MinDrivingLessonsReq = 30,
                AutoSchoolId = 1
            };
            _context.TeachingCategories.Add(category);

            // Create test file with different owner
            var file = new File
            {
                FileId = 1,
                ScholarshipStartDate = DateTime.Now.AddMonths(-6),
                Status = FileStatus.Approved,
                TeachingCategoryId = 1,
                StudentId = "instructor1", // Different owner than the student1 who will try to access
                VehicleId = null
            };
            _context.Files.Add(file);

            _context.SaveChanges();
        }

        private void SetupFileFromDifferentSchool()
        {
            SetupBasicUserData();

            // Create second school
            var school2 = new AutoSchool
            {
                AutoSchoolId = 2,
                Name = "Another School",
                Email = "another@test.com"
            };
            _context.AutoSchools.Add(school2);

            // Create student from different school
            var student2 = new ApplicationUser
            {
                Id = "student2",
                UserName = "student2@test.com",
                Email = "student2@test.com",
                FirstName = "Student2",
                LastName = "Test",
                AutoSchoolId = 2
            };
            _context.Users.Add(student2);

            // Create test teaching category
            var category = new TeachingCategory
            {
                TeachingCategoryId = 1,
                Code = "B",
                MinDrivingLessonsReq = 30,
                AutoSchoolId = 2
            };
            _context.TeachingCategories.Add(category);

            // Create test file for the different school
            var file = new File
            {
                FileId = 1,
                ScholarshipStartDate = DateTime.Now.AddMonths(-6),
                Status = FileStatus.Approved,
                TeachingCategoryId = 1,
                StudentId = "student2",
                VehicleId = null
            };
            _context.Files.Add(file);

            _context.SaveChanges();
        }

        private void SetupIneligibleFile()
        {
            SetupBasicUserData();

            // Create test teaching category
            var category = new TeachingCategory
            {
                TeachingCategoryId = 1,
                Code = "B",
                MinDrivingLessonsReq = 30,
                AutoSchoolId = 1
            };
            _context.TeachingCategories.Add(category);

            // Create test file
            var file = new File
            {
                FileId = 1,
                ScholarshipStartDate = DateTime.Now.AddMonths(-6),
                Status = FileStatus.Approved,
                TeachingCategoryId = 1,
                StudentId = "student1",
                VehicleId = null
            };
            _context.Files.Add(file);

            // Create test payment with insufficient lessons
            var payment = new Payment
            {
                PaymentId = 1,
                FileId = 1,
                ScholarshipBasePayment = true,
                SessionsPayed = 15  // Less than required 30
            };
            _context.Payments.Add(payment);

            _context.SaveChanges();
        }
    }
} 