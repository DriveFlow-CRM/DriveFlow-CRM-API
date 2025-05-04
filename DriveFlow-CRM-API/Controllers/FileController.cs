using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/file")]
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



    // ────────────────────────────── CREATE FILE ──────────────────────────────
    /// <summary>
    /// 
    /// </summary>
    /// <param name="studentId">The id of the student that previously made
    /// the request to enroll with at this auto school</param>
    /// <param name="fileDto">All the necessary content of the file</param>
    /// <returns></returns>


    [HttpPost("createFile/{studentId}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> CreateFile(string studentId,[FromBody] CreateFileDto fileDto )
    {
        if (studentId == null)
            return BadRequest("Invalid user ID.");
        if (fileDto == null)
            return BadRequest("Empty request.");

        var userAdmin = _db.ApplicationUsers.Find(User);
        var userStudent = _db.ApplicationUsers.Find(studentId);

        if(userAdmin == null)
            return BadRequest("User not found.");
        if (userStudent == null)
            return BadRequest("Student not found.");

        if (userAdmin.AutoSchoolId == userStudent.AutoSchoolId)
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
            ScholarshipBasePayment = fileDto.payment.scholarshipBasePayment,
            SessionsPayed = fileDto.payment.sessionPayed,
            FileId = file.FileId
            //aici sper sa nu pice, adica atunci cand adaug obiectul in BD
            //ar trebui sa primeasca si ID implicit..i hope
        };

        await _db.Payments.AddAsync(payment);



        _db.Files.Add(file);
        _db.SaveChanges();



        return Ok(new CreateFileResponseDto()
        {
            fileId = file.FileId,
            paymentId = payment.PaymentId,
            message = "File created successfully"
        });
    }
}


///<see cref="FileDto"/>

public sealed class CreateFilePaymentDto
{
    public int sessionPayed = 0;
    public bool scholarshipBasePayment = false;
}
public sealed class CreateFileDto
{
    public DateTime scholarschipStartDate { get; init; } = default!;
    public DateTime criminalRecordExpiryDate { get; init; } = default!;

    public DateTime medicalRecordExpiryDate { get; init; } = default!;

    public FileStatus status { get; init; } = default!;

    public int teachingCategoryId { get; init; } = default!;

    public int? vehicleId { get; init; } = default!;

    public string? instructorId { get; init; } = default!;

    public CreateFilePaymentDto payment { get; init; } = default!;


}


public sealed class CreateFileResponseDto
{
    public int fileId { get; init; } = default!;
    public int paymentId { get; init; } = default!;
    public string message { get; init; } = default!;
}