using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DriveFlow_CRM_API.Models;
using File = DriveFlow_CRM_API.Models.File;

/// <summary>
///     Entity Framework Core context for DriveFlow CRM. Combines ASP.NET Core
///     Identity tables (via <see cref="IdentityDbContext{TUser}"/>) with the
///     domain entities specific to the platform.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ──────────────── DbSets ────────────────
    /// <summary>Romanian counties.</summary>
    public DbSet<County> Counties { get; set; }

    /// <summary>Cities associated with a <see cref="County"/>.</summary>
    public DbSet<City> Cities { get; set; }

    /// <summary>Physical addresses.</summary>
    public DbSet<Address> Addresses { get; set; }

    /// <summary>Driving schools registered on the platform.</summary>
    public DbSet<AutoSchool> AutoSchools { get; set; }

    /// <summary>User‑submitted requests (enrolment, category upgrade, etc.).</summary>
    public DbSet<Request> Requests { get; set; }

    /// <summary>Vehicles owned or used by an <see cref="AutoSchool"/>.</summary>
    public DbSet<Vehicle> Vehicles { get; set; }

    /// <summary>Payments and invoices.</summary>
    public DbSet<Payment> Payments { get; set; }

    /// <summary>Driving licences.</summary>
    public DbSet<License> Licenses { get; set; }

    /// <summary>Uploaded documents (medical, criminal record, contracts…).</summary>
    public DbSet<File> Files { get; set; }

    /// <summary>Platform users (students, instructors, admins).</summary>
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }

    /// <summary>Teaching categories (A, B, C…).</summary>
    public DbSet<TeachingCategory> TeachingCategories { get; set; }

    /// <summary>Join table between users and the categories they teach.</summary>
    public DbSet<ApplicationUserTeachingCategory> ApplicationUserTeachingCategories { get; set; }

    /// <summary>Instructor availability time slots.</summary>
    public DbSet<InstructorAvailability> InstructorAvailabilities { get; set; }

    /// <summary>Driving lessons and exam appointments.</summary>
    public DbSet<Appointment> Appointments { get; set; }

    /// <summary>Exam forms for teaching categories.</summary>
    public DbSet<Formular> Formulars { get; set; }

    /// <summary>Exam items (penalties) within forms.</summary>
    public DbSet<Item> Items { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Always call the base method first, so Identity creates its tables.
        base.OnModelCreating(builder);

        // ───────── County ↔ City (1 : M) ─────────
        builder.Entity<County>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.Abbreviation).IsUnique();

            entity.HasMany(c => c.Cities)
                  .WithOne(city => city.County)
                  .HasForeignKey(city => city.CountyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── City ↔ Address (1 : M) ─────────
        builder.Entity<City>(entity =>
        {
            entity.HasMany(c => c.Addresses)
                  .WithOne(a => a.City)
                  .HasForeignKey(a => a.CityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── Address ↔ AutoSchool (1 : 1) ─────────
        builder.Entity<Address>(entity =>
        {
            entity.HasIndex(a => a.Postcode).IsUnique();
        });

        builder.Entity<AutoSchool>(entity =>
        {
            entity.HasOne(a => a.Address)
                  .WithOne()
                  .HasForeignKey<AutoSchool>(a => a.AddressId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ───────── AutoSchool core configuration ─────────
        builder.Entity<AutoSchool>(entity =>
        {
            entity.HasIndex(a => a.PhoneNumber).IsUnique();
            entity.HasIndex(a => a.Email).IsUnique();

            entity.Property(a => a.Status).HasConversion<string>();
        });

        // ───────── AutoSchool ↔ Users (1 : M, cascade) ─────────
        builder.Entity<ApplicationUser>()
               .HasOne(u => u.AutoSchool)
               .WithMany(s => s.ApplicationUsers)
               .HasForeignKey(u => u.AutoSchoolId)
               .OnDelete(DeleteBehavior.Cascade);

        // ───────── AutoSchool ↔ TeachingCategory (1 : M, cascade) ─────────
        builder.Entity<TeachingCategory>()
               .HasOne(tc => tc.AutoSchool)
               .WithMany(s => s.TeachingCategories)
               .HasForeignKey(tc => tc.AutoSchoolId)
               .OnDelete(DeleteBehavior.Cascade);

        // ───────── AutoSchool ↔ Request (1 : M, cascade) ─────────
        builder.Entity<Request>()
               .HasOne(r => r.AutoSchool)
               .WithMany(s => s.Requests)
               .HasForeignKey(r => r.AutoSchoolId)
               .OnDelete(DeleteBehavior.Cascade);

        // ───────── Vehicle ↔ File (1 : M optional, SetNull) ─────────
        builder.Entity<File>()
               .HasOne(f => f.Vehicle)
               .WithMany(v => v.Files)
               .HasForeignKey(f => f.VehicleId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(v => v.LicensePlateNumber).IsUnique();

            // ───────── Vehicle → License (M : 1, SetNull) ─────────
            entity.HasOne(v => v.License)
                  .WithMany(l => l.Vehicles)
                  .HasForeignKey(v => v.LicenseId)
                  .OnDelete(DeleteBehavior.SetNull);

            // ───────── AutoSchool ↔ Vehicle (1 : M, cascade) ─────────
            entity.HasOne(v => v.AutoSchool)
                  .WithMany(s => s.Vehicles)
                  .HasForeignKey(v => v.AutoSchoolId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── TeachingCategory → License (M : 1 optional, SetNull) ─────────
        builder.Entity<TeachingCategory>()
               .HasOne(tc => tc.License)
               .WithMany(l => l.TeachingCategories)
               .HasForeignKey(tc => tc.LicenseId)
               .OnDelete(DeleteBehavior.SetNull);

        // ───────── TeachingCategory → File (1 : M optional, SetNull) ─────────
        builder.Entity<File>()
               .HasOne(f => f.TeachingCategory)
               .WithMany(tc => tc.Files)
               .HasForeignKey(f => f.TeachingCategoryId)
               .OnDelete(DeleteBehavior.SetNull);

        // ───────── TeachingCategory ↔ ApplicationUserTeachingCategory (1 : M, cascade) ─────────
        builder.Entity<ApplicationUserTeachingCategory>()
               .HasOne(j => j.TeachingCategory)
               .WithMany(tc => tc.ApplicationUserTeachingCategories)
               .HasForeignKey(j => j.TeachingCategoryId)
               .OnDelete(DeleteBehavior.Cascade);

        // ───────── File ↔ Payment (1 : 1, cascade) ─────────
        builder.Entity<Payment>()
        .HasOne(p => p.File)            
        .WithOne()                     
        .HasForeignKey<Payment>(p => p.FileId)
        .OnDelete(DeleteBehavior.Cascade);

        // ───────── File ↔ Appointment (1 : M, cascade) ─────────
        builder.Entity<Appointment>()
               .HasOne(a => a.File)
               .WithMany(f => f.Appointments)
               .HasForeignKey(a => a.FileId)
               .OnDelete(DeleteBehavior.Cascade);

        // ───────── File → Student (M : 1, cascade) ─────────
        builder.Entity<File>()
               .HasOne(f => f.Student)
               .WithMany(u => u.StudentFiles)
               .HasForeignKey(f => f.StudentId)
               .OnDelete(DeleteBehavior.Cascade);

        // ───────── File → Instructor (M : 1, SetNull) ─────────
         builder.Entity<File>()
            .HasOne(f => f.Instructor)
            .WithMany(u => u.InstructorFiles)
            .HasForeignKey(f => f.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);

        // ───────── InstructorAvailability → Instructor (M : 1, cascade) ─────────
        builder.Entity<InstructorAvailability>()
               .HasOne(ia => ia.Instructor)
               .WithMany(u => u.InstructorAvailabilities)
               .HasForeignKey(ia => ia.InstructorId)
               .OnDelete(DeleteBehavior.Cascade);

        // ───────── ApplicationUserTeachingCategory join (M : N, cascade) ─────────
        builder.Entity<ApplicationUserTeachingCategory>(entity =>
        {
            entity.HasKey(j => j.ApplicationUserTeachingCategoryId);

            // User (1) → (M) join
            entity.HasOne(j => j.User)
                  .WithMany(u => u.ApplicationUserTeachingCategories)
                  .HasForeignKey(j => j.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // TeachingCategory (1) → (M) join
            entity.HasOne(j => j.TeachingCategory)
                  .WithMany(tc => tc.ApplicationUserTeachingCategories)
                  .HasForeignKey(j => j.TeachingCategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── TeachingCategory ↔ Formular (1 : 1, cascade) ─────────
        builder.Entity<Formular>(entity =>
        {
            entity.HasOne(f => f.TeachingCategory)
                  .WithOne()
                  .HasForeignKey<Formular>(f => f.TeachingCategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── Formular ↔ Item (1 : M, cascade) ─────────
        builder.Entity<Item>(entity =>
        {
            entity.HasOne(i => i.Formular)
                  .WithMany(f => f.Items)
                  .HasForeignKey(i => i.FormularId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
