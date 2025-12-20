using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DriveFlow_CRM_API;
/// <summary>
/// Design-time factory for <see cref="ApplicationDbContext"/> used by EF Core tooling
/// (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>).
/// </summary>
/// <remarks>
/// <para>
/// • Resolves the connection string from the standard "DefaultConnection" key (env/appsettings).
/// • Falls back to converting a JawsDB URI from <c>JAWSDB_URL</c> when needed.
/// </para>
/// </remarks>

public sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string JawsVar = "JAWSDB_URL";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.Load();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? cs = null;

        var jawsDbUrl = configuration[JawsVar];
        if (!string.IsNullOrWhiteSpace(jawsDbUrl))
        {
            var uri = new Uri(jawsDbUrl);
            var userPass = uri.UserInfo.Split(':', 2);

            cs = $"Server={uri.Host};Port={uri.Port};" +
                 $"Database={uri.AbsolutePath.TrimStart('/')};" +
                 $"User ID={userPass[0]};Password={userPass[1]};" +
                 "SslMode=Required;";
        }
        else
        {
            cs = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "No database connection configured. Set JAWSDB_URL or ConnectionStrings__DefaultConnection.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(cs, ServerVersion.AutoDetect(cs))
            .Options;

        return new ApplicationDbContext(options);
    }
}
