using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveFlow_CRM_API;
/// <summary>
/// Design-time factory for <see cref="ApplicationDbContext"/> used by EF Core tooling
/// (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>).
/// </summary>
/// <remarks>
/// <para>
/// • Reads the JawsDB connection URI from the <c>JAWSDB_URL</c> environment variable.<br/>
/// • Converts that URI into a MySQL connection string and configures the Pomelo provider.<br/>
/// • Throws <see cref="System.InvalidOperationException"/> if <c>JAWSDB_URL</c> is missing
///   so design-time operations fail fast.
/// </para>
/// </remarks>

public sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string JawsVar = "JAWSDB_URL";

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>JAWSDB_URL</c> is not defined.
    /// </exception>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var uriString = Environment.GetEnvironmentVariable(JawsVar)
                       ?? throw new InvalidOperationException(
                           $"Environment variable '{JawsVar}' is not set.");

        // Convert URI  (mysql://user:pass@host:port/db) → key=value;...
        var uri = new Uri(uriString);
        var userPass = uri.UserInfo.Split(':', 2);

        var cs = $"Server={uri.Host};Port={uri.Port};" +
                 $"Database={uri.AbsolutePath.TrimStart('/')};" +
                 $"User ID={userPass[0]};Password={userPass[1]};" +
                 "SslMode=Required;";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseMySql(cs, ServerVersion.AutoDetect(cs))
                      .Options;

        return new ApplicationDbContext(options);
    }
}
