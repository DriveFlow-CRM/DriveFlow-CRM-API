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
            // Resolve ApplicationDbContext with the DI‑configured options.
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // ──────────────── Roles ────────────────
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

            // ──────────────── Users ────────────────
            if (!context.Users.Any())
            {
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
                    }
                );

                // ──────────────── User ↔ Role mappings ────────────────
                context.UserRoles.AddRange(
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4600", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc0" },
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4601", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc1" },
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4602", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
                    new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4603", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3" }
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
                context.Licenses.Add(new License
                {
                    LicenseId = 1,
                    Type = "B"
                });
                context.SaveChanges();
            }

            // ──────────────── TeachingCategory ────────────────
            if (!context.TeachingCategories.Any())
            {
                context.TeachingCategories.Add(new TeachingCategory
                {
                    TeachingCategoryId = 1,
                    Code = "B",
                    SessionCost = 150,
                    SessionDuration = 90,
                    ScholarshipPrice = 2500,
                    MinDrivingLessonsReq = 30,
                    LicenseId = 1,
                    AutoSchoolId = 1
                });
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
                context.ExamForms.Add(new ExamForm
                {
                    FormId = 1,
                    TeachingCategoryId = 1,
                    MaxPoints = 21
                });
                context.SaveChanges();
            }

            // ──────────────── ExamItems ────────────────
            if (!context.ExamItems.Any())
            {
                context.ExamItems.AddRange(
                    new ExamItem
                    {
                        ItemId = 1,
                        FormId = 1,
                        Description = "Pornire și oprire corectă",
                        PenaltyPoints = 3,
                        OrderIndex = 1
                    },
                    new ExamItem
                    {
                        ItemId = 2,
                        FormId = 1,
                        Description = "Respectarea regulilor de circulație",
                        PenaltyPoints = 5,
                        OrderIndex = 2
                    },
                    new ExamItem
                    {
                        ItemId = 3,
                        FormId = 1,
                        Description = "Semnalizare",
                        PenaltyPoints = 2,
                        OrderIndex = 3
                    },
                    new ExamItem
                    {
                        ItemId = 4,
                        FormId = 1,
                        Description = "Parcare",
                        PenaltyPoints = 3,
                        OrderIndex = 4
                    }
                );
                context.SaveChanges();
            }

            // ──────────────── Vehicle ────────────────
            if (!context.Vehicles.Any())
            {
                context.Vehicles.Add(new Vehicle
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
                });
                context.SaveChanges();
            }

            // ──────────────── File (Student enrollment) ────────────────
            if (!context.Files.Any())
            {
                context.Files.Add(new File
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
                });
                context.SaveChanges();
            }

            // ──────────────── Payment ────────────────
            if (!context.Payments.Any())
            {
                context.Payments.Add(new Payment
                {
                    PaymentId = 1,
                    ScholarshipBasePayment = true,
                    SessionsPayed = 30,
                    FileId = 1
                });
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
                context.Appointments.Add(new Appointment
                {
                    AppointmentId = 1,
                    Date = DateTime.Today.AddDays(1),
                    StartHour = new TimeSpan(10, 0, 0),
                    EndHour = new TimeSpan(11, 30, 0),
                    FileId = 1
                });
                context.SaveChanges();
            }
        }
    }

