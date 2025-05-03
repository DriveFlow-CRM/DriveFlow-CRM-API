using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/request")]
public class FileController : ControllerBase
{

    // ───────────────────────────── fields & ctor ─────────────────────────────
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




    //────────────────────────────── GET FILE STUDENT RECORDS ──────────────────────────────

    /// <response code="200">Request sent succesffully.</response>
    /// <response code="400">Empty request</response>>
    /// <response code="401">No valid JWT supplied.</response>
    /// <response code="403">User is forbidden from seeing the requests of this auto school.</response>

    [HttpGet("createRequest/{schoolId}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> GetStudentFileRecords(int schoolId)
    {
        if (schoolId < 0)
        {
            return BadRequest("Invalid school ID.");
        }
        var user = await _users.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }
        if (user.AutoSchoolId != schoolId)
        {
            return Forbid("You are not allowed to see the requests of this auto school.");
        }

        var autoSchool = _db.AutoSchools.Find(schoolId);
        if (autoSchool == null)
        {
            return NotFound("Auto school not found.");
        }

        var Students = autoSchool.ApplicationUsers.Where(u => _users.IsInRoleAsync(u, "Student").Result).ToList();

        if (Students == null || Students.Count == 0)
        {
            return NotFound("No students found for this auto school.");
        }

        List<KeyValuePair<StudentUserDto, List<FileDto>>> studentFiles = new List<KeyValuePair<StudentUserDto, List<FileDto>>>();

        //foreach(var student in Students)
        //{
        //    var studentFilesDto = new List<FileDto>();
        //    var files = _db.Files.Where(f => f.ApplicationUserId == student.Id).ToList();
        //    if (files != null && files.Count > 0)
        //    {
        //        foreach (var file in files)
        //        {
        //            studentFilesDto.Add(new FileDto
        //            {
        //                FileId = file.FileId,
        //                FileName = file.FileName,
        //                FileType = file.FileType,
        //                FileSize = file.FileSize,
        //                CreatedAt = file.CreatedAt
        //            });
        //        }
        //    }
        //    studentFiles.Add(new KeyValuePair<StudentUserDto, List<FileDto>>(new StudentUserDto
        //    {
        //        UserId = student.Id,
        //        FirstName = student.FirstName,
        //        LastName = student.LastName,
        //        Cnp = student.Cnp
        //    }, studentFilesDto));
        //}



        return Ok(studentFiles);



    }
}

