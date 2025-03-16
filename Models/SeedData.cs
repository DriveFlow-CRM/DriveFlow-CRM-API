using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;
using System.Collections.Generic;

public static class SeedData
{
    public static void Initialize(IServiceProvider
   serviceProvider)
    {
        using (var context = new ApplicationDbContext(
        serviceProvider.GetRequiredService
        <DbContextOptions<ApplicationDbContext>>()))
        {
    
    if (context.Roles.Any())
            {
                return; 
            }

            context.Roles.AddRange(
            new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc0", Name = "Admin", NormalizedName = "SuperAdmin".ToUpper() },
            new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc1", Name = "Editor", NormalizedName = "SchoolAdmin".ToUpper() },
            new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2", Name = "User", NormalizedName = "Student".ToUpper() },
            new IdentityRole { Id = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3", Name = "User", NormalizedName = "Instructor".ToUpper() }
            );
             var hasher = new PasswordHasher<ApplicationUser>();
            context.Users.AddRange(
            new ApplicationUser
            {
                Id = "419decbe-6af1-4d84-9b45-c1ef796f4600",
                // primary key
                UserName = "superadmin@test.com",
                EmailConfirmed = true,
                NormalizedEmail = "SUPERADMIN@TEST.COM",
                Email = "superadmin@test.com",
                NormalizedUserName = "SUPERADMIN@TEST.COM",
                PasswordHash = hasher.HashPassword(null,"SuperAdmin231!")
            },
           new ApplicationUser
           {
                Id = "419decbe-6af1-4d84-9b45-c1ef796f4601",
                // primary key
                UserName = "schooladmin@test.com",
                EmailConfirmed = true,
                NormalizedEmail = "SCHOOLADMIN@TEST.COM",
                Email = "schooladmin@test.com",
                NormalizedUserName = "SCHOOLADMIN@TEST.COM",
                PasswordHash = hasher.HashPassword(null,"SchoolAdmin231!")
            },
           new ApplicationUser
           {
               Id = "419decbe-6af1-4d84-9b45-c1ef796f4602",
               // primary key
               UserName = "student@test.com",
               EmailConfirmed = true,
               NormalizedEmail = "STUDENT@TEST.COM",
               Email = "student@test.com",
               NormalizedUserName = "STUDENT@TEST.COM",
               PasswordHash = hasher.HashPassword(null,"Student231!")
           },
            new ApplicationUser
            {
                Id = "419decbe-6af1-4d84-9b45-c1ef796f4602",
                // primary key
                UserName = "instructor@test.com",
                EmailConfirmed = true,
                NormalizedEmail = "INSTRUCTOR@TEST.COM",
                Email = "instructor@test.com",
                NormalizedUserName = "INSTRUCTOR@TEST.COM",
                PasswordHash = hasher.HashPassword(null, "Instructor231!")
            }
           );
            context.UserRoles.AddRange(
            new IdentityUserRole<string>
            {
                RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc0",
                UserId = "419decbe-6af1-4d84-9b45-c1ef796f4600"
            },
           new IdentityUserRole<string>
           {
               RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc1",
               UserId = "419decbe-6af1-4d84-9b45-c1ef796f4601"
           },
           new IdentityUserRole<string>
           {
               RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc2",
               UserId = "419decbe-6af1-4d84-9b45-c1ef796f4602"
           },
            new IdentityUserRole<string>
            {
                RoleId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3",
                UserId = "c391f8d7-3e74-4017-ac10-59fe7c4e5dc3"
            }
            );
            context.SaveChanges();
        }
    }
}