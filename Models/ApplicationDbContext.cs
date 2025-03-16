using DriveFlow_CRM_API;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherForecast> WeatherForecasts { get; set; }
    public DbSet<County> Counties { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<AutoSchool> AutoSchools { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<DriveFlow_CRM_API.Models.File> Files { get; set; }

    public DbSet<ApplicationUser> ApplicationUsers { get; set; }

    // Teaching-related DbSets:
    public DbSet<TeachingCategory> TeachingCategories { get; set; }
    public DbSet<ApplicationUserTeachingCategory> ApplicationUserTeachingCategories { get; set; }

    public DbSet<InstructorAvailability> InstructorAvailabilities { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Student side
        builder.Entity<DriveFlow_CRM_API.Models.File>()
            .HasOne(f => f.Student)
            .WithMany(u => u.StudentFiles)
            .HasForeignKey(f => f.StudentId);

        // Instructor side
        builder.Entity<DriveFlow_CRM_API.Models.File>()
            .HasOne(f => f.Instructor)
            .WithMany(u => u.InstructorFiles)
            .HasForeignKey(f => f.InstructorId);

        // Very important to call the base method,
        // so Identity sets up its own tables (AspNetUsers, etc.).
        base.OnModelCreating(builder);

        // Optionally configure relationships for the bridging entity:
        builder.Entity<ApplicationUserTeachingCategory>(entity =>
        {
            // One ApplicationUser can have many bridging rows
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ApplicationUserTeachingCategories)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // One TeachingCategory can have many bridging rows
            entity.HasOne(e => e.TeachingCategory)
                  .WithMany(t => t.ApplicationUserTeachingCategories)
                  .HasForeignKey(e => e.TeachingCategoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            // (Optional) If you want to prevent duplicates of (UserID, TeachingCategoryID):
            // entity.HasIndex(e => new { e.UserID, e.TeachingCategoryID }).IsUnique();
        });
    }
}
