using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow_CRM_API.Models;

    /// <summary>
    /// Seeds default roles and users for the DriveFlow CRM database.
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

            // Abort seeding if at least one role already exists.
            if (context.Roles.Any())
                return;

            // ──────────────── Roles ────────────────
            context.Roles.AddRange(
                new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc0", Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc1", Name = "SchoolAdmin", NormalizedName = "SCHOOLADMIN" },
                new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2", Name = "Student", NormalizedName = "STUDENT" },
                new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3", Name = "Instructor", NormalizedName = "INSTRUCTOR" }
            );

            // ──────────────── Users ────────────────
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
                    PasswordHash = hasher.HashPassword(null, "SuperAdmin231!")
                },
                new ApplicationUser
                {
                    Id = "419decbe-6af1-4d84-9b45-c1ef796f4601",
                    UserName = "schooladmin@test.com",
                    NormalizedUserName = "SCHOOLADMIN@TEST.COM",
                    Email = "schooladmin@test.com",
                    NormalizedEmail = "SCHOOLADMIN@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "SchoolAdmin231!")
                },
                new ApplicationUser
                {
                    Id = "419decbe-6af1-4d84-9b45-c1ef796f4602",
                    UserName = "student@test.com",
                    NormalizedUserName = "STUDENT@TEST.COM",
                    Email = "student@test.com",
                    NormalizedEmail = "STUDENT@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "Student231!")
                },
                new ApplicationUser
                {
                    Id = "419decbe-6af1-4d84-9b45-c1ef796f4603",
                    UserName = "instructor@test.com",
                    NormalizedUserName = "INSTRUCTOR@TEST.COM",
                    Email = "instructor@test.com",
                    NormalizedEmail = "INSTRUCTOR@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "Instructor231!")
                }
            );

            // ──────────────── User ↔ Role mappings ────────────────
            context.UserRoles.AddRange(
                new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4600", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc0" },
                new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4601", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc1" },
                new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4602", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2" },
                new IdentityUserRole<string> { UserId = "419decbe-6af1-4d84-9b45-c1ef796f4603", RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3" }
            );

            // Commit all inserts in a single transaction.
            context.SaveChanges();
        }
    }

