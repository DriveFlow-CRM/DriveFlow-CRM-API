using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using DriveFlow_CRM_API.Models.DTOs; // Pentru a accesa tipurile din CommonDTOs
using DriveFlow_CRM_API.Controllers; // Pentru a accesa tipurile din StudentController

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/file")]
[Authorize(Roles = "SchoolAdmin,SuperAdmin")]
public class FileController : ControllerBase
{

    // ───────────────────────────── fields & actor ─────────────────────────────
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    /// <summary>Constructor invoked per request by DI.</summary>
    public FileController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }


    //[
    //  {
    //    "studentData": { /* all student fields except password */ },
    //    "files": [
    //      {
    //        "fileId": 501,
    //        "scholarshipStartDate": "2025-01-10",
    //        "criminalRecordExpiryDate": "2026-01-10",
    //        "medicalRecordExpiryDate": "2025-07-10",
    //        "status": "APPROVED",

    //        "teachingCategory": {
    //          "teachingCategoryId": 10,
    //          "sessionCost": 150,
    //          "sessionDuration": 60,
    //          "scholarshipPrice": 2500,
    //          "minDrivingLessonsReq": 30,
    //          "licenseType": "B"
    //        },
    //        "vehicle": {
    //          "vehicleId": 301,
    //          "licensePlateNumber": "CJ-456-ABC",
    //          "transmissionType": "manual",
    //          "color": "blue"
    //        },
    //        "instructor": {
    //          "instructorId": 41,
    //          "firstName": "Andrei",
    //          "lastName": "Popescu"
    //        },
    //        "payment": {
    //          "paymentId": 201,
    //          "sessionsPayed": 18,
    //          "scholarshipBasePayment": true
    //        }
    //      }
    //    ]
    //  }
    //]




    /// <summary>Retrieve all student file records for a specific auto school (SchoolAdmin only).</summary>
    /// <remarks>
    /// This endpoint returns a list of students and their associated file records, including details about payments, vehicles, instructors, and teaching categories.
    /// <para></para>
    ///```json 
    ///[
    ///  {
    ///    "studentData": { /* all student fields except password */ },
    ///    "files": [
    ///      {
    ///        "fileId": 501,
    ///        "scholarshipStartDate": "2025-01-10",
    ///       "criminalRecordExpiryDate": "2025-12-10",
    ///        "medicalRecordExpiryDate": "2025-07-10",
    ///        "status": "APPROVED",
    ///
    ///        "teachingCategory": {
    ///          "teachingCategoryId": 10,
    ///          "sessionCost": 150,
    ///          "sessionDuration": 60,
    ///          "scholarshipPrice": 2500,
    ///          "minDrivingLessonsReq": 30,
    ///          "licenseType": "B"
    ///        },
    ///        "vehicle": {
    ///          "vehicleId": 301,
    ///          "licensePlateNumber": "CJ-456-ABC",
    ///          "transmissionType": "MANUAL",
    ///          "color": "blue"
    ///        },
    ///        "instructor": {
    ///         "instructorId": 41,
    ///          "firstName": "Andrei",
    ///         "lastName": "Popescu"
    ///        },
    ///        "payment": {
    ///          "paymentId": 201,
    ///          "sessionsPayed": 18,
    ///          "scholarshipBasePayment": true
    ///        }
    ///      }
    ///    ]
    ///  }
    ///]
    /// </remarks>
    /// <param name="schoolId">The ID of the auto school whose student file records are to be retrieved.</param>
    /// <response code="200">Student file records retrieved successfully.</response>
    /// <response code="400">Auto school not found.</response>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is forbidden from accessing files of this auto school.</response>




    [HttpGet("fetchAll/{schoolId}")]
    [Authorize(Roles = "SchoolAdmin,SuperAdmin")]
    public async Task<IActionResult> GetStudentFileRecords(int schoolId)
    {
        var autoSchool = await _db.AutoSchools.FindAsync(schoolId);
        if (autoSchool == null)
            return BadRequest("Auto school not found.");


        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized("You are not properly logged in to fetch files.");

        // Check if the user is a SchoolAdmin
        bool isSchoolAdmin = await _users.IsInRoleAsync(user, "SchoolAdmin");
        bool isSuperAdmin = await _users.IsInRoleAsync(user, "SuperAdmin");

        if (isSchoolAdmin && user.AutoSchoolId != schoolId && !isSuperAdmin)
            return Forbid();



        var all_files = _db.Files
            .Include(f => f.Student)
            .Where(f => f.Student.AutoSchoolId == schoolId)
            .ToList();

        var students = _db.ApplicationUsers
            .Include(u => u.AutoSchool)
            .ThenInclude(a => a.Address)
            .ThenInclude(c => c.City)
            .Where(user => all_files.Select(file => file.StudentId).Contains(user.Id))
            .ToList();


        var studentFiles = new List<StudentFileRecordsDto>();

        foreach (var student in students)
        {
            List<StudentFileDataDto> files = new List<StudentFileDataDto>();
            foreach (var file in student.StudentFiles)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference
                var payment = _db.Payments.Where(p => p.FileId == file.FileId).FirstOrDefault();
                var vehicle = _db.Vehicles.Where(v => v.VehicleId == file.VehicleId).FirstOrDefault();
                var instructor = _db.ApplicationUsers.Where(i => i.Id == file.InstructorId).FirstOrDefault();
                var teachingCategory = _db.TeachingCategories
                    .Include(tc => tc.License)
                    .Where(tc => tc.TeachingCategoryId == file.TeachingCategoryId)
                    .FirstOrDefault();
#pragma warning restore CS8602

                if (payment == null)
                {
                    payment = new Payment()
                    {
                        PaymentId = 0,
                        ScholarshipBasePayment = false,
                        SessionsPayed = 0
                    };
                }

                if (vehicle == null)
                {
                    vehicle = new Vehicle()
                    {
                        VehicleId = 0,
                        LicensePlateNumber = "N/A",
                        TransmissionType = TransmissionType.MANUAL,
                        Color = "N/A"
                    };
                }

                if (instructor == null)
                {
                    instructor = new ApplicationUser()
                    {
                        Id = "N/A",
                        FirstName = "N/A",
                        LastName = "N/A"
                    };
                }
                if (teachingCategory == null)
                {
                    teachingCategory = new TeachingCategory()
                    {
                        TeachingCategoryId = 0,
                        SessionCost = 0,
                        SessionDuration = 0,
                        ScholarshipPrice = 0,
                        MinDrivingLessonsReq = 0,
                    };
                }

                TeachingCategoryDto teachingCategoryDto = new TeachingCategoryDto()
                {
                    TeachingCategoryId = teachingCategory.TeachingCategoryId,
                    SessionCost = teachingCategory.SessionCost,
                    SessionDuration = teachingCategory.SessionDuration,
                    ScholarshipPrice = teachingCategory.ScholarshipPrice,
                    MinDrivingLessonsReq = teachingCategory.MinDrivingLessonsReq,
#pragma warning disable CS8602 // Dereference of a possibly null reference
                    LicenseType = teachingCategory.License?.Type ?? teachingCategory.Code,
#pragma warning restore CS8602
                };


                StudentFileDataInstructorDto instructorDto = new StudentFileDataInstructorDto()
                {
                    InstructorId = instructor.Id,
                    FirstName = instructor.FirstName,
                    LastName = instructor.LastName
                };


                FileVehicleDto fileVehicleDto = new FileVehicleDto()
                {
                    VehicleId = vehicle.VehicleId,
                    LicensePlateNumber = vehicle.LicensePlateNumber,
                    TransmissionType = vehicle.TransmissionType.ToString(),
                    Color = (vehicle.Color == null ? "N/A" : vehicle.Color)
                };

                FilePaymentDto filePaymentDto = new FilePaymentDto()
                {
                    PaymentId = payment?.PaymentId,
                    ScholarshipBasePayment = payment?.ScholarshipBasePayment,
                    SessionsPayed = payment?.SessionsPayed
                };


                switch (file.Status)
                {
                    case FileStatus.APPROVED:
                        break;
                    case FileStatus.ARCHIVED:
                        break;
                    case FileStatus.EXPIRED:
                        break;
                    case FileStatus.FINALISED:
                        break;
                    default:
                        break;
                }

                files.Add(new StudentFileDataDto()
                {

                    fileId = file.FileId,
                    scholarshipStartDate = file.ScholarshipStartDate?.Date,
                    criminalRecordExpiryDate = file.CriminalRecordExpiryDate?.Date,
                    medicalRecordExpiryDate = file.MedicalRecordExpiryDate?.Date,
                    status = file.Status.ToString(),

                    teachingCategory = teachingCategoryDto,
                    vehicle = fileVehicleDto,
                    instructor = instructorDto,
                    payment = filePaymentDto,

                });
            }
            StudentDataDto studentData = new StudentDataDto()
            {
                StudentId = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                Address = student.AutoSchool?.Address != null ? 
                    $"{student.AutoSchool.Address.StreetName} {student.AutoSchool.Address.AddressNumber}, {student.AutoSchool.Address.City?.Name}" : null,
                Cnp = student.Cnp,
                AutoSchoolId = schoolId,
            };

            studentFiles.Add(new StudentFileRecordsDto()
            {
                StudentData = studentData,
                Files = files
            });
        }

        return Ok(studentFiles);
    }









    // ────────────────────────────── CREATE FILE ──────────────────────────────
    /// <summary>Create a new student file and set the payment method for someone
    /// wishing to start their courses (SchoolAdmin only).</summary>
    /// <remarks> SchoolAdmin's AutoSchoolId must match the student's
    /// AutoSchoolId given as method paramether.
    /// <para> <strong> Sample Request body </strong> </para> 
    /// The JSON structure for the request body is as follows:
    /// ```json
    /// 
     ///     {
     ///      "scholarshipStartDate": "2025-01-10",
     ///      "criminalRecordExpiryDate": "2025-12-10",
     ///      "medicalRecordExpiryDate": "2025-07-10",
     ///      "status": "APPROVED",  
     ///      "teachingCategoryId": 1,
     ///      "vehicleId": 1,       
     ///      "instructorId": "419decbe-6af1-4d84-9b45-c1ef796f4603",      
     ///      "payment": {
     ///           "sessionsPayed": 2,
     ///           "scholarshipBasePayment": true
     ///          }
     ///      }
    ///
    ///   "vehicleId": 301,      // optional
    ///   "instructorId": 41,        // optional
    /// 
    ///
    /// Response if 200-OK
    /// 
    /// {
    ///  "fileId":    560,
    ///  "paymentId": 320,
    ///  "message":   "File created successfully"
    /// }
    /// 
    /// 
    /// 
    /// 
    /// </remarks>
    /// <response code = "200">File and Payment method created with success</response>
    /// <response code = "400">Invalid user ID</response>
    /// <response code = "401">No valid JWT supplied</response>
    /// <response code = "403">User is forbidden from seeing the files of this auto school</response>
    [HttpPost("createFile/{studentId}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> CreateFile(string studentId,[FromBody] CreateFileDto fileDto )
    {

        var userStudent = _db.ApplicationUsers.Find(studentId);
        if (userStudent == null)
            return BadRequest("Student not found.");

        var userAdmin = await _users.GetUserAsync(User);

        if (userAdmin == null || userAdmin.AutoSchoolId != userStudent.AutoSchoolId)
        {
            return Forbid();
        }


        var instructor = await _db.ApplicationUsers.FindAsync(fileDto.instructorId);
        if (instructor == null)
            return BadRequest("Instructor not found or not assigned to a valid instructor.");

        var vehicle = await _db.Vehicles.FindAsync(fileDto.vehicleId);
        if (vehicle == null)
            return BadRequest("Vehicle not found.");

        // Include the License relationship when retrieving TeachingCategory
        var teachingCategory = await _db.TeachingCategories
            .Include(tc => tc.License)
            .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == fileDto.teachingCategoryId);
            
        if (teachingCategory == null)
            return BadRequest("Teaching category not found.");

        if (teachingCategory.AutoSchoolId != userAdmin.AutoSchoolId)
            return BadRequest("This school does not have this teaching category");

        /*
         * In json un enumerate se trimite prin id-ul lui (0,1,2,3..) 
         * Daca Gabi trimite un string, il interpretez apoi ca enumerate
         * pentru modelul de File
         */
        FileStatus localStatus;

        switch (fileDto.status)
        {
            case "APPROVED":
                localStatus = FileStatus.APPROVED;
                break;
            case "ARCHIVED":
                localStatus = FileStatus.ARCHIVED;
                break;
            case "EXPIRED":
                localStatus = FileStatus.EXPIRED;
                break;
            case "FINALISED":
                localStatus = FileStatus.FINALISED;
                break;
            default:
                return BadRequest("Invalid file status.");
        }


        var file = new Models.File
        {
            ScholarshipStartDate = fileDto.scholarshipStartDate.Date,
            CriminalRecordExpiryDate = fileDto.criminalRecordExpiryDate.Date,
            MedicalRecordExpiryDate = fileDto.medicalRecordExpiryDate.Date,
            //FileStatus enum values: APPROVED, ARCHIVED, EXPIRED, FINALISED
            Status = localStatus,
            TeachingCategoryId = fileDto.teachingCategoryId,
            StudentId = studentId,
            //VehicleId && InstructorId are optional therefore ternary operator should
            //preferably be used here i believe
            VehicleId = fileDto.vehicleId,
            InstructorId = fileDto.instructorId
        };




         _db.Files.Add(file);
        await _db.SaveChangesAsync();
      //  return Ok(file.FileId);
        //if(fileDto.payment == null)
        //{
        //    return BadRequest("Payment method not found.");
        //}

        var payment = new Payment
        {
            ScholarshipBasePayment = fileDto.payment.ScholarshipBasePayment,
            SessionsPayed = fileDto.payment.SessionsPayed,
            FileId = file.FileId
        };

        await _db.Payments.AddAsync(payment);
        await _db.SaveChangesAsync();


        return Ok(new CreateFileResponseDto()
        {
            FileId = file.FileId,
            PaymentId = payment.PaymentId,
            Message = "File created successfully"
        });
    }

    // ────────────────────────────── UPDATE FILE ──────────────────────────────


    /// <summary>Edit an existing student file (SchoolAdmin only).</summary>
    /// <remarks>
    ///     FileDto must contain all updatable fields except the fileId.
    ///     
    /// ``` Json example
    /// {
    ///  "scholarshipStartDate": "2025-05-10",
    ///  "criminalRecordExpiryDate": "2025-10-10",
    ///  "medicalRecordExpiryDate": "2025-12-10",
    ///  "status": "APPROVED",
    ///  "instructorId": "419decbe-6af1-4d84-9b45-c1ef796f4603",
    ///  "vehicleId": 1,
    ///  "teachingCategoryId": 1
    ///}
    /// 
    /// Returns 
    /// 
    /// {
    ///    "message": "File updated successfully"
    /// {
    /// 
    /// 
    /// 
    /// </remarks>
    /// SchoolAdmin's AutoSchoolId must match the student's AutoSchoolId associated with the file.
    ///  <param name="fileId">The id of the file to be updated</param>
    ///  <param name="fileDto">The body of the updated object</param>
    ///  
    /// <response code="200">File updated successfully</response>
    /// <response code="400">Invalid file ID or file not found</response>
    /// <response code="401">No valid JWT supplied</response>
    /// <response code="403">User is forbidden from editing files of this auto school</response>

    [HttpPut("editFile/{fileId}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> EditFile(int fileId, [FromBody] EditFileDto fileDto)
    {
        var userAdmin = await _users.GetUserAsync(User);
        if (userAdmin == null)
            return Unauthorized("You are not properly logged in to edit files.");

        // Get the file with its related student
        var file = await _db.Files
            .Include(f => f.Student)
            .FirstOrDefaultAsync(f => f.FileId == fileId);

        if (file == null)
            return NotFound("File not found.");

        if (file.Student.AutoSchoolId != userAdmin.AutoSchoolId)
            return Forbid("You can only edit files from your own auto school.");

        // Get the teaching category if one was provided
        TeachingCategory? teachingCategory = null;
        if (fileDto.TeachingCategoryId.HasValue)
        {
            teachingCategory = await _db.TeachingCategories
                .Include(tc => tc.License)
                .FirstOrDefaultAsync(tc => tc.TeachingCategoryId == fileDto.TeachingCategoryId);
                
            if (teachingCategory == null)
                return BadRequest("Teaching category not found.");

#pragma warning disable CS8602 // Dereference of a possibly null reference
            if (teachingCategory.AutoSchoolId != userAdmin.AutoSchoolId)
                return BadRequest("Teaching category does not belong to the same auto school.");
#pragma warning restore CS8602
        }

        if (fileDto.InstructorId != null)
        {
            var instructor = await _db.ApplicationUsers.FindAsync(fileDto.InstructorId);

            if(instructor == null)
            {
                return BadRequest("Instructor does not exist");
            }

            if (instructor.AutoSchoolId != userAdmin.AutoSchoolId)
                return BadRequest("Instructor does not belong to the same auto school.");
        }
        if (fileDto.VehicleId != null)
        {
            var vehicle = await _db.Vehicles.FindAsync(fileDto.VehicleId);
            if (vehicle == null)
                return BadRequest("Vehicle not found.");
            if (vehicle.AutoSchoolId != userAdmin.AutoSchoolId)
                return BadRequest("Vehicle does not belong to the same auto school.");
        }
        if (fileDto.ScholarshipStartDate.HasValue)
            file.ScholarshipStartDate = fileDto.ScholarshipStartDate.Value.Date;
            
        if (fileDto.CriminalRecordExpiryDate.HasValue)
            file.CriminalRecordExpiryDate = fileDto.CriminalRecordExpiryDate.Value.Date;
            
        if (fileDto.MedicalRecordExpiryDate.HasValue)
            file.MedicalRecordExpiryDate = fileDto.MedicalRecordExpiryDate.Value.Date;

        file.TeachingCategoryId = fileDto.TeachingCategoryId;
        file.InstructorId = fileDto.InstructorId;
        file.VehicleId = fileDto.VehicleId;

        if (fileDto.Status != null)
        {
            switch (fileDto.Status)
            {
                case "APPROVED":
                    file.Status = FileStatus.APPROVED;
                    break;
                case "ARCHIVED":
                    file.Status = FileStatus.ARCHIVED;
                    break;
                case "EXPIRED":
                    file.Status = FileStatus.EXPIRED;
                    break;
                case "FINALISED":
                    file.Status = FileStatus.FINALISED;
                    break;
                default:
                    return BadRequest($"Invalid file status. Allowed values: {string.Join(", ", Enum.GetNames(typeof(FileStatus)))}.");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new
        {
            message = "File updated successfully"
        });
    }

    // ────────────────────────────── UPDATE FILE PAYMENT ──────────────────────────────

    /// <summary>Edit an existing payment record (SchoolAdmin only).</summary>
    /// <remarks>
    /// SchoolAdmin's AutoSchoolId must match the student's AutoSchoolId associated with the file linked to the payment.
    /// <para><strong>Sample Request Body</strong></para>
    /// The JSON structure for the request body is as follows:
    /// ```
    /// {
    ///  "scholarshipBasePayment": true,
    ///  "sessionsPayed": 5
    /// }
    /// </remarks>
    /// 
    /// <param name="paymentId"> The id of the payment to be updated</param>
    /// <param name="paymentDto"> The body of the updated object</param>
    /// <response code="200">Payment updated successfully</response>
    /// <response code="400">Invalid payment ID or payment not found</response>
    /// <response code="401">No valid JWT supplied</response>
    /// <response code="403">User is forbidden from editing files and payments of this auto school</response>

    [HttpPut("editPayment/{paymentId}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> EditPayment(int paymentId,[FromBody] PaymentDto paymentDto)
    {

        var payment = await _db.Payments
            .Include(p => p.File)
            .ThenInclude(f => f.Student)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        if (payment == null)
        {
            return BadRequest("Payment not found to edit");
        }



        var schoolAdmin = await _users.GetUserAsync(User);

        int? fileSchoolId = payment.File.Student.AutoSchoolId;
        

        if (fileSchoolId == null)
        {
            return BadRequest("File does not belong to any auto school");
        }

        if (schoolAdmin?.AutoSchoolId != fileSchoolId)
        {
            return Forbid();
        }
        // all ok here
        // 

        payment.ScholarshipBasePayment = paymentDto.ScholarshipBasePayment;
        payment.SessionsPayed = paymentDto.SessionsPayed;

        await _db.SaveChangesAsync();
        return Ok(new
        {
            message = "Payment updated successfully"
        });

    }
    // ────────────────────────────── DELETE FILE ──────────────────────────────


    /// <summary>Delete an existing student file (SchoolAdmin only).</summary>
    /// <remarks>
    /// SchoolAdmin's AutoSchoolId must match the student's AutoSchoolId associated with the file.
    /// </remarks>
    /// <param name="fileId">The ID of the file to be deleted.</param>
    /// <response code="200">File deleted successfully</response>
    /// <response code="400">You cannot delete files of other auto schools</response>
    /// <response code="401">No valid JWT supplied</response>
    /// <response code="403">User is forbidden from deleting files of this auto school</response>
    /// <response code="404">File not found</response>


    [HttpDelete("delete/{fileId}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> DeleteFile(int fileId)
    {

        var file = await _db.Files
            .Include(f=>f.Student)
            .FirstOrDefaultAsync(f=>f.FileId==fileId);
        if (file == null)
            return NotFound(new { message = "File not found." });

        var user = await  _users.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("You don't have the rights to do this");
        }

        if(file.Student.AutoSchoolId != user.AutoSchoolId)
        {
            return Forbid();
        }

        var paymemts = _db.Payments.Where(p => p.FileId == file.FileId);
        _db.Files.Remove(file);
        await _db.SaveChangesAsync();

        _db.Payments.RemoveRange(paymemts);
        await _db.SaveChangesAsync();


        return Ok(new
        {
            message = "File deleted successfully"
        });
    }

    [HttpGet("details/{fileId}")]
    [Authorize(Roles = "SchoolAdmin,Instructor,Student")]
    public async Task<IActionResult> GetFileDetails(int fileId)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Unauthorized("You are not properly logged in to view file details.");

        var file = await _db.Files
            .Include(f => f.Student)
            .Include(f => f.TeachingCategory)
                .ThenInclude(tc => tc.License)
            .Include(f => f.Vehicle)
                .ThenInclude(v => v.License)
            .Include(f => f.Instructor)
            .Include(f => f.Appointments)
            .FirstOrDefaultAsync(f => f.FileId == fileId);

        if (file == null)
            return NotFound("File not found.");

        // Check authorization based on role
        bool isSchoolAdmin = await _users.IsInRoleAsync(user, "SchoolAdmin");
        bool isInstructor = await _users.IsInRoleAsync(user, "Instructor");
        bool isStudent = await _users.IsInRoleAsync(user, "Student");
        bool isSuperAdmin = await _users.IsInRoleAsync(user, "SuperAdmin");

        if (isSchoolAdmin && user.AutoSchoolId != file.Student.AutoSchoolId && !isSuperAdmin)
            return Forbid();
        if (isInstructor && file.InstructorId != user.Id)
            return Forbid();
        if (isStudent && file.StudentId != user.Id)
            return Forbid();

        // Get payment for the file
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.FileId == fileId);

        // Process appointments for counting completed ones
        var now = DateTime.Now;
#pragma warning disable CS8602 // Dereference of a possibly null reference
        var appointments = file.Appointments
            .Select(a => new AppointmentDetailsDto
            {
                AppointmentId = a.AppointmentId,
                Date = a.Date,
                StartHour = a.StartHour.ToString(@"hh\:mm"),
                EndHour = a.EndHour.ToString(@"hh\:mm"),
                Status = a.Date.Add(a.EndHour) < now ? "completed" : "pending"
            })
            .ToList();
#pragma warning restore CS8602

        int completedCount = appointments.Count(a => a.Status == "completed");

        // Create and return the FileDetailsDto
        var fileDetailsDto = new FileDetailsDto
        {
            FileId = file.FileId,
            ScholarshipStartDate = file.ScholarshipStartDate?.Date,
            CriminalRecordExpiryDate = file.CriminalRecordExpiryDate?.Date,
            MedicalRecordExpiryDate = file.MedicalRecordExpiryDate?.Date,
            Status = file.Status.ToString(),
            Payment = payment != null
                ? new PaymentDetailsDto
                {
                    ScholarshipPayment = payment.ScholarshipBasePayment,
                    SessionsPayed = payment.SessionsPayed
                }
                : null,
            Instructor = file.Instructor != null
                ? new InstructorDetailsDto
                {
                    UserId = file.Instructor.Id,
                    FirstName = file.Instructor.FirstName,
                    LastName = file.Instructor.LastName,
                    Email = file.Instructor.Email,
                    Phone = file.Instructor.PhoneNumber,
                    Role = "Instructor"
                }
                : null,
            Vehicle = file.Vehicle != null
                ? new VehicleDetailsDto
                {
                    LicensePlateNumber = file.Vehicle.LicensePlateNumber,
                    TransmissionType = file.Vehicle.TransmissionType.ToString(),
                    Color = file.Vehicle.Color,
                    Brand = file.Vehicle.Brand,
                    Model = file.Vehicle.Model,
                    YearOfProduction = file.Vehicle.YearOfProduction,
                    FuelType = file.Vehicle.FuelType.HasValue ? file.Vehicle.FuelType.ToString() : null,
                    EngineSizeLiters = file.Vehicle.EngineSizeLiters,
                    PowertrainType = file.Vehicle.PowertrainType.HasValue ? file.Vehicle.PowertrainType.ToString() : null,
#pragma warning disable CS8602 // Dereference of a possibly null reference
                    Type = file.TeachingCategory?.License?.Type ?? file.TeachingCategory?.Code
#pragma warning restore CS8602
                }
                : null,
            Appointments = appointments,
            AppointmentsCompleted = completedCount
        };

        return Ok(fileDetailsDto);
    }
}

/// Used for EditFile !!!!!
///<see cref="FileDto"/>

//public sealed class CreateFilePaymentDto
//{
//    public int sessionPayed = 0;
//    public bool scholarshipBasePayment = false;
//}
public sealed class CreateFileDto
{
    public DateTime scholarshipStartDate { get; init; } = default!;
    public DateTime criminalRecordExpiryDate { get; init; } = default!;

    public DateTime medicalRecordExpiryDate { get; init; } = default!;

    public string status { get; init; } = default!;

    public int teachingCategoryId { get; init; } = default!;

    public int? vehicleId { get; init; } = default!;

    public string? instructorId { get; init; } = default!;

    public PaymentDto payment { get; init; } = default!;


}


public sealed class CreateFileResponseDto
{
    public int FileId { get; init; } = default!;
    public int PaymentId { get; init; } = default!;
    public string Message { get; init; } = default!;
}

// Gabi mai imi spui daca mai ai nevoie si de alte campuri.
// Pe trello scrie 'toate campurile mai putin parola'
public sealed class StudentDataDto
{

    public string StudentId { get; init; } = default!;
    public string? FirstName { get; init; } = default!;

    public string? LastName { get; init; } = default!;

    public string? Email { get; init; } = default!;

    public string? PhoneNumber { get; init; } = default!;

    public string? Address { get; init; } = default!;

    public string? Cnp { get; init; } = default!;

    public int? AutoSchoolId { get; init; } = default!;
}
public sealed class StudentFileDataInstructorDto
{

    public string? InstructorId { get; init; } = default!;

    public string? FirstName { get; init; } = default!;

    public string? LastName { get; init; } = default!;

}
public sealed class FilePaymentDto
{
    public int? PaymentId  { get; init; } = default!;
    public bool? ScholarshipBasePayment { get; init; } = default!;
    public int? SessionsPayed { get; init; } = default!;
}

public sealed class FileVehicleDto
{
    public int VehicleId { get; init; } = default!;
    public string LicensePlateNumber { get; init; } = default!;
    public string TransmissionType { get; init; } = default!;
    public string Color { get; init; } = "N/A";
}


public sealed class EditFileDto
{
    public DateTime? ScholarshipStartDate { get; init; } = default!;
    public DateTime? CriminalRecordExpiryDate { get; init; } = default!;
    public DateTime? MedicalRecordExpiryDate { get; init; } = default!;

    public string Status { get; set; } = "APPROVED";

    public int? VehicleId { get; set; } = default!;

    public string? InstructorId { get; init; } = default!;

    public int? TeachingCategoryId { get; init; } = default!;
}




public sealed class StudentFileDataDto
{
    public int fileId { get; init; } = default!;
    public DateTime? scholarshipStartDate { get; init; } = default!;
    public DateTime? criminalRecordExpiryDate { get; init; } = default!;
    public DateTime? medicalRecordExpiryDate { get; init; } = default!;
    public string status { get; init; } = default!;
    public TeachingCategoryDto teachingCategory { get; init; } = default!;
    public FileVehicleDto vehicle { get; init; } = default!;
    public StudentFileDataInstructorDto instructor { get; init; } = default!;
    public FilePaymentDto payment { get; init; } = default!;
}


public sealed class StudentFileRecordsDto
{
    public StudentDataDto StudentData { get; init; } = default!;
    public List<StudentFileDataDto> Files { get; init; } = default!;
}


