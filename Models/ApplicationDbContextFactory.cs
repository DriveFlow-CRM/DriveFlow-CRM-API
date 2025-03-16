/*ApplicationDbContextFactory implements EF Core’s IDesignTimeDbContextFactory<ApplicationDbContext>, specifying how ApplicationDbContext is created at design-time.
This prevents errors like “Unable to create a ‘DbContext’…” when running commands such as dotnet ef migrations add or dotnet ef database 
update by providing a factory method (CreateDbContext) that returns a properly configured context instance.*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("JAWSDB_URL");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Environment variable 'JAWSDB_URL' is not set.");
        }

        builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new ApplicationDbContext(builder.Options);
    }
}
