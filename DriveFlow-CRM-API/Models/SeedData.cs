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
        // Use proper DI scope pattern for DbContext - ensures proper connection lifecycle
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Set longer timeout for seeding operations
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

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
            if (connection.State != ConnectionState.Open)
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
            // DO NOT close connection - let EF Core manage it

            return result != null && Convert.ToInt32(result) > 0;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Roles â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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



        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Geography (County, City, Address) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ AutoSchool â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ License â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ TeachingCategory â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ ExamForm â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Create one ExamForm for each of the 15 licenses (FormId = LicenseId for simplicity)
        if (!context.ExamForms.Any())
        {
            var examForms = new List<ExamForm>();
            for (var i = 1; i <= 15; i++)
            {
                examForms.Add(new ExamForm
                {
                    FormId = i,
                    LicenseId = i,
                    MaxPoints = 21
                });
            }
            context.ExamForms.AddRange(examForms);
            context.SaveChanges();
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Users â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ ApplicationUserTeachingCategory â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ ExamItems â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Generate items for ALL 15 licenses using 3 templates:
        // - Template CAR (B): for B1, B, BE (LicenseId 5, 6, 7)
        // - Template MOTO (A): for AM, A1, A2, A (LicenseId 1, 2, 3, 4)
        // - Template TRUCK (C/D): for C1, C1E, C, CE, D1, D1E, D, DE (LicenseId 8-15)
        if (!context.ExamItems.Any())
        {
            // Template items (Description, PenaltyPoints) - without diacritics for encoding compatibility
            var carTemplateItems = new List<(string Description, int PenaltyPoints)>
            {
                ("Neverificarea, prin intermediul aparaturii de bord sau al comenzilor autovehiculului, a functionarii directiei, franei, a instalatiei de ungere/racire, a luminilor, a semnalizarii, a avertizorului sonor", 3),
                ("Neverificarea dispozitivului de cuplare si conexiunilor instalatiei de franare/electrice a catadioptrilor (numai BE)", 9),
                ("Neverificarea elementelor de siguranta legate de incarcatura vehiculului, fixare, inchidere usi/obloane (numai BE)", 9),
                ("Nereglarea scaunului, a oglinzilor retrovizoare, nefixarea centurii de siguranta, neeliberarea franei de ajutor", 3),
                ("Necunoasterea aparaturii de bord sau a comenzilor autovehiculului", 3),
                ("Nesincronizarea comenzilor (oprirea motorului, accelerarea excesiva, folosirea incorecta a treptelor de viteza)", 5),
                ("Nementinerea directiei de mers", 9),
                ("Folosirea incorecta a drumului cu sau fara marcaj", 6),
                ("Manevrarea incorecta la incrucisarea cu alte vehicule, inclusiv in spatii restranse", 6),
                ("Intoarcerea incorecta pe o strada cu mai multe benzi de circulatie pe sens", 5),
                ("Manevrarea incorecta la urcarea rampelor/coborarea pantelor lungi, la circulatia in tuneluri", 5),
                ("Folosirea incorecta a luminilor de intalnire/luminilor de drum", 3),
                ("Conducerea in mod neeconomic si agresiv pentru mediul inconjurator (turatie excesiva, franare/accelerare nejustificate)", 5),
                ("Executarea incorecta a mersului inapoi", 5),
                ("Executarea incorecta a intoarcerii vehiculului cu fata in sens opus prin efectuarea manevrelor de mers inainte si inapoi", 5),
                ("Executarea incorecta a parcarii cu fata, spatele sau lateral", 5),
                ("Executarea incorecta a franarii cu precizie", 5),
                ("Executarea incorecta a cuplarii/decuplarii remorcii la/de la autovehiculul tragator (numai BE)", 5),
                ("Neasigurarea la schimbarea directiei de mers/la parasirea locului de stationare", 9),
                ("Executarea nereglementara a virajelor", 6),
                ("Nesemnalizarea sau semnalizarea gresita a schimbarii directiei de mers", 6),
                ("Incadrarea necorespunzatoare in raport cu directia de mers indicata", 6),
                ("Efectuarea unor manevre interzise (oprire, stationare, intoarcere, mers inapoi)", 6),
                ("Neasigurarea la patrunderea in intersectii", 9),
                ("Folosirea incorecta a benzilor la intrarea/iesirea pe/de pe autostrada/artere similare", 5),
                ("Nepastrarea distantei suficiente fata de cei care ruleaza inainte sau vin din sens opus", 9),
                ("Ezitarea repetata de a depasi alte vehicule", 3),
                ("Nerespectarea regulilor de executare a depasirii ori efectuarea acestora in locuri si situatii interzise", 21),
                ("Neacordarea prioritatii vehiculelor si pietonilor care au acest drept (la plecarea de pe loc, in intersectii, sens giratoriu, statie de mijloc de transport in comun prevazuta cu alveola, statie de tramvai fara refugiu pentru pietoni, trecere de pietoni)", 21),
                ("Tendinte repetate de a ceda trecerea vehiculelor si pietonilor care nu au prioritate", 6),
                ("Nerespectarea semnificatiei indicatoarelor/marcajelor/culorilor semaforului (cu exceptia culorii rosii)", 9),
                ("Nerespectarea semnificatiei culorii rosii a semaforului/a semnalelor politistului rutier/a semnalelor altor persoane cu atributii legale similare", 21),
                ("Depasirea vitezei maxime admise", 5),
                ("Conducerea cu viteza redusa in mod nejustificat, neincadrarea in ritmul impus de ceilalti participanti la trafic", 3),
                ("Neindemanarea in conducerea in conditii de ploaie, zapada, mazga, polei", 9),
                ("Deplasarea cu viteza neadaptata conditiilor atmosferice si de drum", 9),
                ("Prezentarea la examen sub influenta bauturilor alcoolice, substantelor sau produselor stupefiante, a medicamentelor cu efecte similare acestora sau manifestari de natura sa perturbe examinarea celorlalti candidati", 21),
                ("Interventia examinatorului pentru evitarea unui pericol iminent/producerea unui eveniment rutier", 21)
            };

            var motoTemplateItems = new List<(string Description, int PenaltyPoints)>
            {
                ("Nesincronizarea comenzilor (oprirea motorului, accelerarea excesiva, folosirea incorecta a treptelor de viteza)", 6),
                ("Nementinerea directiei de mers", 9),
                ("Folosirea incorecta a drumului cu sau fara marcaj", 6),
                ("Manevrarea incorecta la incrucisarea cu alte vehicule, inclusiv in spatii restranse", 6),
                ("Neasigurarea la schimbarea directiei de mers", 9),
                ("Executarea nereglementara a virajelor", 6),
                ("Nesemnalizarea sau semnalizarea gresita a schimbarii directiei de mers", 6),
                ("Folosirea incorecta a luminilor de intalnire/luminilor de drum", 3),
                ("Neincadrarea corespunzatoare in raport cu directia de mers indicata", 6),
                ("Efectuarea unor manevre interzise (oprire, stationare, intoarcere)", 6),
                ("Neasigurarea la patrunderea in intersectii/la parasirea zonei de stationare", 9),
                ("Folosirea incorecta a benzilor la intrarea/iesirea pe/de pe autostrada/artere similare", 5),
                ("Nepastrarea distantei suficiente fata de cei care ruleaza inainte sau vin din sens opus", 9),
                ("Conducerea in mod neeconomic si agresiv pentru mediul inconjurator (turatie excesiva, franare/accelerare nejustificate)", 5),
                ("Manevrarea incorecta la urcarea rampelor/coborarea pantelor lungi, la circulatia in tuneluri", 5),
                ("Nerespectarea normelor legale referitoare la manevra de depasire", 21),
                ("Ezitarea repetata de a depasi alte vehicule", 6),
                ("Neacordarea prioritatii de trecere vehiculelor si pietonilor care au acest drept (la plecarea de pe loc, in intersectii, sens giratoriu, statie mijloc de transport in comun prevazuta cu alveola, statie de tramvai fara refugiu pentru pietoni, trecere de pietoni)", 21),
                ("Nerespectarea semnificatiei culorii rosii a semaforului/a semnalelor politistului rutier/a semnalelor altor persoane cu atributii legale similare", 21),
                ("Nerespectarea semnificatiei indicatoarelor/marcajelor/culorii semaforului (cu exceptia culorii rosii)", 9),
                ("Nerespectarea normelor legale referitoare la trecerea la nivel cu calea ferata", 21),
                ("Depasirea vitezei legale maxime admise", 9),
                ("Tendinte repetate de a ceda trecerea vehiculelor si pietonilor care nu au prioritate", 6),
                ("Conducerea cu viteza redusa in mod nejustificat, neincadrarea in ritmul impus de ceilalti participanti la trafic", 6),
                ("Neindemanarea in conducere in conditii de carosabil alunecos (reducerea vitezei, conduita preventiva)", 9),
                ("Nerespectarea comenzii examinatorului privind traseul de urmat", 6),
                ("Prezentarea la examen sub influenta bauturilor alcoolice, substantelor sau produselor stupefiante, a medicamentelor cu efecte similare acestora sau manifestari de natura sa perturbe examinarea candidatilor", 21),
                ("Interventia instructorului pentru evitarea unui pericol iminent/producerea unui eveniment rutier", 21)
            };

            var truckTemplateItems = new List<(string Description, int PenaltyPoints)>
            {
                ("Neefectuarea controlului vizual, in ordine aleatorie, privind: starea anvelopelor, fixarea rotilor (starea piulitelor), starea elementelor suspensiei, a rezervoarelor de aer, a parbrizului, ferestrelor, a fluidelor (ulei motor, lichid racire, fluid spalare parbriz), a blocului de lumini/semnalizare fata/spate, catadioptrii, trusa medicala, triunghiul reflectorizant, stingatorul de incendiu", 3),
                ("Neefectuarea controlului caroseriei, a invelisului usilor pentru marfa, a mecanismului de incarcare, a fixarii incarcaturii (numai pentru C, CE, C1, C1E, Tr)", 9),
                ("Neverificarea, prin intermediul aparaturii de bord sau al comenzilor autovehiculului, a functionarii directiei, franei, a instalatiei de ungere/racire, a luminilor, a semnalizarii, a avertizorului sonor", 3),
                ("Necunoasterea aparaturii de inregistrare a activitatii conducatorului auto [cu exceptia C1, C1E, Tr, care nu intra in domeniul Regulamentului (CEE) nr. 3.821/85]", 3),
                ("Neverificarea dispozitivului de cuplare si a conexiunilor instalatiei de franare/electrice, a catadioptrilor (numai pentru CE, C1E, D1E, DE, Tr)", 9),
                ("Neverificarea caroseriei, a usilor de serviciu, a iesirilor de urgenta, a echipamentului de prim ajutor, a stingatoarelor de incendiu si a altor echipamente de siguranta (numai pentru D, DE, D1, D1E, Tb, Tv)", 5),
                ("Nereglarea scaunului, a oglinzilor retrovizoare, nefixarea centurii de siguranta, neeliberarea franei de ajutor", 3),
                ("Necunoasterea aparaturii de bord sau a comenzilor autovehiculului", 3),
                ("Cuplarea unei remorci de autovehiculul tragator din/cu revenire la pozitia initiala in stationare paralel", 5),
                ("Mersul inapoi", 5),
                ("Parcarea in siguranta cu fata/cu spatele/laterala pentru incarcare/descarcare, sau la o rampa/platforma de incarcare, sau la o instalatie similara", 7),
                ("Oprirea pentru a permite calatorilor urcarea/coborarea in/din autobuz/tramvai/troleibuz, in siguranta", 7),
                ("Nesincronizarea comenzilor (oprirea motorului, accelerarea excesiva, folosirea incorecta a treptelor de viteza)", 5),
                ("Nementinerea directiei de mers", 9),
                ("Folosirea incorecta a drumului, cu sau fara marcaje", 6),
                ("Manevrarea incorecta la incrucisarea cu alte vehicule, inclusiv in spatii restranse", 6),
                ("Executarea incorecta a mersului inapoi, a parcarii cu fata, spatele sau lateral", 5),
                ("Executarea incorecta a intoarcerii vehiculului cu fata in sens opus prin efectuarea manevrelor de mers inainte si inapoi", 5),
                ("Intoarcerea incorecta pe o strada cu mai multe benzi de circulatie pe sens", 5),
                ("Manevrarea incorecta la urcarea rampelor/coborarea pantelor lungi, la circulatia in tuneluri", 5),
                ("Folosirea incorecta a luminilor de intalnire/luminilor de drum", 3),
                ("Conducerea in mod neeconomic si agresiv pentru mediul inconjurator (turatie excesiva, franare/accelerare nejustificate)", 5),
                ("Neasigurarea la schimbarea directiei de mers/parasirea locului de stationare", 9),
                ("Executarea nereglementara a virajelor", 6),
                ("Nesemnalizarea sau semnalizarea gresita a schimbarii directiei de mers", 6),
                ("Incadrarea necorespunzatoare in raport cu directia de mers indicata", 6),
                ("Efectuarea unor manevre interzise (oprire, stationare, intoarcere, mers inapoi)", 6),
                ("Neasigurarea la patrunderea in intersectii", 9),
                ("Folosirea incorecta a benzilor la intrarea/iesirea pe/de pe autostrada/artere similare", 5),
                ("Nepastrarea distantei suficiente fata de cei care ruleaza inainte sau vin din sens opus", 9),
                ("Ezitarea repetata de a depasi alte vehicule", 6),
                ("Nerespectarea regulilor de executare a depasirii ori efectuarea acesteia in locuri si situatii interzise", 21),
                ("Neacordarea prioritatii vehiculelor si pietonilor care au acest drept (la plecarea de pe loc, in intersectii, sens giratoriu, statie mijloc de transport in comun prevazuta cu alveola, statie de tramvai fara refugiu pentru pietoni, trecere de pietoni)", 21),
                ("Tendinte repetate de a ceda trecerea vehiculelor si pietonilor care nu au prioritate", 6),
                ("Nerespectarea semnificatiei indicatoarelor/marcajelor/culorii semaforului (cu exceptia culorii rosii)", 9),
                ("Nerespectarea semnificatiei culorii rosii a semaforului/semnalelor politistului rutier/semnalelor altor persoane cu atributii legale similare", 21),
                ("Depasirea vitezei legale maxime admise", 9),
                ("Conducerea cu viteza redusa in mod nejustificat, neincadrarea in ritmul impus de ceilalti participanti la trafic", 6),
                ("Neindemanarea in conducere in conditii de carosabil alunecos (reducerea vitezei, conduita preventiva)", 9),
                ("Deplasarea cu viteza neadaptata conditiilor atmosferice si de drum", 9),
                ("Nerespectarea normelor legale la trecerile la nivel cu calea ferata", 21),
                ("Prezentarea la examen sub influenta bauturilor alcoolice, substantelor sau produselor stupefiante, a medicamentelor cu efecte similare acestora sau manifestari de natura sa perturbe examinarea celorlalti candidati", 21),
                ("Interventia examinatorului pentru evitarea unui pericol iminent/producerea unui eveniment rutier", 21)
            };

            // License mapping: which template to use for each LicenseId
            // LicenseId 1-4 (AM, A1, A2, A) -> moto template
            // LicenseId 5-7 (B1, B, BE) -> car template
            // LicenseId 8-15 (C1, C1E, C, CE, D1, D1E, D, DE) -> truck template
            var examItems = new List<ExamItem>();
            var itemId = 1;

            for (var licenseId = 1; licenseId <= 15; licenseId++)
            {
                var templateItems = licenseId switch
                {
                    >= 1 and <= 4 => motoTemplateItems,  // AM, A1, A2, A
                    >= 5 and <= 7 => carTemplateItems,   // B1, B, BE
                    _ => truckTemplateItems              // C1, C1E, C, CE, D1, D1E, D, DE
                };

                var orderIndex = 1;
                foreach (var (description, penaltyPoints) in templateItems)
                {
                    examItems.Add(new ExamItem
                    {
                        ItemId = itemId++,
                        FormId = licenseId, // FormId = LicenseId
                        Description = description,
                        PenaltyPoints = penaltyPoints,
                        OrderIndex = orderIndex++
                    });
                }
            }

            context.ExamItems.AddRange(examItems);
            context.SaveChanges();
        }
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Vehicle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ File (Student enrollment) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Payment â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Request â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ InstructorAvailability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Appointment (ready for SessionForm) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
            
            // Get file -> teachingCategoryId mapping
            var fileTeachingCategories = context.Files
                .AsNoTracking()
                .ToDictionary(f => f.FileId, f => f.TeachingCategoryId);

            // Get teachingCategory -> licenseId mapping (used to determine FormId)
            var categoryToLicense = context.TeachingCategories
                .AsNoTracking()
                .Where(tc => tc.LicenseId.HasValue)
                .ToDictionary(tc => tc.TeachingCategoryId, tc => tc.LicenseId!.Value);

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
                    // Fallback to FormId 6 (license B) if no items found for this form
                    if (examItemsByForm.TryGetValue(6, out var fallbackItems) && fallbackItems.Count > 0)
                    {
                        items = fallbackItems;
                    }
                    else
                    {
                        return ("[{\"id_item\":1,\"count\":1}]", 1);
                    }
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
                int? licenseId = null;
                if (appointment.FileId.HasValue &&
                    fileTeachingCategories.TryGetValue(appointment.FileId.Value, out var categoryId) &&
                    categoryId.HasValue &&
                    categoryToLicense.TryGetValue(categoryId.Value, out var lId))
                {
                    licenseId = lId;
                }

                // FormId = LicenseId (since ExamForm is now linked to License)
                // Default to FormId 6 (license B) if no license found
                var formId = licenseId ?? 6;

                var createdAt = appointment.Date.Date.AddHours(appointment.StartHour.Hours);
                var (mistakesJson, totalPoints) = BuildMistakesJson(formId, appointment.AppointmentId);
                var result = totalPoints >= 21 ? "FAILED" : "PASSED";

                sessionForms.Add(new SessionForm
                {
                    SessionFormId = sessionFormId++,
                    AppointmentId = appointment.AppointmentId,
                    FormId = formId,
                    MistakesJson = mistakesJson,
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

