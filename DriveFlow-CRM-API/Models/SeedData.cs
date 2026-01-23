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

        // #region agent log
        LogDebug("H3", "SeedData.cs:32", "seed start", new { });
        // #endregion

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



        // ──────────────── Users ────────────────
        if (!context.Users.Any())
            {
                // #region agent log
                LogDebug("H5", "SeedData.cs:251", "ensure autoschool before users", new { });
                // #endregion
                if (!context.Counties.Any())
                {
                    context.Counties.Add(new County
                    {
                        CountyId = 1,
                        Name = "Cluj",
                        Abbreviation = "CJ"
                    });
                    context.SaveChanges();
                }

                if (!context.Cities.Any())
                {
                    context.Cities.Add(new City
                    {
                        CityId = 1,
                        Name = "Cluj-Napoca",
                        CountyId = 1
                    });
                    context.SaveChanges();
                }

                if (!context.Addresses.Any())
                {
                    context.Addresses.Add(new Address
                    {
                        AddressId = 1,
                        StreetName = "Strada Aviatorilor",
                        AddressNumber = "10",
                        Postcode = "400000",
                        CityId = 1
                    });
                    context.SaveChanges();
                }

                if (!context.AutoSchools.Any())
                {
                    context.AutoSchools.Add(new AutoSchool
                    {
                        AutoSchoolId = 1,
                        Name = "DriveFlow Test School",
                        Description = "Test driving school for SessionForm testing",
                        PhoneNumber = "0740123456",
                        Email = "school@driveflow.test",
                        WebSite = "https://driveflow.test",
                        Status = AutoSchoolStatus.Active,
                        AddressId = 1
                    });
                    context.SaveChanges();
                }

                // #region agent log
                LogDebug("H5", "SeedData.cs:308", "autoschool ready", new { autoSchools = context.AutoSchools.Count() });
                // #endregion

                var hasher = new PasswordHasher<ApplicationUser>();

                context.Users.AddRange(
                    new ApplicationUser
                    {
                        Id = "419decbe-6af1-4d84-9b45-c1ef796f4600",
                        UserName = "superadmin@test.com",
                        NormalizedUserName = "SUPERADMIN@TEST.COM",
                        Email = "superadmin@test.com",
                        NormalizedEmail = "SUPERADMIN@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), "SuperAdmin231!"),
                        FirstName = "Super",
                        LastName = "Admin"
                    },
                    new ApplicationUser
                    {
                        Id = "419decbe-6af1-4d84-9b45-c1ef796f4601",
                        UserName = "schooladmin@test.com",
                        NormalizedUserName = "SCHOOLADMIN@TEST.COM",
                        Email = "schooladmin@test.com",
                        NormalizedEmail = "SCHOOLADMIN@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), "SchoolAdmin231!"),
                        FirstName = "School",
                        LastName = "Admin",
                        AutoSchoolId = 1
                    },
                    new ApplicationUser
                    {
                        Id = "419decbe-6af1-4d84-9b45-c1ef796f4602",
                        UserName = "student@test.com",
                        NormalizedUserName = "STUDENT@TEST.COM",
                        Email = "student@test.com",
                        NormalizedEmail = "STUDENT@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), "Student231!"),
                        FirstName = "Test",
                        LastName = "Student",
                        Cnp = "1234567890123",
                        AutoSchoolId = 1
                    },
                    new ApplicationUser
                    {
                        Id = "419decbe-6af1-4d84-9b45-c1ef796f4603",
                        UserName = "instructor@test.com",
                        NormalizedUserName = "INSTRUCTOR@TEST.COM",
                        Email = "instructor@test.com",
                        NormalizedEmail = "INSTRUCTOR@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), "Instructor231!"),
                        FirstName = "Test",
                        LastName = "Instructor",
                        AutoSchoolId = 1
                    },

                    new ApplicationUser
                    {
                        Id = "419decbe-6af1-4d84-9b45-c1ef796f4604",
                        UserName = "mihailconstantin@gmail.com",
                        NormalizedUserName = "MIHAILCONSTANTIN@GMAIL.COM",
                        Email = "mihailconstantin@gmail.com",
                        NormalizedEmail = "MIHAILCONSTANTIN@GMAIL.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), "mihail123*"),
                        FirstName = "Mihail",
                        LastName = "Constantin",
                        AutoSchoolId = 1
                    },


                    new ApplicationUser
                    {
                        Id = "419decbe-6af1-4d84-9b45-c1ef796f4605",
                        UserName = "anaabsinte@gmail.com",
                        NormalizedUserName = "ANAABSINTE@GMAIL.COM",
                        Email = "anaabsinte@gmail.com",
                        NormalizedEmail = "ANAABSINTE@GMAIL.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), "longlivabsinth969*"),
                        FirstName = "Ana",
                        LastName = "Absinte",
                        AutoSchoolId = 1
                    },

                    new ApplicationUser
                    {
                        Id = "419decbe-6af1-4d84-9b45-c1ef796f4606",
                        UserName = "sanduilie@gmail.com",
                        NormalizedUserName = "SANDUILIE@GMAIL.COM",
                        Email = "sanduilie@gmail.com",
                        NormalizedEmail = "SANDUILIE@GMAIL.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), "gloryto^ROMANIA^*"),
                        FirstName = "Sandu",
                        LastName = "Ilie",
                        AutoSchoolId = 1
                    },

                    //
                    new ApplicationUser
                    {
                        Id = "419decbe-6af1-4d84-9b45-c1ef796f4607",
                        UserName = "andreipostavaru@test.com",
                        NormalizedUserName = "ANDREIPOSTAVARU@GMAIL.COM",
                        Email = "andreipostavaru@test.com",
                        NormalizedEmail = "ANDREIPOSTAVARU@GMAIL.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(new ApplicationUser(), "VandGolf_6_!"),
                        FirstName = "Andrei",
                        LastName = "Postavaru",
                        AutoSchoolId = 1
                    }
                );

                // ──────────────── User ↔ Role mappings ────────────────
                context.UserRoles.AddRange(
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4600", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc0" },
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4601", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc1" },
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4602", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4603", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3" },
                    ///
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4604", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4605", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4606", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
                    //
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4607", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3" }
                );

                

                context.SaveChanges();
            }

            // ──────────────── Geography (County, City, Address) ────────────────
            if (!context.Counties.Any())
            {
                context.Counties.Add(new County
                {
                    CountyId = 1,
                    Name = "Cluj",
                    Abbreviation = "CJ"
                });
                context.SaveChanges();
            }

            if (!context.Cities.Any())
            {
                context.Cities.Add(new City
                {
                    CityId = 1,
                    Name = "Cluj-Napoca",
                    CountyId = 1
                });
                context.SaveChanges();
            }

            if (!context.Addresses.Any())
            {
                context.Addresses.Add(new Address
                {
                    AddressId = 1,
                    StreetName = "Strada Aviatorilor",
                    AddressNumber = "10",
                    Postcode = "400000",
                    CityId = 1
                });
                context.SaveChanges();
            }

            // ──────────────── AutoSchool ────────────────
            if (!context.AutoSchools.Any())
            {
                context.AutoSchools.Add(new AutoSchool
                {
                    AutoSchoolId = 1,
                    Name = "DriveFlow Test School",
                    Description = "Test driving school for SessionForm testing",
                    PhoneNumber = "0740123456",
                    Email = "school@driveflow.test",
                    WebSite = "https://driveflow.test",
                    Status = AutoSchoolStatus.Active,
                    AddressId = 1
                });
                context.SaveChanges();
            }

            // ──────────────── License ────────────────
            if (!context.Licenses.Any())
            {
                context.Licenses.AddRange(new License
                {
                    LicenseId = 1,
                    Type = "B"
                },
                new License
                {
                    LicenseId = 2,
                    Type = "A"
                },
                new License
                {
                    LicenseId = 3,
                    Type = "C/D"
                }
                );
                context.SaveChanges();
            }

            // ──────────────── TeachingCategory ────────────────
            if (!context.TeachingCategories.Any())
            {
                context.TeachingCategories.AddRange(new TeachingCategory
                {
                    TeachingCategoryId = 1,
                    Code = "B",
                    SessionCost = 150,
                    SessionDuration = 90,
                    ScholarshipPrice = 2500,
                    MinDrivingLessonsReq = 30,
                    LicenseId = 1,
                    AutoSchoolId = 1
                },
                new TeachingCategory
                {
                    TeachingCategoryId = 2,
                    Code = "A",
                    SessionCost = 120,
                    SessionDuration = 90,
                    ScholarshipPrice = 2000,
                    MinDrivingLessonsReq = 30,
                    LicenseId = 2,
                    AutoSchoolId = 1
                }
                ,
                new TeachingCategory
                {
                    TeachingCategoryId = 3,
                    Code = "C/D",
                    SessionCost = 180,
                    SessionDuration = 90,
                    ScholarshipPrice = 3000,
                    MinDrivingLessonsReq = 40,
                    LicenseId = 3,
                    AutoSchoolId = 1
                }
                );
                context.SaveChanges();
            }

            // ──────────────── ApplicationUserTeachingCategory ────────────────
            if (!context.ApplicationUserTeachingCategories.Any())
            {
                context.ApplicationUserTeachingCategories.Add(new ApplicationUserTeachingCategory
                {
                    ApplicationUserTeachingCategoryId = 1,
                    UserId = "419decbe-6af1-4d84-9b45-c1ef796f4603",
                    TeachingCategoryId = 1
                });
                context.SaveChanges();
            }

            // ──────────────── ExamForm ────────────────
            if (!context.ExamForms.Any())
            {
                context.ExamForms.AddRange(new ExamForm //categ A
                {
                    FormId = 1,
                    TeachingCategoryId = 1,
                    MaxPoints = 21
                },
                //new ExamForm //Categoria A poligon
                //{
                //    FormId = 2,
                //    TeachingCategoryId = 2,
                //    MaxPoints = 16
                //},
                new ExamForm
                {
                    FormId = 3,//Categoria A traseu
                    TeachingCategoryId = 2,
                    MaxPoints = 21
                },
                new ExamForm //Categoria C/D
                {
                    FormId = 4,
                    TeachingCategoryId = 3,
                    MaxPoints = 21
                });
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
                context.Vehicles.AddRange(new Vehicle
                {
                    VehicleId = 1,
                    LicensePlateNumber = "CJ-01-TEST",
                    TransmissionType = TransmissionType.MANUAL,
                    Color = "White",
                    Brand = "Dacia",
                    Model = "Logan",
                    YearOfProduction = 2020,
                    FuelType = TipCombustibil.BENZINA,
                    EngineSizeLiters = 1.2m,
                    PowertrainType = TipPropulsie.COMBUSTIBIL,
                    LicenseId = 1,
                    AutoSchoolId = 1
                },
                new Vehicle
                {
                    VehicleId = 2,
                    LicensePlateNumber = "B-66-ROM",
                    TransmissionType = TransmissionType.MANUAL,
                    Color = "Black",
                    Brand = "Suzuki",
                    Model = "Hayabusa",
                    YearOfProduction = 2021,
                    FuelType = TipCombustibil.MOTORINA,
                    EngineSizeLiters = 8.7m,
                    PowertrainType = TipPropulsie.COMBUSTIBIL,
                    LicenseId = 2,
                    AutoSchoolId = 1
                },
                new Vehicle
                {
                    VehicleId = 3,
                    LicensePlateNumber = "B-252-AFR",
                    TransmissionType = TransmissionType.MANUAL,
                    Color = "White",
                    Brand = "Opel",
                    Model = "Astra",
                    YearOfProduction = 2018,
                    FuelType = TipCombustibil.BENZINA,
                    EngineSizeLiters = 40.0m,
                    PowertrainType = TipPropulsie.COMBUSTIBIL,
                    LicenseId = 1,
                    AutoSchoolId = 1
                },
                new Vehicle
                {
                    VehicleId = 4,
                    LicensePlateNumber = "B-989-KZE",
                    TransmissionType = TransmissionType.MANUAL,
                    Color = "Red",
                    Brand = "Ford",
                    Model = "Focus",
                    YearOfProduction = 2002,
                    FuelType = TipCombustibil.MOTORINA,
                    EngineSizeLiters = 35.0m,
                    PowertrainType = TipPropulsie.COMBUSTIBIL,
                    LicenseId = 1,
                    AutoSchoolId = 1
                }

                );
                context.SaveChanges();
            }

            // ──────────────── File (Student enrollment) ────────────────
            if (!context.Files.Any())
            {
                context.Files.AddRange(new File
                {
                    FileId = 1,
                    ScholarshipStartDate = DateTime.Today,
                    CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
                    MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
                    Status = FileStatus.APPROVED,
                    StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4602",
                    InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4603",
                    TeachingCategoryId = 1,
                    VehicleId = 1
                },
                    new File
                    {
                        FileId = 2,
                        ScholarshipStartDate = DateTime.Today,
                        CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
                        MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
                        Status = FileStatus.APPROVED,
                        StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4604",
                        InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4607",
                        TeachingCategoryId = 1,
                        VehicleId = 1
                    },
                    new File
                    {
                        FileId = 3,
                        ScholarshipStartDate = DateTime.Today.AddMonths(-5),
                        CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
                        MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
                        Status = FileStatus.APPROVED,
                        StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4605",
                        InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4607",
                        TeachingCategoryId = 1,
                        VehicleId = 3
                    },
                    new File
                    {
                        FileId = 4,
                        ScholarshipStartDate = DateTime.Today,
                        CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
                        MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
                        Status = FileStatus.APPROVED,
                        StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4606",
                        InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4607",
                        TeachingCategoryId = 2,
                        VehicleId = 2
                    },
                    new File
                    {
                        FileId = 5,
                        ScholarshipStartDate = DateTime.Today,
                        CriminalRecordExpiryDate = DateTime.Today.AddMonths(12),
                        MedicalRecordExpiryDate = DateTime.Today.AddMonths(6),
                        Status = FileStatus.APPROVED,
                        StudentId = "419decbe-6af1-4d84-9b45-c1ef796f4602",
                        InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4607",
                        TeachingCategoryId = 1,
                        VehicleId = 1
                    }

                );
                context.SaveChanges();
            }

            // ──────────────── Payment ────────────────
            if (!context.Payments.Any())
            {
                context.Payments.AddRange(
                   new Payment
                {
                    PaymentId = 1,
                    ScholarshipBasePayment = true,
                    SessionsPayed = 30,
                    FileId = 1
                },
                new Payment
                {
                    PaymentId = 2,
                    ScholarshipBasePayment = false,
                    SessionsPayed = 40,
                    FileId = 2
                },
                new Payment
                {
                    PaymentId = 3,
                    ScholarshipBasePayment = true,
                    SessionsPayed = 70,
                    FileId = 3
                },
                new Payment
                {
                    PaymentId = 4,
                    ScholarshipBasePayment = true,
                    SessionsPayed = 39,
                    FileId = 4
                },
                new Payment
                {
                    PaymentId = 5,
                    ScholarshipBasePayment = false,
                    SessionsPayed = 1,
                    FileId = 5
                }
                );
                context.SaveChanges();
            }

            // ──────────────── InstructorAvailability ────────────────
            if (!context.InstructorAvailabilities.Any())
            {
                var today = DateTime.Today;
                for (int i = 0; i < 7; i++)
                {
                    var date = today.AddDays(i);
                    context.InstructorAvailabilities.Add(new InstructorAvailability
                    {
                        IntervalId = i + 1,
                        Date = date,
                        StartHour = new TimeSpan(9, 0, 0),
                        EndHour = new TimeSpan(17, 0, 0),
                        InstructorId = "419decbe-6af1-4d84-9b45-c1ef796f4603"
                    });
                }
                context.SaveChanges();
            }

            // ──────────────── Appointment (ready for SessionForm) ────────────────
            if (!context.Appointments.Any())
            {
                context.Appointments.AddRange(new Appointment
                {
                    AppointmentId = 1,
                    Date = DateTime.Today.AddDays(1),
                    StartHour = new TimeSpan(10, 0, 0),
                    EndHour = new TimeSpan(11, 30, 0),
                    FileId = 1
                },

                   new Appointment
                   {
                       AppointmentId = 2,
                       Date = DateTime.Today.AddDays(1),
                       StartHour = new TimeSpan(11, 0, 0),
                       EndHour = new TimeSpan(12, 30, 0),
                       FileId = 2
                   },
                    new Appointment
                    {
                        AppointmentId = 3,
                        Date = DateTime.Today.AddDays(2-2*30),
                        StartHour = new TimeSpan(9, 0, 0),
                        EndHour = new TimeSpan(10, 30, 0),
                        FileId = 3
                    },
                    new Appointment
                    {
                        AppointmentId = 4,
                        Date = DateTime.Today.AddDays(2-30),
                        StartHour = new TimeSpan(11, 0, 0),
                        EndHour = new TimeSpan(12, 30, 0),
                        FileId = 3
                    },
                    new Appointment
                    {
                        AppointmentId = 5,
                        Date = DateTime.Today.AddDays(3),
                        StartHour = new TimeSpan(14, 0, 0),
                        EndHour = new TimeSpan(15, 30, 0),
                        FileId = 3
                    },
                    new Appointment
                    {
                        AppointmentId = 6,
                        Date = DateTime.Today.AddDays(4),
                        StartHour = new TimeSpan(16, 0, 0),
                        EndHour = new TimeSpan(17, 30, 0),
                        FileId = 4
                    },
                    new Appointment
                    {
                        AppointmentId = 7,
                        Date = DateTime.Today.AddDays(4),
                        StartHour = new TimeSpan(16, 0, 0),
                        EndHour = new TimeSpan(17, 30, 0),
                        FileId = 4
                    }
            
                );
                context.SaveChanges();
            };

        if (!context.SessionForms.Any())
        {
            context.SessionForms.AddRange(
                new SessionForm
                {
                    SessionFormId = 1,
                    AppointmentId = 1,
                    FormId = 1,
                    MistakesJson = "[{\"id_item\":6,\"count\":3}]",
                    IsLocked = false,
                    CreatedAt = DateTime.Now,
                    FinalizedAt = DateTime.Now.AddMinutes(30),
                    TotalPoints = 15,
                    Result = "PASSED"
                },
                new SessionForm
                {
                    SessionFormId = 2,
                    AppointmentId = 2,
                    FormId = 1,
                    MistakesJson = "[{\"id_item\":11,\"count\":1},{\"id_item\":20,\"count\":2},{\"id_item\":21,\"count\":1}]",
                    IsLocked = false,
                    CreatedAt = DateTime.Now,
                    FinalizedAt = DateTime.Now.AddMinutes(30),
                    TotalPoints = 17,
                    Result = "PASSED"
                },
                new SessionForm
                {
                    SessionFormId = 3,
                    AppointmentId = 3,
                    FormId = 1,
                    MistakesJson = "[{\"id_item\":1,\"count\":1},{\"id_item\":5,\"count\":2},{\"id_item\":10,\"count\":1}]",
                    IsLocked = false,
                    CreatedAt = DateTime.Today.AddDays(2 - 2*30),
                    FinalizedAt = DateTime.Now.AddDays(2 - 2*30).AddMinutes(30),
                    TotalPoints = 42,
                    Result = "FAILED"
                },
                new SessionForm
                {
                    SessionFormId = 4,
                    AppointmentId = 4,
                    FormId = 1,
                    MistakesJson = "[{\"id_item\":29,\"count\":1},{\"id_item\":38,\"count\":2},{\"id_item\":15,\"count\":2}]",
                    IsLocked = false,
                    CreatedAt = DateTime.Today.AddDays(2 - 30),
                    FinalizedAt = DateTime.Now.AddDays(2 - 30).AddMinutes(30),
                    TotalPoints = 73,
                    Result = "FAILED"
                },
                new SessionForm
                {
                    SessionFormId = 5,
                    AppointmentId = 5,
                    FormId = 1,
                    MistakesJson = "[{\"id_item\":33,\"count\":1},{\"id_item\":6,\"count\":2}]",
                    IsLocked = false,
                    CreatedAt = DateTime.Now,
                    FinalizedAt = DateTime.Now.AddMinutes(30),
                    TotalPoints = 17,
                    Result = "PASSED"
                },
                new SessionForm
                {
                    SessionFormId = 6,
                    AppointmentId = 6,
                    FormId = 3,
                    MistakesJson = "[{\"id_item\":76,\"count\":1},{\"id_item\":73,\"count\":1},{\"id_item\":78,\"count\":1}]", //73, 78
                    IsLocked = false,
                    CreatedAt = DateTime.Now,
                    FinalizedAt = DateTime.Now.AddMinutes(30),
                    TotalPoints = 21,
                    Result = "FAILED"
                },
                new SessionForm
                {
                    SessionFormId = 7,
                    AppointmentId = 7,
                    FormId = 3,
                    MistakesJson = "[{\"id_item\":60,\"count\":1},{\"id_item\":73,\"count\":2}]",
                    IsLocked = false,
                    CreatedAt = DateTime.Now,
                    FinalizedAt = DateTime.Now.AddMinutes(30),
                    TotalPoints = 17,
                    Result = "PASSED"
                }
            );
            context.SaveChanges();
        }
    }




        
    }

