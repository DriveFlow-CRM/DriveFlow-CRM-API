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
/// • Falls back to converting a DB connection URI from <c>DB_CONNECTION_URI</c> when needed.
/// </para>
/// </remarks>

public sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string ConnectionUriVar = "DB_CONNECTION_URI";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Try to load .env from current directory first, then parent directories
        var currentDir = Directory.GetCurrentDirectory();
        var envPath = Path.Combine(currentDir, ".env");
        
        if (!File.Exists(envPath))
        {
            // Try parent directory (for when running from DriveFlow-CRM-API\DriveFlow-CRM-API)
            var parentDir = Directory.GetParent(currentDir)?.FullName;
            if (parentDir != null)
            {
                var parentEnvPath = Path.Combine(parentDir, ".env");
                if (File.Exists(parentEnvPath))
                {
                    envPath = parentEnvPath;
                }
            }
        }
        
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? cs = null;

        var connectionUri = configuration[ConnectionUriVar];
        if (!string.IsNullOrWhiteSpace(connectionUri))
        {
            var uri = new Uri(connectionUri);
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
                "No database connection configured. Set DB_CONNECTION_URI or ConnectionStrings__DefaultConnection.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(cs, ServerVersion.AutoDetect(cs))
            .Options;

        return new ApplicationDbContext(options);
    }
}
