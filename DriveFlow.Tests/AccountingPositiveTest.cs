using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using File = DriveFlow_CRM_API.Models.File;

namespace DriveFlow.Tests
{
    public class AccountingPositiveTest : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private ApplicationDbContext _context;

        public AccountingPositiveTest()
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

        [Fact]
        public async Task GetInvoice_StudentRole_ReturnsCorrectInvoice()
        {
            // Arrange
            SetupTestData("Student");
            
            // Setup mock HTTP handler for invoice service
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("PDF content")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            
            var controller = new AccountingController(
                _context,
                _mockUserManager.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object);

            // Setup controller context with claims
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
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("invoice_1.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetInvoice_SchoolAdminRole_ReturnsCorrectInvoice()
        {
            // Arrange
            SetupTestData("SchoolAdmin");
            
            // Setup mock HTTP handler for invoice service
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("PDF content")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            
            var controller = new AccountingController(
                _context,
                _mockUserManager.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object);

            // Setup controller context with claims
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
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("invoice_1.pdf", fileResult.FileDownloadName);
        }

        private void SetupTestData(string role)
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

            // Create test teaching category
            var category = new TeachingCategory
            {
                TeachingCategoryId = 1,
                Code = "B",
                SessionCost = 150,
                SessionDuration = 60,
                ScholarshipPrice = 2500,
                MinDrivingLessonsReq = 30,
                AutoSchoolId = 1
            };
            _context.TeachingCategories.Add(category);

            // Create test student
            var student = new ApplicationUser
            {
                Id = "student1",
                UserName = "student@test.com",
                Email = "student@test.com",
                FirstName = "Student",
                LastName = "Test",
                PhoneNumber = "987654321",
                Cnp = "1234567890123",
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

            // Create test vehicle
            var vehicle = new Vehicle
            {
                VehicleId = 1,
                LicensePlateNumber = "TEST123",
                Color = "Blue",
                TransmissionType = TransmissionType.Manual,
                AutoSchoolId = 1
            };
            _context.Vehicles.Add(vehicle);

            // Create test file
            var file = new File
            {
                FileId = 1,
                ScholarshipStartDate = DateTime.Now.AddMonths(-6),
                Status = FileStatus.Approved,
                TeachingCategoryId = 1,
                StudentId = "student1",
                InstructorId = "instructor1",
                VehicleId = 1
            };
            _context.Files.Add(file);

            // Create test payment
            var payment = new Payment
            {
                PaymentId = 1,
                FileId = 1,
                ScholarshipBasePayment = true,
                SessionsPayed = 30
            };
            _context.Payments.Add(payment);

            _context.SaveChanges();

            // Setup user manager mock
            _mockUserManager.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(role == "Student" ? "student1" : "admin1");

            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _context.Users.Find(id));

            _mockUserManager.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == "student1"), "Student"))
                .ReturnsAsync(role == "Student");

            _mockUserManager.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == "admin1"), "SchoolAdmin"))
                .ReturnsAsync(role == "SchoolAdmin");

            // Setup configuration mock - no URL configured in appsettings
            _mockConfiguration.Setup(x => x["InvoiceService:Url"])
                .Returns((string)null);
        }

        public void Dispose()
        {
            // Clean up environment variables
            Environment.SetEnvironmentVariable("INVOICE_SERVICE_URL", null);
            
            // Dispose any resources if needed
            _context.Dispose();
        }
    }
} 