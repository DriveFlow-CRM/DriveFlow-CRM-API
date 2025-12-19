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

            // Seed roles and users if not already present
            if (!context.Roles.Any())
            {
                SeedRolesAndUsers(context);
            }

            // Seed forms and items if not already present
            if (!context.Formulars.Any())
            {
                SeedFormsAndItems(context);
            }
        }

        /// <summary>
        /// Seeds default roles and users.
        /// </summary>
        private static void SeedRolesAndUsers(ApplicationDbContext context)
        {

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
                    PasswordHash = hasher.HashPassword(new ApplicationUser(), "SuperAdmin231!")
                },
                new ApplicationUser
                {
                    Id = "419decbe-6af1-4d84-9b45-c1ef796f4601",
                    UserName = "schooladmin@test.com",
                    NormalizedUserName = "SCHOOLADMIN@TEST.COM",
                    Email = "schooladmin@test.com",
                    NormalizedEmail = "SCHOOLADMIN@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(new ApplicationUser(), "SchoolAdmin231!")
                },
                new ApplicationUser
                {
                    Id = "419decbe-6af1-4d84-9b45-c1ef796f4602",
                    UserName = "student@test.com",
                    NormalizedUserName = "STUDENT@TEST.COM",
                    Email = "student@test.com",
                    NormalizedEmail = "STUDENT@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(new ApplicationUser(), "Student231!")
                },
                new ApplicationUser
                {
                    Id = "419decbe-6af1-4d84-9b45-c1ef796f4603",
                    UserName = "instructor@test.com",
                    NormalizedUserName = "INSTRUCTOR@TEST.COM",
                    Email = "instructor@test.com",
                    NormalizedEmail = "INSTRUCTOR@TEST.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(new ApplicationUser(), "Instructor231!")
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

        /// <summary>
        /// Seeds forms and items for teaching categories.
        /// Seeds a reference form for Category B with 21 max points and predefined penalty items.
        /// </summary>
        /// <remarks>
        /// This is a reference implementation that seeds for the first available teaching category.
        /// In production, consider:
        /// 1. Creating a global reference categories table (not school-specific)
        /// 2. Seeding forms for all standard license categories (A, B, C, etc.)
        /// 3. Linking school-specific teaching categories to these reference forms
        /// </remarks>
        private static void SeedFormsAndItems(ApplicationDbContext context)
        {
            // If there are no teaching categories yet, we can't seed forms
            if (!context.TeachingCategories.Any())
                return;

            // Seed form for the first teaching category as a reference
            // This serves as the official exam form for instructors to reference
            var teachingCategory = context.TeachingCategories.FirstOrDefault();
            if (teachingCategory == null)
                return;

            // Create form for Category B with 21 max points
            var formular = new Formular
            {
                TeachingCategoryId = teachingCategory.TeachingCategoryId,
                MaxPoints = 21
            };
            context.Formulars.Add(formular);
            context.SaveChanges(); // Save to get the FormularId

            // Create predefined items for Category B
            var items = new List<Item>
            {
                new Item { FormularId = formular.FormularId, Description = "Semnalizare la schimbarea direcției", PenaltyPoints = 3, OrderIndex = 1 },
                new Item { FormularId = formular.FormularId, Description = "Neasigurare la plecarea de pe loc", PenaltyPoints = 3, OrderIndex = 2 },
                new Item { FormularId = formular.FormularId, Description = "Neasigurare la schimbarea benzii", PenaltyPoints = 3, OrderIndex = 3 },
                new Item { FormularId = formular.FormularId, Description = "Neacordare prioritate pietoni", PenaltyPoints = 3, OrderIndex = 4 },
                new Item { FormularId = formular.FormularId, Description = "Nerespectare semnalizare semafor", PenaltyPoints = 3, OrderIndex = 5 },
                new Item { FormularId = formular.FormularId, Description = "Viteză neadaptată la condiții", PenaltyPoints = 2, OrderIndex = 6 },
                new Item { FormularId = formular.FormularId, Description = "Distanță necorespunzătoare față de vehiculul din față", PenaltyPoints = 2, OrderIndex = 7 },
                new Item { FormularId = formular.FormularId, Description = "Poziționare incorectă pe carosabil", PenaltyPoints = 2, OrderIndex = 8 },
                new Item { FormularId = formular.FormularId, Description = "Folosire necorespunzătoare ambreiaj/frână", PenaltyPoints = 1, OrderIndex = 9 },
                new Item { FormularId = formular.FormularId, Description = "Oprire/staționare neregulamentară", PenaltyPoints = 2, OrderIndex = 10 }
            };

            context.Items.AddRange(items);
            context.SaveChanges();
        }
    }

