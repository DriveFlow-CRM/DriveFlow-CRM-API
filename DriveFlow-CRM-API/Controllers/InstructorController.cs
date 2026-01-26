using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using File = DriveFlow_CRM_API.Models.File;

namespace DriveFlow_CRM_API.Controllers;

/// <summary>
/// Instructor-specific endpoints for the DriveFlow CRM API.
/// </summary>
/// <remarks>
/// Exposes endpoints for instructors to manage their files and track student progress.
/// All endpoints require authentication and are restricted to users with the Instructor role.
/// </remarks>
[ApiController]
[Route("api/instructor")]
[Authorize(Roles = "Instructor,SchoolAdmin")]
public class InstructorController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    /// <summary>
    /// Constructor injected by the framework with request‑scoped services.
    /// </summary>
    public InstructorController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves all files assigned to a specific instructor with student and vehicle details.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "fileId": 1,
    ///     "firstName": "Maria",
    ///     "lastName": "Ionescu",
    ///     "phoneNumber": "+40 712 345 678",
    ///     "email": "maria.ionescu@email.com",
    ///     "scholarshipStartDate": "2025-02-01",
    ///     "licensePlateNumber": "B‑12‑XYZ",
    ///     "transmissionType": "MANUAL",
    ///     "status": "ARCHIVED",
    ///     "type": "B",
    ///     "color": "red"
    ///   },
    ///   {
    ///     "fileId": 2,
    ///     "firstName": "Andrei",
    ///     "lastName": "Pop",
    ///     "phoneNumber": "+40 745 987 654",
    ///     "email": "andrei.pop@example.com",
    ///     "scholarshipStartDate": "2025-03-10",
    ///     "licensePlateNumber": "CJ‑34‑ABC",
    ///     "transmissionType": "AUTOMATIC",
    ///     "status": "ARCHIVED",
    ///     "type": "BE",
    ///     "color": "blue"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="instructorId">The ID of the instructor whose assigned files to retrieve</param>
    /// <response code="200">Files retrieved successfully. Returns empty array if no files found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to access these files.</response>
    [HttpGet("{instructorId}/fetchInstructorAssignedFiles")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<IEnumerable<InstructorAssignedFileDto>>> FetchInstructorAssignedFiles(string instructorId)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 2. Verify the authenticated user is the same as the requested instructorId
        if (userId != instructorId)
        {
            return Forbid(); // Return 403 Forbidden if trying to access another instructor's data
        }

        // 3. Query files with required joins
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var files = await _db.Files
            .Where(f => f.InstructorId == instructorId)
            .Include(f => f.Student)
            .Include(f => f.Vehicle)
            .Include(f => f.TeachingCategory)
                .ThenInclude(tc => tc.License)
            .AsNoTracking()
            .ToListAsync();
#pragma warning restore CS8602

        // 4. Map to DTOs after materializing the query, with additional null checks
        var result = files.Select(f => 
        {
            var student = f.Student; // Avoid multiple property access that could trigger warning
            var vehicle = f.Vehicle; // Avoid multiple property access that could trigger warning
            var teachingCategory = f.TeachingCategory; // Avoid multiple property access
            
            return new InstructorAssignedFileDto
            {
                FileId = f.FileId,
                FirstName = student?.FirstName,
                LastName = student?.LastName,
                PhoneNumber = student?.PhoneNumber,
                Email = student?.Email,
                ScholarshipStartDate = f.ScholarshipStartDate?.Date,
                LicensePlateNumber = vehicle?.LicensePlateNumber,
                TransmissionType = vehicle != null ? vehicle.TransmissionType.ToString() : null,
                Status = f.Status.ToString(),
                Type = teachingCategory?.License?.Type ?? teachingCategory?.Code,
                Color = vehicle?.Color
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Retrieves detailed information about a specific file assigned to the authenticated instructor.
    /// </summary>
    /// <remarks>
    /// <para>Returns comprehensive file details including student information, file status, payment details, and lesson history.</para>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// {
    ///   "firstName": "Maria",
    ///   "lastName": "Ionescu",
    ///   "email": "maria.ionescu@email.com",
    ///   "phoneNo": "+40 712 345 678",
    ///   "scholarshipStartDate": "2025-02-01",
    ///   "criminalRecordExpiryDate": "2026-02-01",
    ///   "medicalRecordExpiryDate": "2025-08-01",
    ///   "status": "APPROVED",
    ///   "scholarshipPayment": true,
    ///   "sessionsPayed": 30,
    ///   "minDrivingLessonsRequired": 30,
    ///   "lessonsMade": [
    ///     "2025-03-01",
    ///     "2025-03-05",
    ///     "2025-03-12"
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="fileId">The ID of the file to retrieve details for</param>
    /// <response code="200">File details retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">File not found or not assigned to the authenticated instructor.</response>
    [HttpGet("fetchFileDetails/{fileId:int}")]
    [Authorize(Roles = "Instructor")]

    public async Task<ActionResult<InstructorFileDetailsDto>> FetchFileDetails(int fileId)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 2. Get current date and time for lessons filtering
        var now = DateTime.Now;

        // 3. Query file with all necessary data
        var file = await _db.Files
            .Where(f => f.FileId == fileId && f.InstructorId == userId)
            .Include(f => f.Student)
            .Include(f => f.TeachingCategory)
            .Include(f => f.Appointments)
            .FirstOrDefaultAsync();

        if (file == null)
        {
            return NotFound();
        }

        // 4. Get payment information separately
        var payment = await _db.Payments
            .Where(p => p.FileId == fileId)
            .FirstOrDefaultAsync();

        // 5. Create the DTO with all collected data
        var fileDetails = new InstructorFileDetailsDto
        {
            FirstName = file.Student?.FirstName,
            LastName = file.Student?.LastName,
            Email = file.Student?.Email,
            PhoneNo = file.Student?.PhoneNumber,
            ScholarshipStartDate = file.ScholarshipStartDate?.Date,
            CriminalRecordExpiryDate = file.CriminalRecordExpiryDate?.Date,
            MedicalRecordExpiryDate = file.MedicalRecordExpiryDate?.Date,
            Status = file.Status.ToString(),
            ScholarshipPayment = payment != null && payment.ScholarshipBasePayment,
            SessionsPayed = payment != null ? payment.SessionsPayed : 0,
            MinDrivingLessonsRequired = file.TeachingCategory?.MinDrivingLessonsReq ?? 0,
            LessonsMade = file.Appointments
                .Where(a => a.Date.Add(a.EndHour) <= now)
                .OrderBy(a => a.Date)
                .Select(a => a.Date.Add(a.StartHour).Date)
                .ToList()
        };

        return Ok(fileDetails);
    }

    /// <summary>
    /// Retrieves all future appointments for an instructor filtered by date range.
    /// </summary>
    /// <remarks>
    /// <para>Returns a list of appointments with student and vehicle details.</para>
    /// <para><strong>Sample response</strong></para>
    ///
    /// ```json
    /// [
    ///   {
    ///     "appointmentId": 5012,
    ///     "date": "2025-05-15",
    ///     "startHour": "09:00",
    ///     "endHour": "11:00",
    ///     "fileId": 918,
    ///     "firstName": "Maria",
    ///     "lastName": "Ionescu",
    ///     "phoneNo": "+40 712 345 678",
    ///     "licensePlateNumber": "B‑12‑XYZ",
    ///     "type": "B"
    ///   },
    ///   {
    ///     "appointmentId": 5013,
    ///     "date": "2025-05-16",
    ///     "startHour": "14:00",
    ///     "endHour": "16:00",
    ///     "fileId": 922,
    ///     "firstName": "Andrei",
    ///     "lastName": "Pop",
    ///     "phoneNo": "+40 745 987 654",
    ///     "licensePlateNumber": "CJ‑34‑ABC",
    ///     "type": "BE"
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <param name="instructorId">The ID of the instructor whose appointments to retrieve</param>
    /// <param name="startDate">Start date for filtering appointments (inclusive)</param>
    /// <param name="endDate">End date for filtering appointments (inclusive)</param>
    /// <response code="200">Appointments retrieved successfully. Returns empty array if no appointments found.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to access these appointments.</response>
    [HttpGet("{instructorId}/fetchInstructorAppointments/{startDate}/{endDate}")]
    [Authorize(Roles = "Instructor,SchoolAdmin")]
    public async Task<ActionResult<IEnumerable<InstructorAppointmentDto>>> FetchInstructorAppointments(string instructorId, DateTime startDate, DateTime endDate)
    {
        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 2. Check if caller is Instructor role and verify access rights if so
        var isCallerInstructor = User.IsInRole("Instructor");
        if (isCallerInstructor && userId != instructorId)
        {
            return Forbid(); // Return 403 Forbidden if instructor trying to access another instructor's data
        }

        // 3. Query appointments with required joins
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var query = from instructor in _db.ApplicationUsers
                    where instructor.Id == instructorId
                    join file in _db.Files
                        .Include(f => f.Student)
                        .Include(f => f.Vehicle)
                        .Include(f => f.TeachingCategory)
                            .ThenInclude(tc => tc.License)
                        on instructor.Id equals file.InstructorId
                    join appointment in _db.Appointments
                        on file.FileId equals appointment.FileId
                    where appointment.Date >= startDate && appointment.Date <= endDate
                    orderby appointment.Date, appointment.StartHour
                    select new { appointment, file };

        // 4. Execute the query and materialize the results
        var results = await query.AsNoTracking().ToListAsync();
#pragma warning restore CS8602

        // 5. Map to DTOs safely
        var appointments = results.Select(r => 
        {
            var student = r.file.Student; // Avoid multiple property access that could trigger warning
            var vehicle = r.file.Vehicle; // Avoid multiple property access that could trigger warning
            var teachingCategory = r.file.TeachingCategory; // Avoid multiple property access
            
            return new InstructorAppointmentDto
            {
                AppointmentId = r.appointment.AppointmentId,
                Date = r.appointment.Date.Date,
                StartHour = r.appointment.StartHour.ToString(@"hh\:mm"),
                EndHour = r.appointment.EndHour.ToString(@"hh\:mm"),
                FileId = r.file.FileId,
                FirstName = student?.FirstName,
                LastName = student?.LastName,
                PhoneNo = student?.PhoneNumber,
                LicensePlateNumber = vehicle?.LicensePlateNumber,
                Type = teachingCategory?.License?.Type ?? teachingCategory?.Code
            };
        }).ToList();

        return Ok(appointments);
    }

    /// <summary>
    /// Retrieves a distribution chart of mistakes made by students in a specific cohort.
    /// </summary>
    /// <remarks>
    /// <para><strong>Sample response for 419decbe-6af1-4d84-9b45-c1ef796f4607</strong></para>
    ///
    /// ``` 
    ///    {
    ///  "histogramtotalpoints": [
    ///    {
    ///      "bucket": "0-10",
    ///      "count": 0
    ///    },
    ///    {
    ///      "bucket": "11-20",
    ///      "count": 3
    ///    },
    ///    {
    ///    "bucket": "21+",
    ///      "count": 3
    ///    }
    ///  ],
    ///  "topitemsbystudent": [
    ///    {
    ///      "studentid": "419decbe-6af1-4d84-9b45-c1ef796f4604",
    ///      "studentname": "Mihail Constantin",
    ///      "items": [
    ///        {
    ///          "id_item": 20,
    ///          "count": 2
    ///        },
    ///        {
    ///    "id_item": 11,
    ///          "count": 1
    ///        },
    ///        {
    ///    "id_item": 21,
    ///          "count": 1
    ///        }
    ///      ]
    ///    },
    ///    {
    ///    "studentid": "419decbe-6af1-4d84-9b45-c1ef796f4605",
    ///      "studentname": "Ana Absinte",
    ///      "items": [
    ///        {
    ///        "id_item": 5,
    ///          "count": 2
    ///        },
    ///        {
    ///        "id_item": 38,
    ///          "count": 2
    ///        },
    ///        {
    ///        "id_item": 15,
    ///          "count": 2
    ///        }
    ///      ]
    ///    },
    ///    {
    ///    "studentid": "419decbe-6af1-4d84-9b45-c1ef796f4606",
    ///      "studentname": "Sandu Ilie",
    ///      "items": [
    ///        {
    ///        "id_item": 73,
    ///          "count": 3
    ///        },
    ///        {
    ///        "id_item": 76,
    ///          "count": 1
    ///        },
    ///        {
    ///        "id_item": 78,
    ///          "count": 1
    ///        }
    ///      ]
    ///    }
    ///  ],
    ///  "failureRate": 0.5
    ///}
    /// ```
    /// </remarks>
    /// <param name="instructorId">The ID of the instructor whose appointments to retrieve</param>
    /// <response code="200">Items stats retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to access these appointments.</response>
    [HttpGet("{instructorId}/stats/cohort")]
    [Authorize(Roles = "Instructor,SchoolAdmin")]

    public async Task<ActionResult<InstructorCohortStatsDto>> GetInstructorCohortStats(string instructorId)
    {

        // 1. Get authenticated user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        
        if (User.IsInRole("Instructor") && userId != instructorId )
        {
            return Forbid(); 
        }

        if(User.IsInRole("SchoolAdmin"))
        {
            int? schoolId = _db.Users.Where(u => u.Id == userId)
                .Select(u => u.AutoSchoolId)
                .FirstOrDefault();
            int? instructorSchoolId = _db.Users.Where(u => u.Id == instructorId)
                .Select(u => u.AutoSchoolId)
                .FirstOrDefault();
            if(schoolId==null || instructorSchoolId==null || schoolId!=instructorSchoolId)
                return Forbid();
            

        }

        DateTime from,to;

        if (Request.Query.ContainsKey("from")) {
            var fromStr = Request.Query["from"].ToString();
            if(!DateTime.TryParse(fromStr, out from))
            {
                return BadRequest();
            }
        }
        else
        {
            from = DateTime.MinValue;
        }

        if (Request.Query.ContainsKey("to"))
        {
            var toStr = Request.Query["to"].ToString();
            if (!DateTime.TryParse(toStr, out to))
            {
                return BadRequest();
            }
        }
        else
        {
            to = DateTime.Now;
        }


        //var obj = _db.SessionForms
        //        .Where(s => s.SessionFormId == 2)
        //        .Select(s => s.MistakesJson).ToList().First().ToString();


        //try
        //{
        //    List<(int id_item, int count)> items = System.Text.Json.JsonSerializer.Deserialize<List<(int id_item, int count)>>(obj)!;
        //}catch(Exception ex)
        //{
        //    return BadRequest("Deserialization error: " + ex.Message);
        //}




        var sessionForms = await _db.SessionForms
            .Where(f => f.Appointment.File.InstructorId == instructorId
                        && f.Appointment.Date >= from
                        && f.Appointment.Date <= to.AddYears(2))
            .Select(f => new
            {
                f.TotalPoints,
                f.MistakesJson,
                f.Result,
                f.Appointment.File.StudentId,
                f.Appointment.File.Student.FirstName,
                f.Appointment.File.Student.LastName,
            })
            .AsNoTracking()
            .ToListAsync();


        if (sessionForms.Count == 0)
        {
            return Ok(new InstructorCohortStatsDto(
                new List<Bucket>()
                {
                    new Bucket("0-10", 0),
                    new Bucket("11-20", 0),
                    new Bucket("21+", 0)
                },
                new List<StudentItemAgg>(),
                0));
        }
        ///0-10 | 11-20 | 21+ points buckets
        int bucket1 = 0;
        int bucket2 = 0;
        int bucket3 = 0;

        foreach(var se in sessionForms)
        {
            if(se.TotalPoints <=10)
            {
                bucket1++;
            }
            else if(se.TotalPoints <=20)
            {
                bucket2++;
            }
            else
            {
                bucket3++;
            }
        }
        var studentMistakesAgg = new List<StudentItemAgg>();

        List<(string, string)> students = sessionForms
          .Select(s => (s.StudentId, s.FirstName + " " + s.LastName))
          .Distinct()
          .ToList();




        foreach (var student in students)
        {
            Dictionary<int, int> allMistakes = new Dictionary<int, int>();
            List<string> mistakesjson = sessionForms
                .Where(se => se.StudentId == student.Item1)
                .Select(se => se.MistakesJson)
                .ToList();
            foreach (string mistake in mistakesjson)
            {
                List<MistakeEntry> items = System.Text.Json.JsonSerializer.Deserialize<List<MistakeEntry>>(mistake)!;
                if(items.Count==0)
                {
                    continue;
                }

                foreach (var pair in items)
                {
                    if (allMistakes.ContainsKey(pair.id_item))
                    {
                        allMistakes[pair.id_item] = allMistakes[pair.id_item] + pair.count;
                    }
                    else
                    {
                        allMistakes.Add(pair.id_item, pair.count);
                    }
                }
            }


            var top = allMistakes
                .OrderByDescending(kv => kv.Value)
                .Take(1)      //can be changed, depending how many we want
                .Select(kv => (kv.Key, kv.Value))
                .ToList();

            List<MistakeEntry> topItems = new List<MistakeEntry>();   
            foreach (var t in top)
            {
                topItems.Add( new MistakeEntry(t.Item1, t.Item2));
            }

            studentMistakesAgg.Add(new StudentItemAgg(student.Item1, student.Item2, topItems ));
        }




        float failureRate = 0;
        failureRate = float.Round((float)sessionForms.Where(s=>s.Result=="FAILED").Count() / sessionForms.Count() ,2);


        //return Ok(studentMistakesAgg[);

        return Ok(new InstructorCohortStatsDto(
            new List<Bucket>()
            {
                new Bucket("0-10", bucket1),
                new Bucket("11-20", bucket2),
                new Bucket("21+", bucket3)
            },
            studentMistakesAgg,
            failureRate));
    }


}

public sealed class Bucket
{
    /// <summary>Bucket label (presentation string).</summary>
    public string bucket { get; init; }

    /// <summary>Number of items/sessions in the bucket.</summary>
    public int count { get; init; }

    public Bucket(string bucket, int count)
    {
        this.bucket = bucket;
        this.count = count;
    }
}



public sealed class StudentItemAgg
{
    /// <summary>Primary key / identifier of the student (Identity user id).</summary>
    public string studentid { get; init; }

    /// <summary>Display name for the student (suitable for UI).</summary>
    public string studentname { get; init; }

    public IEnumerable<MistakeEntry> items { get; init; } 
    public StudentItemAgg(string studentid, string studentname, IEnumerable<MistakeEntry> items)
    {
        this.studentid = studentid;
        this.studentname = studentname;
        this.items = items;
    }
}

public sealed class InstructorCohortStatsDto
{
    /// <summary>Score distribution as an ordered sequence of <see cref="Bucket"/>.</summary>
    public IEnumerable<Bucket> histogramtotalpoints { get; init; } = Array.Empty<Bucket>();

    /// <summary>Per‑student aggregated mistake counts.</summary>
    public IEnumerable<StudentItemAgg> topitemsbystudent { get; init; } = Array.Empty<StudentItemAgg>();

    /// <summary>Failure rate expressed as a fraction between 0 and 1.</summary>
    public double failureRate { get; init; }

    public InstructorCohortStatsDto(IEnumerable<Bucket> histogramtotalpoints, IEnumerable<StudentItemAgg> topitemsbystudent, double failurerate)
    {
        this.histogramtotalpoints = histogramtotalpoints ?? Array.Empty<Bucket>();
        this.topitemsbystudent = topitemsbystudent ?? Array.Empty<StudentItemAgg>();
        this.failureRate = failurerate;
    }
}
/// <summary>
/// DTO for instructor assigned file information
/// </summary>
public sealed class InstructorAssignedFileDto
{
    /// <summary>File identifier</summary>
    public int FileId { get; init; }
    
    /// <summary>Student's first name</summary>
    public string? FirstName { get; init; }
    
    /// <summary>Student's last name</summary>
    public string? LastName { get; init; }
    
    /// <summary>Student's phone number</summary>
    public string? PhoneNumber { get; init; }
    
    /// <summary>Student's email address</summary>
    public string? Email { get; init; }
    
    /// <summary>Date when scholarship starts</summary>
    public DateTime? ScholarshipStartDate { get; init; }
    
    /// <summary>Vehicle license plate number</summary>
    public string? LicensePlateNumber { get; init; }
    
    /// <summary>Vehicle transmission type (manual/automatic)</summary>
    public string? TransmissionType { get; init; }
    
    /// <summary>File status</summary>
    public string Status { get; init; } = null!;
    
    /// <summary>License type</summary>
    public string? Type { get; init; }
    
    /// <summary>Vehicle color</summary>
    public string? Color { get; init; }
}

/// <summary>
/// DTO for detailed file information retrieved by instructors
/// </summary>
public sealed class InstructorFileDetailsDto
{
    /// <summary>Student's first name</summary>
    public string? FirstName { get; init; }
    
    /// <summary>Student's last name</summary>
    public string? LastName { get; init; }
    
    /// <summary>Student's email address</summary>
    public string? Email { get; init; }
    
    /// <summary>Student's phone number</summary>
    public string? PhoneNo { get; init; }
    
    /// <summary>Date when scholarship starts</summary>
    public DateTime? ScholarshipStartDate { get; init; }
    
    /// <summary>Expiry date for criminal record</summary>
    public DateTime? CriminalRecordExpiryDate { get; init; }
    
    /// <summary>Expiry date for medical certificate</summary>
    public DateTime? MedicalRecordExpiryDate { get; init; }
    
    /// <summary>File status</summary>
    public string Status { get; init; } = null!;
    
    /// <summary>Whether scholarship payment is complete</summary>
    public bool ScholarshipPayment { get; init; }
    
    /// <summary>Number of sessions paid for</summary>
    public int SessionsPayed { get; init; }
    
    /// <summary>Minimum required driving lessons</summary>
    public int MinDrivingLessonsRequired { get; init; }
    
    /// <summary>Dates of completed lessons</summary>
    public List<DateTime> LessonsMade { get; init; } = new List<DateTime>();
}

/// <summary>
/// DTO for instructor appointment information
/// </summary>
public sealed class InstructorAppointmentDto
{
    /// <summary>Appointment identifier</summary>
    public int AppointmentId { get; init; }
    
    /// <summary>Date of the appointment</summary>
    public DateTime Date { get; init; }
    
    /// <summary>Start time of the appointment</summary>
    public string StartHour { get; init; } = null!;
    
    /// <summary>End time of the appointment</summary>
    public string EndHour { get; init; } = null!;
    
    /// <summary>Associated file identifier</summary>
    public int FileId { get; init; }
    
    /// <summary>Student's first name</summary>
    public string? FirstName { get; init; }
    
    /// <summary>Student's last name</summary>
    public string? LastName { get; init; }
    
    /// <summary>Student's phone number</summary>
    public string? PhoneNo { get; init; }
    
    /// <summary>Vehicle license plate number</summary>
    public string? LicensePlateNumber { get; init; }
    
    /// <summary>License type</summary>
    public string? Type { get; init; }
} 