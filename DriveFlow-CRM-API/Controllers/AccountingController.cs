using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace DriveFlow_CRM_API.Controllers
{
    [Route("api/accounting")]
    [ApiController]
    [Authorize]
    public class AccountingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AccountingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Generates a PDF invoice for a completed driving-school scholarship
        /// </summary>
        /// <param name="fileId">The ID of the file for which to generate an invoice</param>
        /// <returns>A PDF invoice if the file is eligible</returns>
        /// <response code="200">Returns the PDF invoice</response>
        /// <response code="400">If the file is not eligible for an invoice</response>
        /// <response code="403">If the user is not authorized to access this file</response>
        /// <response code="404">If the file is not found</response>
        [HttpGet("file/{fileId}/invoice")]
        [Authorize(Roles = "Student,SchoolAdmin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetInvoice(int fileId)
        {
            // Get the current user
            var userId = _userManager.GetUserId(User);
            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
            {
                return Forbid();
            }

            // Find the requested file with all related data
            var file = await _context.Files
                .Include(f => f.Student)
                .Include(f => f.Instructor)
                .Include(f => f.Vehicle)
                .Include(f => f.TeachingCategory)
                .Include(f => f.Student.AutoSchool)
                .FirstOrDefaultAsync(f => f.FileId == fileId);

            if (file == null)
            {
                return NotFound();
            }

            // Check ownership based on role
            var isStudent = await _userManager.IsInRoleAsync(currentUser, "Student");
            var isSchoolAdmin = await _userManager.IsInRoleAsync(currentUser, "SchoolAdmin");

            if (isStudent)
            {
                // Students can only access their own files
                if (file.StudentId != userId)
                {
                    return Forbid();
                }
            }
            else if (isSchoolAdmin)
            {
                // School admins can only access files from their school
                if (currentUser.AutoSchoolId != file.Student.AutoSchoolId)
                {
                    return Forbid();
                }
            }
            else
            {
                // Other roles are not allowed
                return Forbid();
            }

            // Get payment data for eligibility check
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.FileId == fileId);

            if (payment == null)
            {
                return BadRequest("Payment information not found for this file.");
            }

            // Check if the file is eligible for an invoice (tuition fully paid)
            var teachingCategory = file.TeachingCategory;
            if (teachingCategory == null)
            {
                return BadRequest("Teaching category information not found for this file.");
            }

            if (payment.SessionsPayed < teachingCategory.MinDrivingLessonsReq || !payment.ScholarshipBasePayment)
            {
                return BadRequest("Invoice unavailable: tuition not fully paid.");
            }

            // Assemble the payload for the invoice generation service
            var invoicePayload = new
            {
                autoSchool = new
                {
                    name = file.Student.AutoSchool?.Name,
                    website = file.Student.AutoSchool?.WebSite,
                    phone = file.Student.AutoSchool?.PhoneNumber,
                    email = file.Student.AutoSchool?.Email
                },
                student = new
                {
                    firstName = file.Student.FirstName,
                    lastName = file.Student.LastName,
                    email = file.Student.Email,
                    phone = file.Student.PhoneNumber ?? "Not provided",
                    cnp = file.Student.Cnp
                },
                file = new
                {
                    scholarshipStartDate = file.ScholarshipStartDate?.ToString("yyyy-MM-dd"),
                    criminalRecordExpiryDate = file.CriminalRecordExpiryDate?.ToString("yyyy-MM-dd"),
                    medicalRecordExpiryDate = file.MedicalRecordExpiryDate?.ToString("yyyy-MM-dd"),
                    status = file.Status.ToString()
                },
                teachingCategory = new
                {
                    type = teachingCategory.Code,
                    sessionCost = teachingCategory.SessionCost,
                    sessionDuration = teachingCategory.SessionDuration,
                    scholarshipPrice = teachingCategory.ScholarshipPrice,
                    minDrivingLessonsReq = teachingCategory.MinDrivingLessonsReq
                },
                vehicle = file.Vehicle == null ? null : new
                {
                    licensePlateNumber = file.Vehicle.LicensePlateNumber,
                    transmissionType = file.Vehicle.TransmissionType.ToString(),
                    color = file.Vehicle.Color,
                    licenseType = file.Vehicle.License?.Type ?? "Not specified"
                },
                instructor = file.Instructor == null ? null : new
                {
                    fullName = $"{file.Instructor.FirstName} {file.Instructor.LastName}"
                },
                payment = new
                {
                    sessionsPayed = payment.SessionsPayed,
                    scholarshipBasePayment = payment.ScholarshipBasePayment
                }
            };

            try
            {
                // Call the invoice generation microservice
                var invoiceServiceUrl = Environment.GetEnvironmentVariable("INVOICE_SERVICE_URL") ?? 
                                       _configuration["InvoiceService:Url"];
                
                if (string.IsNullOrEmpty(invoiceServiceUrl))
                {
                    return StatusCode(500, "Invoice service URL not configured. Please set the INVOICE_SERVICE_URL environment variable.");
                }
                
                var client = _httpClientFactory.CreateClient();
                
                var response = await client.PostAsJsonAsync(invoiceServiceUrl, invoicePayload);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, 
                        $"Invoice generation service error: {errorContent}");
                }

                // Get the PDF stream from the response
                var pdfStream = await response.Content.ReadAsStreamAsync();
                
                // Return the PDF stream to the client
                return File(pdfStream, "application/pdf", $"invoice_{fileId}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating invoice: {ex.Message}");
            }
        }
    }
} 