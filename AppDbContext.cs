using DriveFlow_CRM_API;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Define DbSets for your entities
    // public DbSet<YourEntity> YourEntities { get; set; }
    public DbSet<WeatherForecast> WeatherForecasts { get; set; }

} 