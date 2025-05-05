using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/file")]
[Authorize(Roles = "SchoolAdmin")]
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
    //        "status": "inProgress",

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






    [HttpGet("getStudentFileRecords/{schoolId}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> GetStudentFileRecords(int schoolId)
    {
        var autoSchool = _db.AutoSchools.Find(schoolId);
        if (autoSchool == null)
            return BadRequest("Auto school not found.");
        if (_users.GetUserAsync(User).Result?.AutoSchoolId != schoolId)
            return Forbid("You are not allowed to see the files of this auto school.");


        List<StudentFileRecordsDto> studentFiles = new List<StudentFileRecordsDto>();
        var students = await _db.ApplicationUsers
            .Where(u => u.AutoSchoolId == schoolId && u.StudentFiles.Count > 0)
            .ToListAsync();

        foreach (var student in students)
        {
            List<FilesDataDto> files = new List<FilesDataDto>();
            foreach (var file in student.StudentFiles)
            {
                files.Add(new FilesDataDto()
                {

                    FileId = file.FileId,
                    ScholarshipStartDate = file.ScholarshipStartDate,
                    CriminalRecordExpiryDate = file.CriminalRecordExpiryDate,
                    MedicalRecordExpiryDate = file.MedicalRecordExpiryDate,
                    Status = file.Status,
                    Vehicle = new VehicleDto()
                    {
                        VehicleId = file.VehicleId,
                        LicenseId = file.Vehicle?.LicensePlateNumber,
                        TransmissionType = file.Vehicle?.TransmissionType,
                        Color = file.Vehicle?.Color
                    },
                    TeachingCategory = new TeachingCategoryDto()
                    {
                        TeachingCategoryId = file.TeachingCategoryId,
                        SessionCost = file.TeachingCategory?.SessionCost,
                        SessionDuration = file.TeachingCategory?.SessionDuration,
                        ScholarshipPrice = file.TeachingCategory?.ScholarshipPrice,
                        MinDrivingLessonsReq = file.TeachingCategory?.MinDrivingLessonsReq,
                        LicenseType = file.TeachingCategory?.LicenseType
                    },
                    Instructor = new StudentFileDataInstructorDto()
                    {
                        InstructorId = file.InstructorId,
                        FirstName = file.Instructor?.FirstName,
                        LastName = file.Instructor?.LastName
                    },
                    Payment = new FilePaymentDto()
                    {
                        PaymentId = file.Payment?.PaymentId,
                        SessionsPayed = file.Payment?.SessionsPayed,
                        ScholarshipBasePayment = file.Payment?.ScholarshipBasePayment
                    }
                });
            }
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
    ///{
    ///   "scholarshipStartDate": "2025-01-10",
    ///   "criminalRecordExpiryDate": "2026-01-10",
    ///   "medicalRecordExpiryDate": "2025-07-10",
    ///   "status": "open",
    ///   "teachingCategoryId": 10,
    ///   "vehicleId": 301,          // optional
    ///   "instructorId": 41,        // optional
    ///   "payment": {
    ///     "sessionsPayed": 0,
    ///     "scholarshipBasePayment": false
    /// }
    ///}
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

        if (fileDto == null)
            return BadRequest("Empty request.");

        var userAdmin = _db.ApplicationUsers.Find(User);
        var userStudent = _db.ApplicationUsers.Find(studentId);

        if (userStudent == null)
            return BadRequest("Student not found.");

        if (userAdmin?.AutoSchoolId == userStudent.AutoSchoolId)
        {
            return Forbid("This student does not belong to your auto school");
        }

        var file = new Models.File
        {
            ScholarshipStartDate = fileDto.scholarschipStartDate,
            CriminalRecordExpiryDate = fileDto.criminalRecordExpiryDate,
            MedicalRecordExpiryDate = fileDto.medicalRecordExpiryDate,
            //File.cs/FileStatus class, DRAFT/APPROVED/REJECTED,
            //astept sa imi spui gabi cum modificam aici
            //pe card vad sa apara ca `open`
            Status = fileDto.status, 
            TeachingCategoryId = fileDto.teachingCategoryId,
            StudentId = studentId,
            //VehicleId && InstructorId are optional therefore ternary operator should
            //preferably be used here i believe
            VehicleId = fileDto.vehicleId == null ? null : fileDto.vehicleId,
            InstructorId = fileDto.instructorId == null ? null : fileDto.instructorId
        };


        await _db.Files.AddAsync(file);
        var payment = new Payment
        {
            ScholarshipBasePayment = fileDto.payment.ScholarshipBasePayment,
            SessionsPayed = fileDto.payment.SessionsPayed,
            FileId = file.FileId
            //aici sper sa nu pice, adica atunci cand adaug obiectul in BD
            //ar trebui sa primeasca si ID implicit..i hope
        };

        await _db.Payments.AddAsync(payment);



        _db.Files.Add(file);
        _db.SaveChanges();


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
    public async Task<IActionResult> EditFile(int fileId, [FromBody] FileDto fileDto)
    {

        var file = _db.Files.Find(fileId);
        if(file == null)
        {
            return BadRequest("File not found to edit");
        }
        int? fileSchoolId = file.Student.AutoSchoolId;

        if (fileSchoolId == null)
        {
            return BadRequest("File does not belong to any auto school");
        }
        if(file?.Instructor?.AutoSchoolId != fileSchoolId)
        {
            return BadRequest("Instructor does not belong to the same auto school");
        }
        if(file?.Vehicle?.AutoSchoolId != fileSchoolId)
        {
            return BadRequest("Vehicle does not belong to the same auto school");
        }


        var schoolAdmin = _users.GetUserAsync(User).Result;

        if(schoolAdmin?.AutoSchoolId != fileSchoolId)
        {
            return Forbid("You can not edit the files of other auto schools");
        }

        //Aici va trebui sa modificam din structura de fileDto,
        //pana atunci ramane comentat  vvv

     //->>>>  file.InstructorId = fileDto.InstructorId;

        file.VehicleId = fileDto.VehicleId;


        //Aici ar trebui actualizat campul status sa fie acel enum FileStatus
        //nu un string
     //->>>>      file.Status = fileDto.Status;
        file.ScholarshipStartDate = fileDto.ScholarshipStartDate;
        file.CriminalRecordExpiryDate  = fileDto.CriminalRecordExpiryDate;
        file.MedicalRecordExpiryDate = fileDto.MedicalRecordExpiryDate;
        file.TeachingCategoryId = fileDto.TeachingCategoryId;

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

        var payment = _db.Payments.Find(paymentId);
        if (payment == null)
        {
            return BadRequest("Payment not found to edit");
        }

        var schoolAdmin = _users.GetUserAsync(User).Result;

        var file = _db.Files.Find(payment.FileId);
        if (file == null)
        {
            return BadRequest("File not found to edit");
        }
        int? fileSchoolId = file.Student.AutoSchoolId;

        if (fileSchoolId == null)
        {
            return BadRequest("File does not belong to any auto school");
        }

        if (schoolAdmin?.AutoSchoolId != fileSchoolId)
        {
            return Forbid("You can not edit the files nor payments of other auto schools");
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

        var file = await _db.Files.FindAsync(fileId);
        if (file == null)
            return NotFound(new { message = "File not found." });

        if(file.Student.AutoSchoolId != _users.GetUserAsync(User).Result?.AutoSchoolId)
        {
            return BadRequest("You can not delete the files of other auto schools");
        }
        _db.Files.Remove(file);
        await _db.SaveChangesAsync();
        return Ok(new
        {
            message = "File deleted successfully"
        });
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
    public DateTime scholarschipStartDate { get; init; } = default!;
    public DateTime criminalRecordExpiryDate { get; init; } = default!;

    public DateTime medicalRecordExpiryDate { get; init; } = default!;

    public FileStatus status { get; init; } = default!;

    public int? teachingCategoryId { get; init; } = default!;

    public int? vehicleId { get; init; } = default!;

    public string? instructorId { get; init; } = default!;

    public PaymentDto? payment { get; init; } = default!;


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
    [Required]
    public string StudentId { get; init; } = default!;

    [Required]
    public string FirstName { get; init; } = default!;

    [Required]
    public string LastName { get; init; } = default!;

    [Required]
    public string Email { get; init; } = default!;

    [Required]
    public string PhoneNumber { get; init; } = default!;

    [Required]
    public string Address { get; init; } = default!;

    [Required]
    public string? Cnp { get; init; } = default!;

    [Required]
    public int AutoSchoolId { get; init; } = default!;
}
public sealed class StudentFileDataInstructorDto
{
    [Required]

    public string InstructorId { get; init; } = default!;
    [Required]

    public string FirstName { get; init; } = default!;
    [Required]

    public string LastName { get; init; } = default!;

}
public sealed class FilePaymentDto
{
    public int PaymentId { get; init; } = default!;
    public bool ScholarshipBasePayment { get; init; }
    public int SessionsPayed { get; init; }
}

public sealed class FilesDataDto
{
    [Required]

    public int FileId { get; init; } = default!;
    [Required]

    public DateTime ScholarshipStartDate { get; init; } = default!;
    [Required]

    public DateTime CriminalRecordExpiryDate { get; init; } = default!;
    [Required]

    public DateTime MedicalRecordExpiryDate { get; init; } = default!;
    [Required]

    public FileStatus Status { get; init; } = default!;
    [Required]

    public TeachingCategoryDto TeachingCategory { get; init; } = default!;
    [Required]

    public VehicleDto Vehicle { get; init; } = default!;
    [Required]

    public StudentFileDataInstructorDto Instructor { get; init; } = default!;
    [Required]

    public PaymentDto Payment { get; init; } = default!;
}


public sealed class StudentFileRecordsDto
{
    [Required]
    public StudentDataDto StudentData;
    [Required]
    public List<FilesDataDto> Files;
}


