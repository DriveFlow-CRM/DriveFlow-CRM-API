using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow_CRM_API.Models;

    /// <summary>
    /// Seeds default roles, users, and test data for the DriveFlow CRM database.
    /// This method is invoked from Program.cs at application startup.
    /// </summary>
    public static class SeedData
    {
    /// <summary>
    /// Inserts initial data only if the AspNetRoles table is empty.
    /// Idempotent so it can be executed multiple times safely.
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        void LogDebug(string hypothesisId, string location, string message, object data)
        {
            try
            {
                var logPath = Environment.GetEnvironmentVariable("DEBUG_LOG_PATH") ?? "/debug/debug.log";
                var payload = new
                {
                    sessionId = "debug-session",
                    runId = "pre-fix",
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                var line = System.Text.Json.JsonSerializer.Serialize(payload);
                System.IO.File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch
            {
                // avoid breaking startup on log failure
            }
        }

        // Resolve ApplicationDbContext with the DI‑configured options.
        using var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

        // Seed configuration/constants (keep IDs stable for relationships).
        var schoolIds = new[] { 1, 2, 3, 4 };
        var licenseTypes = new[]
        {
            "AM", "A1", "A2", "A", "B1", "B", "BE",
            "C1", "C1E", "C", "CE",
            "D1", "D1E", "D", "DE"
        };

        const string roleSuperAdminId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc0";
        const string roleSchoolAdminId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc1";
        const string roleStudentId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2";
        const string roleInstructorId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3";

        const string superAdminPassword = "admin123";
        const string schoolAdminPassword = "admin123";
        const string studentPassword = "student123";
        const string instructorPassword = "instructor123";

        var teachingCategoryIdsBySchool = new Dictionary<int, List<int>>();
        var instructorIdsBySchool = new Dictionary<int, List<string>>();
        var studentIdsBySchool = new Dictionary<int, List<string>>();
        var schoolAdminIdsBySchool = new Dictionary<int, string>();
        var vehicleIdsBySchool = new Dictionary<int, List<int>>();
        var licenseIdByType = new Dictionary<string, int>();

        // #region agent log
        LogDebug("H3", "SeedData.cs:32", "seed start", new { });
        // #endregion

        bool ColumnExists(string tableName, string columnName)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = @table
                  AND COLUMN_NAME = @column";

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@table";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);

            var columnParam = command.CreateParameter();
            columnParam.ParameterName = "@column";
            columnParam.Value = columnName;
            command.Parameters.Add(columnParam);

            var result = command.ExecuteScalar();
            if (shouldClose)
            {
                connection.Close();
            }

            return result != null && Convert.ToInt32(result) > 0;
        }

        // ──────────────── Roles ────────────────
        try
        {
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc0", Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                    new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc1", Name = "SchoolAdmin", NormalizedName = "SCHOOLADMIN" },
                    new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2", Name = "Student", NormalizedName = "STUDENT" },
                    new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3", Name = "Instructor", NormalizedName = "INSTRUCTOR" }
                );
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            // #region agent log
            LogDebug("H4", "SeedData.cs:46", "roles seed failed", new { error = ex.GetType().Name, message = ex.Message });
            // #endregion
            throw;
        }

        if(false){
            var hasher = new PasswordHasher<ApplicationUser>();

            //context.Users.AddRange(new ApplicationUser
            //{
            //    Id = "419decbe-6af1-4d84-9b45-c1ef796f4604",
            //    UserName = "mihailconstantin@gmail.com",
            //    NormalizedUserName = "MIHAILCONSTANTIN@GMAIL.COM",
            //    Email = "mihailconstantin@gmail.com",
            //    NormalizedEmail = "MIHAILCONSTANTIN@GMAIL.COM",
            //    EmailConfirmed = true,
            //    PasswordHash = hasher.HashPassword(new ApplicationUser(), "mihail123*"),
            //    FirstName = "Mihail",
            //    LastName = "Constantin",
            //    AutoSchoolId = 1
            //},


            //    new ApplicationUser
            //    {
            //        Id = "419decbe-6af1-4d84-9b45-c1ef796f4605",
            //        UserName = "anaabsinte@gmail.com",
            //        NormalizedUserName = "ANAABSINTE@GMAIL.COM",
            //        Email = "anaabsinte@gmail.com",
            //        NormalizedEmail = "ANAABSINTE@GMAIL.COM",
            //        EmailConfirmed = true,
            //        PasswordHash = hasher.HashPassword(new ApplicationUser(), "longlivabsinth969*"),
            //        FirstName = "Ana",
            //        LastName = "Absinte",
            //        AutoSchoolId = 1
            //    },

            //    new ApplicationUser
            //    {
            //        Id = "419decbe-6af1-4d84-9b45-c1ef796f4606",
            //        UserName = "sanduilie@gmail.com",
            //        NormalizedUserName = "SANDUILIE@GMAIL.COM",
            //        Email = "sanduilie@gmail.com",
            //        NormalizedEmail = "SANDUILIE@GMAIL.COM",
            //        EmailConfirmed = true,
            //        PasswordHash = hasher.HashPassword(new ApplicationUser(), "gloryto^ROMANIA^*"),
            //        FirstName = "Sandu",
            //        LastName = "Ilie",
            //        AutoSchoolId = 1
            //    },

            //    //
            //    new ApplicationUser
            //    {
            //        Id = "419decbe-6af1-4d84-9b45-c1ef796f4607",
            //        UserName = "andreipostavaru@test.com",
            //        NormalizedUserName = "ANDREIPOSTAVARU@GMAIL.COM",
            //        Email = "andreipostavaru@test.com",
            //        NormalizedEmail = "ANDREIPOSTAVARU@GMAIL.COM",
            //        EmailConfirmed = true,
            //        PasswordHash = hasher.HashPassword(new ApplicationUser(), "VandGolf_6_!"),
            //        FirstName = "Andrei",
            //        LastName = "Postavaru",
            //        AutoSchoolId = 1
            //    });
            //context.UserRoles.AddRange(
            //        ///
            //        new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4604", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
            //        new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4605", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
            //        new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4606", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
            //        //
            //        new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4607", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3" });

            //context.Vehicles.AddRange(
            //    new Vehicle
            //    {
            //        VehicleId = 2,
            //        LicensePlateNumber = "B-66-ROM",
            //        TransmissionType = TransmissionType.MANUAL,
            //        Color = "Black",
            //        Brand = "Suzuki",
            //        Model = "Hayabusa",
            //        YearOfProduction = 2021,
            //        FuelType = TipCombustibil.MOTORINA,
            //        EngineSizeLiters = 8.7m,
            //        PowertrainType = TipPropulsie.COMBUSTIBIL,
            //        LicenseId = 2,
            //        AutoSchoolId = 1
            //    },
            //    new Vehicle
            //    {
            //        VehicleId = 3,
            //        LicensePlateNumber = "B-252-AFR",
            //        TransmissionType = TransmissionType.MANUAL,
            //        Color = "White",
            //        Brand = "Opel",
            //        Model = "Astra",
            //        YearOfProduction = 2018,
            //        FuelType = TipCombustibil.BENZINA,
            //        EngineSizeLiters = 40.0m,
            //        PowertrainType = TipPropulsie.COMBUSTIBIL,
            //        LicenseId = 1,
            //        AutoSchoolId = 1
            //    },
            //    new Vehicle
            //    {
            //        VehicleId = 4,
            //        LicensePlateNumber = "B-989-KZE",
            //        TransmissionType = TransmissionType.MANUAL,
            //        Color = "Red",
            //        Brand = "Ford",
            //        Model = "Focus",
            //        YearOfProduction = 2002,
            //        FuelType = TipCombustibil.MOTORINA,
            //        EngineSizeLiters = 35.0m,
            //        PowertrainType = TipPropulsie.COMBUSTIBIL,
            //        LicenseId = 1,
            //        AutoSchoolId = 1
            //    });
            //context.Files.AddRange(
            //      new File
            //      {
            //          FileId = 2,
            //          ScholarshipStartDate = DateTime.Today,
            //          CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
            //          MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
            //          Status = FileStatus.APPROVED,
            //          StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4604",
            //          InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4607",
            //          TeachingCategoryId = 1,
            //          VehicleId = 1
            //      },
            //        new File
            //        {
            //            FileId = 3,
            //            ScholarshipStartDate = DateTime.Today.AddMonths(-5),
            //            CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
            //            MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
            //            Status = FileStatus.APPROVED,
            //            StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4605",
            //            InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4607",
            //            TeachingCategoryId = 1,
            //            VehicleId = 3
            //        },
            //        new File
            //        {
            //            FileId = 4,
            //            ScholarshipStartDate = DateTime.Today,
            //            CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
            //            MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
            //            Status = FileStatus.APPROVED,
            //            StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4606",
            //            InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4607",
            //            TeachingCategoryId = 2,
            //            VehicleId = 2
            //        },
            //        new File
            //        {
            //            FileId = 5,
            //            ScholarshipStartDate = DateTime.Today,
            //            CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
            //            MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
            //            Status = FileStatus.APPROVED,
            //            StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4602",
            //            InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4607",
            //            TeachingCategoryId = 1,
            //            VehicleId = 1
            //        });
            //context.Appointments.AddRange(
            //      new Appointment
            //      {
            //          AppointmentId = 2,
            //          Date = DateTime.Today.AddDays(1),
            //          StartHour = new TimeSpan(11, 0, 0),
            //          EndHour = new TimeSpan(12, 30, 0),
            //          FileId = 2
            //      },
            //        new Appointment
            //        {
            //            AppointmentId = 3,
            //            Date = DateTime.Today.AddDays(2 - 2 * 30),
            //            StartHour = new TimeSpan(9, 0, 0),
            //            EndHour = new TimeSpan(10, 30, 0),
            //            FileId = 3
            //        },
            //        new Appointment
            //        {
            //            AppointmentId = 4,
            //            Date = DateTime.Today.AddDays(2 - 30),
            //            StartHour = new TimeSpan(11, 0, 0),
            //            EndHour = new TimeSpan(12, 30, 0),
            //            FileId = 3
            //        },
            //        new Appointment
            //        {
            //            AppointmentId = 5,
            //            Date = DateTime.Today.AddDays(3),
            //            StartHour = new TimeSpan(14, 0, 0),
            //            EndHour = new TimeSpan(15, 30, 0),
            //            FileId = 3
            //        },
            //        new Appointment
            //        {
            //            AppointmentId = 6,
            //            Date = DateTime.Today.AddDays(4),
            //            StartHour = new TimeSpan(16, 0, 0),
            //            EndHour = new TimeSpan(17, 30, 0),
            //            FileId = 4
            //        },
            //        new Appointment
            //        {
            //            AppointmentId = 7,
            //            Date = DateTime.Today.AddDays(4),
            //            StartHour = new TimeSpan(16, 0, 0),
            //            EndHour = new TimeSpan(17, 30, 0),
            //            FileId = 4
            //        });
            context.SaveChanges();
        }



        // ──────────────── Geography (County, City, Address) ────────────────
        if (!context.Counties.Any())
        {
            context.Counties.AddRange(
                new County { CountyId = 1, Name = "Cluj", Abbreviation = "CJ" },
                new County { CountyId = 2, Name = "Bucuresti", Abbreviation = "B" },
                new County { CountyId = 3, Name = "Timis", Abbreviation = "TM" },
                new County { CountyId = 4, Name = "Iasi", Abbreviation = "IS" },
                new County { CountyId = 5, Name = "Constanta", Abbreviation = "CT" },
                new County { CountyId = 6, Name = "Brasov", Abbreviation = "BV" },
                new County { CountyId = 7, Name = "Sibiu", Abbreviation = "SB" },
                new County { CountyId = 8, Name = "Bihor", Abbreviation = "BH" },
                new County { CountyId = 9, Name = "Dolj", Abbreviation = "DJ" },
                new County { CountyId = 10, Name = "Galati", Abbreviation = "GL" }
            );
            context.SaveChanges();
        }

        if (!context.Cities.Any())
        {
            context.Cities.AddRange(
                new City { CityId = 1, Name = "Cluj-Napoca", CountyId = 1 },
                new City { CityId = 2, Name = "Bucuresti", CountyId = 2 },
                new City { CityId = 3, Name = "Timisoara", CountyId = 3 },
                new City { CityId = 4, Name = "Iasi", CountyId = 4 },
                new City { CityId = 5, Name = "Constanta", CountyId = 5 },
                new City { CityId = 6, Name = "Brasov", CountyId = 6 },
                new City { CityId = 7, Name = "Sibiu", CountyId = 7 },
                new City { CityId = 8, Name = "Oradea", CountyId = 8 },
                new City { CityId = 9, Name = "Craiova", CountyId = 9 },
                new City { CityId = 10, Name = "Galati", CountyId = 10 }
            );
            context.SaveChanges();
        }

        if (!context.Addresses.Any())
        {
            context.Addresses.AddRange(
                new Address { AddressId = 1, StreetName = "Strada Aviatorilor", AddressNumber = "10", Postcode = "400001", CityId = 1 },
                new Address { AddressId = 2, StreetName = "Bulevardul Unirii", AddressNumber = "12", Postcode = "010001", CityId = 2 },
                new Address { AddressId = 3, StreetName = "Strada Take Ionescu", AddressNumber = "5", Postcode = "300001", CityId = 3 },
                new Address { AddressId = 4, StreetName = "Strada Palat", AddressNumber = "18", Postcode = "700001", CityId = 4 },
                new Address { AddressId = 5, StreetName = "Bulevardul Tomis", AddressNumber = "77", Postcode = "900001", CityId = 5 },
                new Address { AddressId = 6, StreetName = "Strada Republicii", AddressNumber = "22", Postcode = "500001", CityId = 6 },
                new Address { AddressId = 7, StreetName = "Strada Nicolae Balcescu", AddressNumber = "9", Postcode = "550001", CityId = 7 },
                new Address { AddressId = 8, StreetName = "Strada Independentei", AddressNumber = "31", Postcode = "410001", CityId = 8 },
                new Address { AddressId = 9, StreetName = "Calea Bucuresti", AddressNumber = "50", Postcode = "200001", CityId = 9 },
                new Address { AddressId = 10, StreetName = "Strada Brailei", AddressNumber = "14", Postcode = "800001", CityId = 10 },
                new Address { AddressId = 11, StreetName = "Strada Memorandumului", AddressNumber = "7", Postcode = "400002", CityId = 1 },
                new Address { AddressId = 12, StreetName = "Strada Eminescu", AddressNumber = "3", Postcode = "300002", CityId = 3 }
            );
            context.SaveChanges();
        }

        // ──────────────── AutoSchool ────────────────
        if (!context.AutoSchools.Any())
        {
            context.AutoSchools.AddRange(
                new AutoSchool
                {
                    AutoSchoolId = 1,
                    Name = "DriveFlow Academy Cluj",
                    Description = "Full-service driving school in Cluj.",
                    PhoneNumber = "0740000001",
                    Email = "cluj@driveflow.test",
                    WebSite = "https://cluj.driveflow.test",
                    Status = AutoSchoolStatus.Active,
                    AddressId = 1
                },
                new AutoSchool
                {
                    AutoSchoolId = 2,
                    Name = "DriveFlow Academy Bucuresti",
                    Description = "Central Bucharest training center.",
                    PhoneNumber = "0740000002",
                    Email = "bucuresti@driveflow.test",
                    WebSite = "https://bucuresti.driveflow.test",
                    Status = AutoSchoolStatus.Active,
                    AddressId = 2
                },
                new AutoSchool
                {
                    AutoSchoolId = 3,
                    Name = "DriveFlow Academy Timisoara",
                    Description = "Timisoara branch for professional drivers.",
                    PhoneNumber = "0740000003",
                    Email = "timisoara@driveflow.test",
                    WebSite = "https://timisoara.driveflow.test",
                    Status = AutoSchoolStatus.Active,
                    AddressId = 3
                },
                new AutoSchool
                {
                    AutoSchoolId = 4,
                    Name = "DriveFlow Academy Iasi",
                    Description = "Iasi branch with modern training fleet.",
                    PhoneNumber = "0740000004",
                    Email = "iasi@driveflow.test",
                    WebSite = "https://iasi.driveflow.test",
                    Status = AutoSchoolStatus.Active,
                    AddressId = 4
                }
            );
            context.SaveChanges();
        }

        // ──────────────── License ────────────────
        if (!context.Licenses.Any())
        {
            var licenses = new List<License>();
            for (var i = 0; i < licenseTypes.Length; i++)
            {
                licenses.Add(new License
                {
                    LicenseId = i + 1,
                    Type = licenseTypes[i]
                });
            }
            context.Licenses.AddRange(licenses);
            context.SaveChanges();
        }

        licenseIdByType = context.Licenses
            .AsNoTracking()
            .ToDictionary(l => l.Type, l => l.LicenseId);

        // ──────────────── TeachingCategory ────────────────
        if (!context.TeachingCategories.Any())
        {
            var teachingCategories = new List<TeachingCategory>();
            var teachingCategoryId = 1;

            foreach (var schoolId in schoolIds)
            {
                var codes = schoolId == 1
                    ? new[] { "B", "A", "C", "CE", "D" }
                    : new[] { "B", "BE", "C", "CE", "D" };

                foreach (var code in codes)
                {
                    if (!licenseIdByType.TryGetValue(code, out var licenseId))
                    {
                        continue;
                    }

                    teachingCategories.Add(new TeachingCategory
                    {
                        TeachingCategoryId = teachingCategoryId,
                        Code = code,
                        SessionCost = 150 + (teachingCategoryId % 3) * 20,
                        SessionDuration = 90,
                        ScholarshipPrice = 2500 + (teachingCategoryId % 4) * 250,
                        MinDrivingLessonsReq = 30,
                        LicenseId = licenseId,
                        AutoSchoolId = schoolId
                    });
                    teachingCategoryId++;
                }
            }

            context.TeachingCategories.AddRange(teachingCategories);
            context.SaveChanges();
        }

        teachingCategoryIdsBySchool = context.TeachingCategories
            .AsNoTracking()
            .GroupBy(tc => tc.AutoSchoolId)
            .ToDictionary(g => g.Key, g => g.Select(tc => tc.TeachingCategoryId).OrderBy(id => id).ToList());

        // ──────────────── ExamForm ────────────────
        if (!context.ExamForms.Any())
        {
            context.ExamForms.AddRange(
                new ExamForm
                {
                    FormId = 1,
                    TeachingCategoryId = 1,
                    MaxPoints = 21
                },
                new ExamForm
                {
                    FormId = 3,
                    TeachingCategoryId = 2,
                    MaxPoints = 21
                },
                new ExamForm
                {
                    FormId = 4,
                    TeachingCategoryId = 3,
                    MaxPoints = 21
                }
            );
            context.SaveChanges();
        }

        // ──────────────── Users ────────────────
        if (!context.Users.Any())
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            var users = new List<ApplicationUser>();
            var userRoles = new List<IdentityUserRole<string>>();

            var superAdminId = "superadmin-0001";
            var superAdminEmail = "superadmin@driveflow.test";
            users.Add(new ApplicationUser
            {
                Id = superAdminId,
                UserName = superAdminEmail,
                NormalizedUserName = superAdminEmail.ToUpperInvariant(),
                Email = superAdminEmail,
                NormalizedEmail = superAdminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(new ApplicationUser(), superAdminPassword),
                FirstName = "Super",
                LastName = "Admin"
            });
            userRoles.Add(new IdentityUserRole<string> { UserId = superAdminId, RoleId = roleSuperAdminId });

            long cnpBase = 1000000000000L;
            var cnpOffset = 1;

            foreach (var schoolId in schoolIds)
            {
                var schoolAdminId = $"schooladmin-{schoolId:00}";
                schoolAdminIdsBySchool[schoolId] = schoolAdminId;
                var schoolAdminEmail = $"schooladmin{schoolId}@driveflow.test";
                users.Add(new ApplicationUser
                {
                    Id = schoolAdminId,
                    UserName = schoolAdminEmail,
                    NormalizedUserName = schoolAdminEmail.ToUpperInvariant(),
                    Email = schoolAdminEmail,
                    NormalizedEmail = schoolAdminEmail.ToUpperInvariant(),
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(new ApplicationUser(), schoolAdminPassword),
                    FirstName = "School",
                    LastName = $"Admin {schoolId}",
                    AutoSchoolId = schoolId
                });
                userRoles.Add(new IdentityUserRole<string> { UserId = schoolAdminId, RoleId = roleSchoolAdminId });

                var instructorIds = new List<string>();
                for (var i = 1; i <= 3; i++)
                {
                    var instructorId = $"instructor-{schoolId:00}-{i:00}";
                    instructorIds.Add(instructorId);
                    var instructorEmail = $"instructor{schoolId}{i}@driveflow.test";
                    users.Add(new ApplicationUser
                    {
                        Id = instructorId,
                        UserName = instructorEmail,
                        NormalizedUserName = instructorEmail.ToUpperInvariant(),
                        Email = instructorEmail,
                        NormalizedEmail = instructorEmail.ToUpperInvariant(),
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), instructorPassword),
                        FirstName = "Instructor",
                        LastName = $"S{schoolId} {i}",
                        AutoSchoolId = schoolId
                    });
                    userRoles.Add(new IdentityUserRole<string> { UserId = instructorId, RoleId = roleInstructorId });
                }
                instructorIdsBySchool[schoolId] = instructorIds;

                var studentIds = new List<string>();
                for (var i = 1; i <= 10; i++)
                {
                    var studentId = $"student-{schoolId:00}-{i:00}";
                    studentIds.Add(studentId);
                    var studentEmail = $"student{schoolId}{i}@driveflow.test";
                    var cnp = (cnpBase + cnpOffset).ToString();
                    cnpOffset++;
                    users.Add(new ApplicationUser
                    {
                        Id = studentId,
                        UserName = studentEmail,
                        NormalizedUserName = studentEmail.ToUpperInvariant(),
                        Email = studentEmail,
                        NormalizedEmail = studentEmail.ToUpperInvariant(),
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), studentPassword),
                        FirstName = "Student",
                        LastName = $"S{schoolId} {i}",
                        Cnp = cnp,
                        AutoSchoolId = schoolId
                    });
                    userRoles.Add(new IdentityUserRole<string> { UserId = studentId, RoleId = roleStudentId });
                }
                studentIdsBySchool[schoolId] = studentIds;
            }

            context.Users.AddRange(users);
            context.SaveChanges();
            context.UserRoles.AddRange(userRoles);
            context.SaveChanges();
        }

        // ──────────────── ApplicationUserTeachingCategory ────────────────
        if (!context.ApplicationUserTeachingCategories.Any())
        {
            var assignments = new List<ApplicationUserTeachingCategory>();
            var assignmentId = 1;

            foreach (var schoolId in schoolIds)
            {
                if (!teachingCategoryIdsBySchool.TryGetValue(schoolId, out var categoryIds) || categoryIds.Count == 0)
                {
                    continue;
                }

                var primaryCategoryId = categoryIds[0];
                var secondaryCategoryId = categoryIds.Count > 1 ? categoryIds[1] : categoryIds[0];

                if (instructorIdsBySchool.TryGetValue(schoolId, out var instructorIds))
                {
                    foreach (var instructorId in instructorIds)
                    {
                        assignments.Add(new ApplicationUserTeachingCategory
                        {
                            ApplicationUserTeachingCategoryId = assignmentId++,
                            UserId = instructorId,
                            TeachingCategoryId = primaryCategoryId
                        });
                        assignments.Add(new ApplicationUserTeachingCategory
                        {
                            ApplicationUserTeachingCategoryId = assignmentId++,
                            UserId = instructorId,
                            TeachingCategoryId = secondaryCategoryId
                        });
                    }
                }

                if (studentIdsBySchool.TryGetValue(schoolId, out var studentIds))
                {
                    foreach (var studentId in studentIds)
                    {
                        assignments.Add(new ApplicationUserTeachingCategory
                        {
                            ApplicationUserTeachingCategoryId = assignmentId++,
                            UserId = studentId,
                            TeachingCategoryId = primaryCategoryId
                        });
                    }
                }
            }

            context.ApplicationUserTeachingCategories.AddRange(assignments);
            context.SaveChanges();
        }

        // ──────────────── ExamItems ────────────────
        if (!context.ExamItems.Any())
        {
            context.ExamItems.AddRange(
                ////////////// categ B traseu //////////////
                new ExamItem
                {
                    ItemId = 1,
                    FormId = 1,
                    Description = "Neverificarea, prin intermediul aparaturii de bord sau al comenzilor autovehiculului, a funcţionării direcţiei, frânei, a instalaţiei de ungere/răcire, a luminilor, a semnalizării, a avertizorului sonor",
                    PenaltyPoints = 3,
                    OrderIndex = 1
                },
                new ExamItem
                {
                    ItemId = 2,
                    FormId = 1,
                    Description = "Neverificarea dispozitivului de cuplare şi conexiunilor instalaţiei de frânare/electrice a catadioptrilor (numai BE)",
                    PenaltyPoints = 9,
                    OrderIndex = 2
                },
                new ExamItem
                {
                    ItemId = 3,
                    FormId = 1,
                    Description = "Neverificarea elementelor de siguranţă legate de încărcătura vehiculului, fixare, închidere uşi/obloane (numai BE)",
                    PenaltyPoints = 9,
                    OrderIndex = 3
                },
                new ExamItem
                {
                    ItemId = 4,
                    FormId = 1,
                    Description = "Nereglarea scaunului, a oglinzilor retrovizoare, nefixarea centurii de siguranţă, neeliberarea frânei de ajutor",
                    PenaltyPoints = 3,
                    OrderIndex = 4
                },
                new ExamItem
                {
                    ItemId = 5,
                    FormId = 1,
                    Description = "Necunoaşterea aparaturii de bord sau a comenzilor autovehiculului",
                    PenaltyPoints = 3,
                    OrderIndex = 5
                },
                new ExamItem
                {
                    ItemId = 6,
                    FormId = 1,
                    Description = "Nesincronizarea comenzilor (oprirea motorului, accelerarea excesivă, folosirea incorectă a treptelor de viteză)",
                    PenaltyPoints = 5,
                    OrderIndex = 6
                },
                new ExamItem
                {
                    ItemId = 7,
                    FormId = 1,
                    Description = "Nemenţinerea direcţiei de mers",
                    PenaltyPoints = 9,
                    OrderIndex = 7
                },
                new ExamItem
                {
                    ItemId = 8,
                    FormId = 1,
                    Description = "Folosirea incorectă a drumului cu sau fără marcaj",
                    PenaltyPoints = 6,
                    OrderIndex = 8
                },
                new ExamItem
                {
                    ItemId = 9,
                    FormId = 1,
                    Description = "Manevrarea incorectă la încrucişarea cu alte vehicule, inclusiv în spaţii restrânse",
                    PenaltyPoints = 6,
                    OrderIndex = 9
                },
                new ExamItem
                {
                    ItemId = 10,
                    FormId = 1,
                    Description = "Întoarcerea incorectă pe o stradă cu mai multe benzi de circulaţie pe sens",
                    PenaltyPoints = 5,
                    OrderIndex = 10
                },
                new ExamItem
                {
                    ItemId = 11,
                    FormId = 1,
                    Description = "Manevrarea incorectă la urcarea rampelor/coborrea pantelor lungi, la circulaţia în tuneluri",
                    PenaltyPoints = 5,
                    OrderIndex = 11
                },
                new ExamItem
                {
                    ItemId = 12,
                    FormId = 1,
                    Description = "Folosirea incorectă a luminilor de întâlnire/luminilor de drum",
                    PenaltyPoints = 3,
                    OrderIndex = 12
                },
                new ExamItem
                {
                    ItemId = 13,
                    FormId = 1,
                    Description = "Conducerea în mod neeconomic şi agresiv pentru mediul înconjurător (turaţie excesivă, frânare/accelerare nejustificate)",
                    PenaltyPoints = 5,
                    OrderIndex = 13
                },
                new ExamItem
                {
                    ItemId = 14,
                    FormId = 1,
                    Description = "Executarea incorectă a mersului înapoi",
                    PenaltyPoints = 5,
                    OrderIndex = 14
                },
                new ExamItem
                {
                    ItemId = 15,
                    FormId = 1,
                    Description = "Executarea incorectă a întoarcerii vehiculului cu faţa în sens opus prin efectuarea manevrelor de mers înainte şi înapoi",
                    PenaltyPoints = 5,
                    OrderIndex = 15
                },
                new ExamItem
                {
                    ItemId = 16,
                    FormId = 1,
                    Description = "Executarea incorectă a parcării cu faţa, spatele sau lateral",
                    PenaltyPoints = 5,
                    OrderIndex = 16
                },
                new ExamItem
                {
                    ItemId = 17,
                    FormId = 1,
                    Description = "Executarea incorectă a frânării cu precizie",
                    PenaltyPoints = 5,
                    OrderIndex = 17
                },
                new ExamItem
                {
                    ItemId = 18,
                    FormId = 1,
                    Description = "Executarea incorectă a cuplării/decuplării remorcii la/de la autovehiculul trăgător (numai BE)",
                    PenaltyPoints = 5,
                    OrderIndex = 18
                },
                new ExamItem
                {
                    ItemId = 19,
                    FormId = 1,
                    Description = "Neasigurarea la schimbarea direcţiei de mers/la părăsirea locului de staţionare",
                    PenaltyPoints = 9,
                    OrderIndex = 19
                },
                new ExamItem
                {
                    ItemId = 20,
                    FormId = 1,
                    Description = "Executarea neregulamentară a virajelor",
                    PenaltyPoints = 6,
                    OrderIndex = 20
                },
                new ExamItem
                {
                    ItemId = 21,
                    FormId = 1,
                    Description = "Nesemnalizarea sau semnalizarea greşită a schimbării direcţiei de mers",
                    PenaltyPoints = 6,
                    OrderIndex = 21
                },
                new ExamItem
                {
                    ItemId = 22,
                    FormId = 1,
                    Description = "Încadrarea necorespunzătoare în raport cu direcţia de mers indicată",
                    PenaltyPoints = 6,
                    OrderIndex = 22
                },
                new ExamItem
                {
                    ItemId = 23,
                    FormId = 1,
                    Description = "Efectuarea unor manevre interzise (oprire, staţionare, întoarcere, mers înapoi)",
                    PenaltyPoints = 6,
                    OrderIndex = 23
                },
                new ExamItem
                {
                    ItemId = 24,
                    FormId = 1,
                    Description = "Neasigurarea la pătrunderea în intersecţii",
                    PenaltyPoints = 9,
                    OrderIndex = 24
                },
                new ExamItem
                {
                    ItemId = 25,
                    FormId = 1,
                    Description = "Folosirea incorectă a benzilor la intrarea/ieşirea pe/de pe autostradă/artere similare",
                    PenaltyPoints = 5,
                    OrderIndex = 25
                },
                new ExamItem
                {
                    ItemId = 26,
                    FormId = 1,
                    Description = "Nepăstrarea distanţei suficiente faţă de cei care rulează înainte sau vin din sens opus",
                    PenaltyPoints = 9,
                    OrderIndex = 26
                },
                new ExamItem
                {
                    ItemId = 27,
                    FormId = 1,
                    Description = "Ezitarea repetată de a depăşi alte vehicule",
                    PenaltyPoints = 3,
                    OrderIndex = 27
                },
                new ExamItem
                {
                    ItemId = 28,
                    FormId = 1,
                    Description = "Nerespectarea regulilor de executare a depăşirii ori efectuarea acestora în locuri şi situaţii interzise",
                    PenaltyPoints = 21,
                    OrderIndex = 28
                },
                new ExamItem
                {
                    ItemId = 29,
                    FormId = 1,
                    Description = "Neacordarea priorităţii vehiculelor şi pietonilor care au acest drept (la plecarea de pe loc, în intersecţii, sens giratoriu, staţie de mijloc de transport în comun prevăzută cu alveolă, staţie de tramvai fără refugiu pentru pietoni, trecere de pietoni)",
                    PenaltyPoints = 21,
                    OrderIndex = 29
                },
                new ExamItem
                {
                    ItemId = 30,
                    FormId = 1,
                    Description = "Tendinţe repetate de a ceda trecerea vehiculelor şi pietonilor care nu au prioritate",
                    PenaltyPoints = 6,
                    OrderIndex = 30
                },
                new ExamItem
                {
                    ItemId = 31,
                    FormId = 1,
                    Description = "Nerespectarea semnificaţiei indicatoarelor/marcajelor/culorilor semaforului (cu excepţia culorii roşii)",
                    PenaltyPoints = 9,
                    OrderIndex = 31
                },
                new ExamItem
                {
                    ItemId = 32,
                    FormId = 1,
                    Description = "Nerespectarea semnificaţiei culorii roşii a semaforului/a semnalelor poliţistului rutier/a semnalelor altor persoane cu atribuţii legale similare",
                    PenaltyPoints = 21,
                    OrderIndex = 32
                },
                new ExamItem
                {
                    ItemId = 33,
                    FormId = 1,
                    Description = "Depăşirea vitezei maxime admise",
                    PenaltyPoints = 5,
                    OrderIndex = 33
                },
                new ExamItem
                {
                    ItemId = 34,
                    FormId = 1,
                    Description = "Conducerea cu viteză redusă în mod nejustificat, neîncadrarea în ritmul impus de ceilalţi participanţi la trafic",
                    PenaltyPoints = 3,
                    OrderIndex = 34
                },
                new ExamItem
                {
                    ItemId = 35,
                    FormId = 1,
                    Description = "Neîndemânarea în conducerea în condiţii de ploaie, zăpadă, mâzgă, polei",
                    PenaltyPoints = 9,
                    OrderIndex = 35
                },
                new ExamItem
                {
                    ItemId = 36,
                    FormId = 1,
                    Description = "Deplasarea cu viteză neadaptată condiţiilor atmosferice şi de drum",
                    PenaltyPoints = 9,
                    OrderIndex = 36
                },
                new ExamItem
                {
                    ItemId = 37,
                    FormId = 1,
                    Description = "Prezentarea la examen sub influenţa băuturilor alcoolice, substanţelor sau produselor stupefiante, a medicamentelor cu efecte similare acestora sau manifestări de natură să perturbe examinarea celorlalţi candidaţi",
                    PenaltyPoints = 21,
                    OrderIndex = 37
                },
                new ExamItem
                {
                    ItemId = 38,
                    FormId = 1,
                    Description = "Intervenţia examinatorului pentru evitarea unui pericol iminent/producerea unui eveniment rutier",
                    PenaltyPoints = 21,
                    OrderIndex = 38
                },
                /*
                ////////////// categ A poligon //////////////

                new ExamItem
                {
                    ItemId = 39,
                    FormId = 2,
                    Description = "Neutilizarea echipamentului de protecţie: mănuşi, cizme, îmbrăcăminte şi casca de protecţie (pentru AM, numai casca de protecţie)",
                    PenaltyPoints = 3,
                    OrderIndex = 1
                },
                new ExamItem
                {
                    ItemId = 40,
                    FormId = 2,
                    Description = "Neverificarea stării anvelopelor/a comutatorului de oprire în caz de urgenţă",
                    PenaltyPoints = 3,
                    OrderIndex = 2
                },
                new ExamItem
                {
                    ItemId = 41,
                    FormId = 2,
                    Description = "Verificarea, prin intermediul aparaturii de bord sau comenzilor autovehiculului, a funcţionalităţii direcţiei, frânei, transmisiei, a instalaţiei de ungere/răcire, a luminilor, a semnalizării, a catadioptrilor, a avertizorului sonor",
                    PenaltyPoints = 3,
                    OrderIndex = 3
                },
                new ExamItem
                {
                    ItemId = 42,
                    FormId = 2,
                    Description = "Aşezarea/coborârea vehiculului pe/de pe suportul de sprijin/cric şi deplasarea pe jos, pe lângă vehicul",
                    PenaltyPoints = 3,
                    OrderIndex = 4
                },
                new ExamItem
                {
                    ItemId = 43,
                    FormId = 2,
                    Description = "Pornirea motorului şi demararea uşoară, fără bruscarea vehiculului",
                    PenaltyPoints = 3,
                    OrderIndex = 5
                },
                new ExamItem
                {
                    ItemId = 44,
                    FormId = 2,
                    Description = "Accelerarea progresivă, menţinerea direcţiei de mers, inclusiv la schimbarea vitezelor",
                    PenaltyPoints = 5,
                    OrderIndex = 6
                },
                new ExamItem
                {
                    ItemId = 45,
                    FormId = 2,
                    Description = "Menţinerea poziţiei pe vehicul, tehnica menţinerii direcţiei (echilibrul permanent fără sprijinirea de carosabil)",
                    PenaltyPoints = 5,
                    OrderIndex = 7
                },
                new ExamItem
                {
                    ItemId = 46,
                    FormId = 2,
                    Description = "Manevrarea ambreiajului în combinaţie cu frâna, schimbarea vitezelor",
                    PenaltyPoints = 3,
                    OrderIndex = 8
                },
                new ExamItem
                {
                    ItemId = 47,
                    FormId = 2,
                    Description = "Executarea slalomului printre 5 jaloane",
                    PenaltyPoints = 5,
                    OrderIndex = 9
                },
                new ExamItem
                {
                    ItemId = 48,
                    FormId = 2,
                    Description = "Executarea de opturi printre 4 jaloane",
                    PenaltyPoints = 5,
                    OrderIndex = 10
                },
                new ExamItem
                {
                    ItemId = 49,
                    FormId = 2,
                    Description = "Ocolirea jalonului, fără lovirea/răsturnarea acestuia",
                    PenaltyPoints = 3,
                    OrderIndex = 11
                },
                new ExamItem
                {
                    ItemId = 50,
                    FormId = 2,
                    Description = "Îndemânarea privind manevrarea frânei faţă/spate la frânarea de urgenţă (menţinerea direcţiei vizuale, poziţia pe vehicul)",
                    PenaltyPoints = 5,
                    OrderIndex = 12
                },
                new ExamItem
                {
                    ItemId = 51,
                    FormId = 2,
                    Description = "Executarea manevrei de evitare a unui obstacol la viteză de peste 30 km/h",
                    PenaltyPoints = 5,
                    OrderIndex = 13
                },
                new ExamItem
                {
                    ItemId = 52,
                    FormId = 2,
                    Description = "Executarea manevrei de evitare a unui obstacol la o viteză minimă de 50 km/h",
                    PenaltyPoints = 5,
                    OrderIndex = 14
                },
                new ExamItem
                {
                    ItemId = 53,
                    FormId = 2,
                    Description = "Executarea frânării, inclusiv frânarea de urgenţă, la o viteză minimă de 50 km/h",
                    PenaltyPoints = 5,
                    OrderIndex = 15
                },
                new ExamItem
                {
                    ItemId = 54,
                    FormId = 2,
                    Description = "A depăşit timpul alocat executării manevrelor în poligon",
                    PenaltyPoints = 16,
                    OrderIndex = 16
                },
                new ExamItem
                {
                    ItemId = 55,
                    FormId = 2,
                    Description = "A căzut cu mopedul/motocicleta",
                    PenaltyPoints = 16,
                    OrderIndex = 17
                },
                new ExamItem
                {
                    ItemId = 56,
                    FormId = 2,
                    Description = "Nu a respectat traseul stabilit în poligon",
                    PenaltyPoints = 16,
                    OrderIndex = 18
                },*/
                ////////////// categ A traseu //////////////
                new ExamItem
                {
                    ItemId = 57,
                    FormId = 3,
                    Description = "Nesincronizarea comenzilor (oprirea motorului, accelerarea excesivă, folosirea incorectă a treptelor de viteză)",
                    PenaltyPoints = 6,
                    OrderIndex = 1
                },
                new ExamItem
                {
                    ItemId = 58,
                    FormId = 3,
                    Description = "Nemenţinerea direcţiei de mers",
                    PenaltyPoints = 9,
                    OrderIndex = 2
                },
                new ExamItem
                {
                    ItemId = 59,
                    FormId = 3,
                    Description = "Folosirea incorectă a drumului cu sau fără marcaj",
                    PenaltyPoints = 6,
                    OrderIndex = 3
                },
                new ExamItem
                {
                    ItemId = 60,
                    FormId = 3,
                    Description = "Manevrarea incorectă la încrucişarea cu alte vehicule, inclusiv în spaţii restrânse",
                    PenaltyPoints = 6,
                    OrderIndex = 4
                },
                new ExamItem
                {
                    ItemId = 61,
                    FormId = 3,
                    Description = "Neasigurarea la schimbarea direcţiei de mers",
                    PenaltyPoints = 9,
                    OrderIndex = 5
                },
                new ExamItem
                {
                    ItemId = 62,
                    FormId = 3,
                    Description = "Executarea neregulamentară a virajelor",
                    PenaltyPoints = 6,
                    OrderIndex = 6
                },
                new ExamItem
                {
                    ItemId = 63,
                    FormId = 3,
                    Description = "Nesemnalizarea sau semnalizarea greşită a schimbării direcţiei de mers",
                    PenaltyPoints = 6,
                    OrderIndex = 7
                },
                new ExamItem
                {
                    ItemId = 64,
                    FormId = 3,
                    Description = "Folosirea incorectă a luminilor de întâlnire/luminilor de drum",
                    PenaltyPoints = 3,
                    OrderIndex = 8
                },
                new ExamItem
                {
                    ItemId = 65,
                    FormId = 3,
                    Description = "Neîncadrarea corespunzătoare în raport cu direcţia de mers indicată",
                    PenaltyPoints = 6,
                    OrderIndex = 9
                },
                new ExamItem
                {
                    ItemId = 66,
                    FormId = 3,
                    Description = "Efectuarea unor manevre interzise (oprire, staţionare, întoarcere)",
                    PenaltyPoints = 6,
                    OrderIndex = 10
                },
                new ExamItem
                {
                    ItemId = 67,
                    FormId = 3,
                    Description = "Neasigurarea la pătrunderea în intersecţii/la părăsirea zonei de staţionare",
                    PenaltyPoints = 9,
                    OrderIndex = 11
                },
                new ExamItem
                {
                    ItemId = 68,
                    FormId = 3,
                    Description = "Folosirea incorectă a benzilor la intrarea/ieşirea pe/de pe autostradă/artere similare",
                    PenaltyPoints = 5,
                    OrderIndex = 12
                },
                new ExamItem
                {
                    ItemId = 69,
                    FormId = 3,
                    Description = "Nepăstrarea distanţei suficiente faţă de cei care rulează înainte sau vin din sens opus",
                    PenaltyPoints = 9,
                    OrderIndex = 13
                },
                new ExamItem
                {
                    ItemId = 70,
                    FormId = 3,
                    Description = "Conducerea în mod neeconomic şi agresiv pentru mediul înconjurător (turaţie excesivă, frânare/accelerare nejustificate)",
                    PenaltyPoints = 5,
                    OrderIndex = 14
                },
                new ExamItem
                {
                    ItemId = 71,
                    FormId = 3,
                    Description = "Manevrarea incorectă la urcarea rampelor/coborârea pantelor lungi, la circulaţia în tuneluri",
                    PenaltyPoints = 5,
                    OrderIndex = 15
                },
                new ExamItem
                {
                    ItemId = 72,
                    FormId = 3,
                    Description = "Nerespectarea normelor legale referitoare la manevra de depăşire",
                    PenaltyPoints = 21,
                    OrderIndex = 16
                },
                new ExamItem
                {
                    ItemId = 73,
                    FormId = 3,
                    Description = "Ezitarea repetată de a depăşi alte vehicule",
                    PenaltyPoints = 6,
                    OrderIndex = 17
                },
                new ExamItem
                {
                    ItemId = 74,
                    FormId = 3,
                    Description = "Neacordarea priorităţii de trecere vehiculelor şi pietonilor care au acest drept (la plecarea de pe loc, în intersecţii, sens giratoriu, staţie mijloc de transport în comun prevăzută cu alveolă, staţie de tramvai fără refugiu pentru pietoni, trecere de pietoni)",
                    PenaltyPoints = 21,
                    OrderIndex = 18
                },
                new ExamItem
                {
                    ItemId = 75,
                    FormId = 3,
                    Description = "Nerespectarea semnificaţiei culorii roşii a semaforului/a semnalelor poliţistului rutier/a semnalelor altor persoane cu atribuţii legale similare",
                    PenaltyPoints = 21,
                    OrderIndex = 19
                },
                new ExamItem
                {
                    ItemId = 76,
                    FormId = 3,
                    Description = "Nerespectarea semnificaţiei indicatoarelor/marcajelor/culorii semaforului (cu excepţia culorii roşii)",
                    PenaltyPoints = 9,
                    OrderIndex = 20
                },
                new ExamItem
                {
                    ItemId = 77,
                    FormId = 3,
                    Description = "Nerespectarea normelor legale referitoare la trecerea la nivel cu calea ferată",
                    PenaltyPoints = 21,
                    OrderIndex = 21
                },
                new ExamItem
                {
                    ItemId = 78,
                    FormId = 3,
                    Description = "Depăşirea vitezei legale maxime admise",
                    PenaltyPoints = 9,
                    OrderIndex = 22
                },
                new ExamItem
                {
                    ItemId = 79,
                    FormId = 3,
                    Description = "Tendinţe repetate de a ceda trecerea vehiculelor şi pietonilor care nu au prioritate",
                    PenaltyPoints = 6,
                    OrderIndex = 23
                },
                new ExamItem
                {
                    ItemId = 80,
                    FormId = 3,
                    Description = "Conducerea cu viteză redusă în mod nejustificat, neîncadrarea în ritmul impus de ceilalţi participanţi la trafic",
                    PenaltyPoints = 6,
                    OrderIndex = 24
                },
                new ExamItem
                {
                    ItemId = 81,
                    FormId = 3,
                    Description = "Neîndemânarea în conducere în condiţii de carosabil alunecos (reducerea vitezei, conduită preventivă)",
                    PenaltyPoints = 9,
                    OrderIndex = 25
                },
                new ExamItem
                {
                    ItemId = 82,
                    FormId = 3,
                    Description = "Nerespectarea comenzii examinatorului privind traseul de urmat",
                    PenaltyPoints = 6,
                    OrderIndex = 26
                },
                new ExamItem
                {
                    ItemId = 83,
                    FormId = 3,
                    Description = "Prezentarea la examen sub influenţa băuturilor alcoolice, substanţelor sau produselor stupefiante, a medicamentelor cu efecte similare acestora sau manifestări de natură să perturbe examinarea candidaţilor",
                    PenaltyPoints = 21,
                    OrderIndex = 27
                },
                new ExamItem
                {
                    ItemId = 84,
                    FormId = 3,
                    Description = "Intervenţia instructorului pentru evitarea unui pericol iminent/producerea unui eveniment rutier",
                    PenaltyPoints = 21,
                    OrderIndex = 28
                },

            ////////////// categ C/D traseu //////////////
            ///
                new ExamItem
                {
                    ItemId = 85,
                    FormId = 4,
                    Description = "Neefectuarea controlului vizual, în ordine aleatorie, privind: starea anvelopelor, fixarea roţilor (starea piuliţelor), starea elementelor suspensiei, a rezervoarelor de aer, a parbrizului, ferestrelor, a fluidelor (ulei motor, lichid răcire, fluid spălare parbriz), a blocului de lumini/semnalizare faţă/spate, catadioptrii, trusa medicală, triunghiul reflectorizant, stingătorul de incendiu",
                    PenaltyPoints = 3,
                    OrderIndex = 1
                },
                new ExamItem
                {
                    ItemId = 86,
                    FormId = 4,
                    Description = "Neefectuarea controlului caroseriei, a învelişului uşilor pentru marfă, a mecanismului de încărcare, a fixării încărcăturii (numai pentru C, CE, C1, C1E, Tr)",
                    PenaltyPoints = 9,
                    OrderIndex = 2
                },
                new ExamItem
                {
                    ItemId = 87,
                    FormId = 4,
                    Description = "Neverificarea, prin intermediul aparaturii de bord sau al comenzilor autovehiculului, a funcţionării direcţiei, frânei, a instalaţiei de ungere/răcire, a luminilor, a semnalizării, a avertizorului sonor",
                    PenaltyPoints = 3,
                    OrderIndex = 3
                },
                new ExamItem
                {
                    ItemId = 88,
                    FormId = 4,
                    Description = "Necunoaşterea aparaturii de înregistrare a activităţii conducătorului auto [cu excepţia C1, C1E, Tr, care nu intră în domeniul Regulamentului (CEE) nr. 3.821/85]",
                    PenaltyPoints = 3,
                    OrderIndex = 4
                },
                new ExamItem
                {
                    ItemId = 89,
                    FormId = 4,
                    Description = "Neverificarea dispozitivului de cuplare şi a conexiunilor instalaţiei de frânare/electrice, a catadioptrilor (numai pentru CE, C1E, D1E, DE, Tr)",
                    PenaltyPoints = 9,
                    OrderIndex = 5
                },
                new ExamItem
                {
                    ItemId = 90,
                    FormId = 4,
                    Description = "Neverificarea caroseriei, a uşilor de serviciu, a ieşirilor de urgenţă, a echipamentului de prim ajutor, a stingătoarelor de incendiu şi a altor echipamente de siguranţă (numai pentru D, DE, D1, D1E, Tb, Tv)",
                    PenaltyPoints = 5,
                    OrderIndex = 6
                },
                new ExamItem
                {
                    ItemId = 91,
                    FormId = 4,
                    Description = "Nereglarea scaunului, a oglinzilor retrovizoare, nefixarea centurii de siguranţă, neeliberarea frânei de ajutor",
                    PenaltyPoints = 3,
                    OrderIndex = 7
                },
                new ExamItem
                {
                    ItemId = 92,
                    FormId = 4,
                    Description = "Necunoaşterea aparaturii de bord sau a comenzilor autovehiculului",
                    PenaltyPoints = 3,
                    OrderIndex = 8
                },
                new ExamItem
                {
                    ItemId = 93,
                    FormId = 4,
                    Description = "Cuplarea unei remorci de autovehiculul trăgător din/cu revenire la poziţia iniţială în staţionare paralel",
                    PenaltyPoints = 5,
                    OrderIndex = 9
                },
                new ExamItem
                {
                    ItemId = 94,
                    FormId = 4,
                    Description = "Mersul înapoi",
                    PenaltyPoints = 5,
                    OrderIndex = 10
                },
                new ExamItem
                {
                    ItemId = 95,
                    FormId = 4,
                    Description = "Parcarea în siguranţă cu faţa/cu spatele/laterală pentru încărcare/descărcare, sau la o rampă/platformă de încărcare, sau la o instalaţie similară",
                    PenaltyPoints = 7,
                    OrderIndex = 11
                },
                new ExamItem
                {
                    ItemId = 96,
                    FormId = 4,
                    Description = "Oprirea pentru a permite călătorilor urcarea/coborârea în/din autobuz/tramvai/troleibuz, în siguranţă",
                    PenaltyPoints = 7,
                    OrderIndex = 12
                },
                new ExamItem
                {
                    ItemId = 97,
                    FormId = 4,
                    Description = "Nesincronizarea comenzilor (oprirea motorului, accelerarea excesivă, folosirea incorectă a treptelor de viteză)",
                    PenaltyPoints = 5,
                    OrderIndex = 13
                },
                new ExamItem
                {
                    ItemId = 98,
                    FormId = 4,
                    Description = "Nemenţinerea direcţiei de mers",
                    PenaltyPoints = 9,
                    OrderIndex = 14
                },
                new ExamItem
                {
                    ItemId = 99,
                    FormId = 4,
                    Description = "Folosirea incorectă a drumului, cu sau fără marcaje",
                    PenaltyPoints = 6,
                    OrderIndex = 15
                },
                new ExamItem
                {
                    ItemId = 100,
                    FormId = 4,
                    Description = "Manevrarea incorectă la încrucişarea cu alte vehicule, inclusiv în spaţii restrânse",
                    PenaltyPoints = 6,
                    OrderIndex = 16
                },
                new ExamItem
                {
                    ItemId = 101,
                    FormId = 4,
                    Description = "Executarea incorectă a mersului înapoi, a parcării cu faţa, spatele sau lateral",
                    PenaltyPoints = 5,
                    OrderIndex = 17
                },
                new ExamItem
                {
                    ItemId = 102,
                    FormId = 4,
                    Description = "Executarea incorectă a întoarcerii vehiculului cu faţa în sens opus prin efectuarea manevrelor de mers înainte şi înapoi",
                    PenaltyPoints = 5,
                    OrderIndex = 18
                },
                new ExamItem
                {
                    ItemId = 103,
                    FormId = 4,
                    Description = "Întoarcerea incorectă pe o stradă cu mai multe benzi de circulaţie pe sens",
                    PenaltyPoints = 5,
                    OrderIndex = 19
                },
                new ExamItem
                {
                    ItemId = 104,
                    FormId = 4,
                    Description = "Manevrarea incorectă la urcarea rampelor/coborrea pantelor lungi, la circulaţia în tuneluri",
                    PenaltyPoints = 5,
                    OrderIndex = 20
                },
                new ExamItem
                {
                    ItemId = 105,
                    FormId = 4,
                    Description = "Folosirea incorectă a luminilor de întâlnire/luminilor de drum",
                    PenaltyPoints = 3,
                    OrderIndex = 21
                },
                new ExamItem
                {
                    ItemId = 106,
                    FormId = 4,
                    Description = "Conducerea în mod neeconomic şi agresiv pentru mediul înconjurător (turaţie excesivă, frânare/accelerare nejustificate)",
                    PenaltyPoints = 5,
                    OrderIndex = 22
                },
                new ExamItem
                {
                    ItemId = 107,
                    FormId = 4,
                    Description = "Neasigurarea la schimbarea direcţiei de mers/părăsirea locului de staţionare",
                    PenaltyPoints = 9,
                    OrderIndex = 23
                },
                new ExamItem
                {
                    ItemId = 108,
                    FormId = 4,
                    Description = "Executarea neregulamentară a virajelor",
                    PenaltyPoints = 6,
                    OrderIndex = 24
                },
                new ExamItem
                {
                    ItemId = 109,
                    FormId = 4,
                    Description = "Nesemnalizarea sau semnalizarea greşită a schimbării direcţiei de mers",
                    PenaltyPoints = 6,
                    OrderIndex = 25
                },
                new ExamItem
                {
                    ItemId = 110,
                    FormId = 4,
                    Description = "Încadrarea necorespunzătoare în raport cu direcţia de mers indicată",
                    PenaltyPoints = 6,
                    OrderIndex = 26
                },
                new ExamItem
                {
                    ItemId = 111,
                    FormId = 4,
                    Description = "Efectuarea unor manevre interzise (oprire, staţionare, întoarcere, mers înapoi)",
                    PenaltyPoints = 6,
                    OrderIndex = 27
                },
                new ExamItem
                {
                    ItemId = 112,
                    FormId = 4,
                    Description = "Neasigurarea la pătrunderea în intersecţii",
                    PenaltyPoints = 9,
                    OrderIndex = 28
                },
                new ExamItem
                {
                    ItemId = 113,
                    FormId = 4,
                    Description = "Folosirea incorectă a benzilor la intrarea/ieşirea pe/de pe autostradă/artere similare",
                    PenaltyPoints = 5,
                    OrderIndex = 29
                },
                new ExamItem
                {
                    ItemId = 114,
                    FormId = 4,
                    Description = "Nepăstrarea distanţei suficiente faţă de cei care rulează înainte sau vin din sens opus",
                    PenaltyPoints = 9,
                    OrderIndex = 30
                },
                new ExamItem
                {
                    ItemId = 115,
                    FormId = 4,
                    Description = "Ezitarea repetată de a depăşi alte vehicule",
                    PenaltyPoints = 6,
                    OrderIndex = 31
                },
                new ExamItem
                {
                    ItemId = 116,
                    FormId = 4,
                    Description = "Nerespectarea regulilor de executare a depăşirii ori efectuarea acesteia în locuri şi situaţii interzise",
                    PenaltyPoints = 21,
                    OrderIndex = 32
                },
                new ExamItem
                {
                    ItemId = 117,
                    FormId = 4,
                    Description = "Neacordarea priorităţii vehiculelor şi pietonilor care au acest drept (la plecarea de pe loc, în intersecţii, sens giratoriu, staţie mijloc de transport în comun prevăzută cu alveolă, staţie de tramvai fără refugiu pentru pietoni, trecere de pietoni)",
                    PenaltyPoints = 21,
                    OrderIndex = 33
                },
                new ExamItem
                {
                    ItemId = 118,
                    FormId = 4,
                    Description = "Tendinţe repetate de a ceda trecerea vehiculelor şi pietonilor care nu au prioritate",
                    PenaltyPoints = 6,
                    OrderIndex = 34
                },
                new ExamItem
                {
                    ItemId = 119,
                    FormId = 4,
                    Description = "Nerespectarea semnificaţiei indicatoarelor/marcajelor/culorii semaforului (cu excepţia culorii roşii)",
                    PenaltyPoints = 9,
                    OrderIndex = 35
                },
                new ExamItem
                {
                    ItemId = 120,
                    FormId = 4,
                    Description = "Nerespectarea semnificaţiei culorii roşii a semaforului/semnalelor poliţistului rutier/semnalelor altor persoane cu atribuţii legale similare",
                    PenaltyPoints = 21,
                    OrderIndex = 36
                },
                new ExamItem
                {
                    ItemId = 121,
                    FormId = 4,
                    Description = "Depăşirea vitezei legale maxime admise",
                    PenaltyPoints = 9,
                    OrderIndex = 37
                },
                new ExamItem
                {
                    ItemId = 122,
                    FormId = 4,
                    Description = "Conducerea cu viteză redusă în mod nejustificat, neîncadrarea în ritmul impus de ceilalţi participanţi la trafic",
                    PenaltyPoints = 6,
                    OrderIndex = 38
                },
                new ExamItem
                {
                    ItemId = 123,
                    FormId = 4,
                    Description = "Neîndemânarea în conducere în condiţii de carosabil alunecos (reducerea vitezei, conduită preventivă)",
                    PenaltyPoints = 9,
                    OrderIndex = 39
                },
                new ExamItem
                {
                    ItemId = 124,
                    FormId = 4,
                    Description = "Deplasarea cu viteză neadaptată condiţiilor atmosferice şi de drum",
                    PenaltyPoints = 9,
                    OrderIndex = 40
                },
                new ExamItem
                {
                    ItemId = 125,
                    FormId = 4,
                    Description = "Nerespectarea normelor legale la trecerile la nivel cu calea ferată",
                    PenaltyPoints = 21,
                    OrderIndex = 41
                },
                new ExamItem
                {
                    ItemId = 126,
                    FormId = 4,
                    Description = "Prezentarea la examen sub influenţa băuturilor alcoolice, substanţelor sau produselor stupefiante, a medicamentelor cu efecte similare acestora sau manifestări de natură să perturbe examinarea celorlalţi candidaţi",
                    PenaltyPoints = 21,
                    OrderIndex = 42
                },
                new ExamItem
                {
                    ItemId = 127,
                    FormId = 4,
                    Description = "Intervenţia examinatorului pentru evitarea unui pericol iminent/producerea unui eveniment rutier",
                    PenaltyPoints = 21,
                    OrderIndex = 43
                }
            );
            context.SaveChanges();
        }
        // ──────────────── Vehicle ────────────────
        if (!context.Vehicles.Any())
        {
            var vehicles = new List<Vehicle>();
            var vehicleId = 1;

            foreach (var schoolId in schoolIds)
            {
                var licenseSequence = schoolId == 1
                    ? new[] { "B", "A", "C", "CE", "D" }
                    : new[] { "B", "BE", "C", "CE", "D" };

                var schoolVehicleIds = new List<int>();
                for (var i = 0; i < 5; i++)
                {
                    var licenseCode = licenseSequence[i % licenseSequence.Length];
                    if (!licenseIdByType.TryGetValue(licenseCode, out var licenseId))
                    {
                        licenseId = licenseIdByType["B"];
                    }

                    var plate = $"DF{schoolId}{i + 1:00}-RO";
                    vehicles.Add(new Vehicle
                    {
                        VehicleId = vehicleId,
                        LicensePlateNumber = plate,
                        TransmissionType = i % 2 == 0 ? TransmissionType.MANUAL : TransmissionType.AUTOMATIC,
                        Color = i % 2 == 0 ? "White" : "Blue",
                        Brand = i % 2 == 0 ? "Dacia" : "Ford",
                        Model = i % 2 == 0 ? "Logan" : "Focus",
                        YearOfProduction = 2018 + (i % 5),
                        FuelType = i % 3 == 0 ? TipCombustibil.BENZINA : TipCombustibil.MOTORINA,
                        EngineSizeLiters = 1.2m + (i % 3) * 0.4m,
                        PowertrainType = TipPropulsie.COMBUSTIBIL,
                        ItpExpiryDate = DateTime.Today.AddMonths(6 + i),
                        InsuranceExpiryDate = DateTime.Today.AddMonths(8 + i),
                        RcaExpiryDate = DateTime.Today.AddMonths(10 + i),
                        LicenseId = licenseId,
                        AutoSchoolId = schoolId
                    });

                    schoolVehicleIds.Add(vehicleId);
                    vehicleId++;
                }

                vehicleIdsBySchool[schoolId] = schoolVehicleIds;
            }

            context.Vehicles.AddRange(vehicles);
            context.SaveChanges();
        }

        if (vehicleIdsBySchool.Count == 0)
        {
            vehicleIdsBySchool = context.Vehicles
                .AsNoTracking()
                .Where(v => v.AutoSchoolId.HasValue)
                .GroupBy(v => v.AutoSchoolId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(v => v.VehicleId).OrderBy(id => id).ToList());
        }

        // ──────────────── File (Student enrollment) ────────────────
        if (!context.Files.Any())
        {
            var files = new List<File>();
            var fileId = 1;

            foreach (var schoolId in schoolIds)
            {
                if (!studentIdsBySchool.TryGetValue(schoolId, out var studentIds) ||
                    !instructorIdsBySchool.TryGetValue(schoolId, out var instructorIds) ||
                    !teachingCategoryIdsBySchool.TryGetValue(schoolId, out var categoryIds) ||
                    !vehicleIdsBySchool.TryGetValue(schoolId, out var vehicleIds))
                {
                    continue;
                }

                for (var studentIndex = 0; studentIndex < studentIds.Count; studentIndex++)
                {
                    var studentId = studentIds[studentIndex];
                    var fileCount = studentIndex % 3 == 0 ? 3 : (studentIndex % 3 == 1 ? 2 : 1);

                    for (var f = 0; f < fileCount; f++)
                    {
                        files.Add(new File
                        {
                            FileId = fileId,
                            ScholarshipStartDate = DateTime.Today.AddMonths(-3).AddDays(f * 7),
                            CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
                            MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
                            Status = f == 0 ? FileStatus.APPROVED : FileStatus.ARCHIVED,
                            StudentId = studentId,
                            InstructorId = instructorIds[(studentIndex + f) % instructorIds.Count],
                            TeachingCategoryId = categoryIds[(studentIndex + f) % categoryIds.Count],
                            VehicleId = vehicleIds[(studentIndex + f) % vehicleIds.Count]
                        });
                        fileId++;
                    }
                }
            }

            context.Files.AddRange(files);
            context.SaveChanges();
        }

        // ──────────────── Payment ────────────────
        if (!context.Payments.Any())
        {
            var payments = new List<Payment>();
            var paymentId = 1;
            foreach (var file in context.Files.AsNoTracking().OrderBy(f => f.FileId))
            {
                payments.Add(new Payment
                {
                    PaymentId = paymentId++,
                    ScholarshipBasePayment = paymentId % 2 == 0,
                    SessionsPayed = 10 + (file.FileId % 20),
                    FileId = file.FileId
                });
            }
            context.Payments.AddRange(payments);
            context.SaveChanges();
        }

        // ──────────────── Request ────────────────
        if (!context.Requests.Any())
        {
            var requestId = 1;
            var requestNames = new[]
            {
                new { First = "Alex", Last = "Popescu" },
                new { First = "Maria", Last = "Ionescu" },
                new { First = "Radu", Last = "Marinescu" },
                new { First = "Ana", Last = "Toma" },
                new { First = "Ioana", Last = "Vlad" },
                new { First = "Paul", Last = "Enache" }
            };

            var hasStatusColumn = ColumnExists("Requests", "Status");

            if (hasStatusColumn)
            {
                var requests = new List<Request>();
                foreach (var schoolId in schoolIds)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        var name = requestNames[(requestId - 1) % requestNames.Length];
                        requests.Add(new Request
                        {
                            RequestId = requestId++,
                            FirstName = name.First,
                            LastName = name.Last,
                            PhoneNumber = $"07{schoolId}{i}00000",
                            DrivingCategory = i % 2 == 0 ? "B" : "C",
                            Status = i % 3 == 0 ? "Approved" : "Pending",
                            RequestDate = DateTime.UtcNow.AddDays(-i),
                            AutoSchoolId = schoolId
                        });
                    }
                }

                context.Requests.AddRange(requests);
                context.SaveChanges();
            }
            else
            {
                using var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                foreach (var schoolId in schoolIds)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        var name = requestNames[(requestId - 1) % requestNames.Length];
                        using var command = connection.CreateCommand();
                        command.CommandText = @"
                            INSERT INTO `Requests`
                                (`RequestId`, `FirstName`, `LastName`, `PhoneNumber`, `DrivingCategory`, `RequestDate`, `AutoSchoolId`)
                            VALUES
                                (@id, @first, @last, @phone, @category, @date, @schoolId)";

                        var idParam = command.CreateParameter();
                        idParam.ParameterName = "@id";
                        idParam.Value = requestId++;
                        command.Parameters.Add(idParam);

                        var firstParam = command.CreateParameter();
                        firstParam.ParameterName = "@first";
                        firstParam.Value = name.First;
                        command.Parameters.Add(firstParam);

                        var lastParam = command.CreateParameter();
                        lastParam.ParameterName = "@last";
                        lastParam.Value = name.Last;
                        command.Parameters.Add(lastParam);

                        var phoneParam = command.CreateParameter();
                        phoneParam.ParameterName = "@phone";
                        phoneParam.Value = $"07{schoolId}{i}00000";
                        command.Parameters.Add(phoneParam);

                        var categoryParam = command.CreateParameter();
                        categoryParam.ParameterName = "@category";
                        categoryParam.Value = i % 2 == 0 ? "B" : "C";
                        command.Parameters.Add(categoryParam);

                        var dateParam = command.CreateParameter();
                        dateParam.ParameterName = "@date";
                        dateParam.Value = DateTime.UtcNow.AddDays(-i);
                        command.Parameters.Add(dateParam);

                        var schoolParam = command.CreateParameter();
                        schoolParam.ParameterName = "@schoolId";
                        schoolParam.Value = schoolId;
                        command.Parameters.Add(schoolParam);

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        // ──────────────── InstructorAvailability ────────────────
        if (!context.InstructorAvailabilities.Any())
        {
            var intervals = new List<InstructorAvailability>();
            var intervalId = 1;

            foreach (var schoolId in schoolIds)
            {
                if (!instructorIdsBySchool.TryGetValue(schoolId, out var instructorIds))
                {
                    continue;
                }

                foreach (var instructorId in instructorIds)
                {
                    for (var day = 0; day < 5; day++)
                    {
                        intervals.Add(new InstructorAvailability
                        {
                            IntervalId = intervalId++,
                            Date = DateTime.Today.AddDays(day),
                            StartHour = new TimeSpan(9, 0, 0),
                            EndHour = new TimeSpan(12, 0, 0),
                            InstructorId = instructorId
                        });
                    }
                }
            }

            context.InstructorAvailabilities.AddRange(intervals);
            context.SaveChanges();
        }

        // ──────────────── Appointment (ready for SessionForm) ────────────────
        if (!context.Appointments.Any())
        {
            var appointments = new List<Appointment>();
            var appointmentId = 1;
            foreach (var file in context.Files.AsNoTracking().OrderBy(f => f.FileId))
            {
                var dayOffset = file.FileId % 7;
                var startHour = 9 + (file.FileId % 6);
                appointments.Add(new Appointment
                {
                    AppointmentId = appointmentId++,
                    Date = DateTime.Today.AddDays(dayOffset),
                    StartHour = new TimeSpan(startHour, 0, 0),
                    EndHour = new TimeSpan(startHour + 1, 30, 0),
                    FileId = file.FileId
                });
            }
            context.Appointments.AddRange(appointments);
            context.SaveChanges();
        }

        if (!context.SessionForms.Any())
        {
            var sessionForms = new List<SessionForm>();
            var sessionFormId = 1;
            var fileTeachingCategories = context.Files
                .AsNoTracking()
                .ToDictionary(f => f.FileId, f => f.TeachingCategoryId);

            var examItemsByForm = context.ExamItems
                .AsNoTracking()
                .GroupBy(e => e.FormId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => new { e.ItemId, e.PenaltyPoints })
                          .OrderBy(e => e.ItemId)
                          .ToList());

            (string json, int totalPoints) BuildMistakesJson(int formId, int seed)
            {
                if (!examItemsByForm.TryGetValue(formId, out var items) || items.Count == 0)
                {
                    return ("[{\"id_item\":1,\"count\":1}]", 1);
                }

                var firstIndex = seed % items.Count;
                var secondIndex = (seed + 3) % items.Count;
                var thirdIndex = (seed + 5) % items.Count;

                var first = items[firstIndex];
                var second = items[secondIndex];
                var third = items[thirdIndex];

                var total = first.PenaltyPoints * 1
                            + second.PenaltyPoints * 2
                            + third.PenaltyPoints * 1;

                var json = $"[{{\"id_item\":{first.ItemId},\"count\":1}}," +
                           $"{{\"id_item\":{second.ItemId},\"count\":2}}," +
                           $"{{\"id_item\":{third.ItemId},\"count\":1}}]";

                return (json, total);
            }

            var appointments = context.Appointments
                .AsNoTracking()
                .OrderBy(a => a.AppointmentId)
                .Take(24)
                .ToList();

            foreach (var appointment in appointments)
            {
                int? teachingCategoryId = null;
                if (appointment.FileId.HasValue &&
                    fileTeachingCategories.TryGetValue(appointment.FileId.Value, out var categoryId))
                {
                    teachingCategoryId = categoryId;
                }

                var formId = teachingCategoryId == 2 ? 3
                    : teachingCategoryId == 3 ? 4
                    : 1;

                var createdAt = appointment.Date.Date.AddHours(appointment.StartHour.Hours);
                var (mistakesJson, totalPoints) = BuildMistakesJson(formId, appointment.AppointmentId);
                var result = totalPoints >= 21 ? "FAILED" : "PASSED";

                sessionForms.Add(new SessionForm
                {
                    SessionFormId = sessionFormId++,
                    AppointmentId = appointment.AppointmentId,
                    FormId = formId,
                    MistakesJson = mistakesJson,
                    IsLocked = true,
                    CreatedAt = createdAt,
                    FinalizedAt = createdAt.AddMinutes(45),
                    TotalPoints = totalPoints,
                    Result = result
                });
            }

            context.SessionForms.AddRange(sessionForms);
            context.SaveChanges();
        }
    }




        
    }

