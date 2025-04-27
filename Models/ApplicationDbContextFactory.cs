using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveFlow_CRM_API;

/// <summary>
/// Design-time factory for <see cref="ApplicationDbContext"/> used by EF Core
/// tooling (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>).
/// Implementing <see cref="IDesignTimeDbContextFactory{TContext}"/> prevents the
/// “Unable to create a DbContext” error by telling EF Core exactly how to build
/// the context when the application entry point is not executed.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>
///     <description>Reads the JawsDB connection URI from the environment
///     variable <c>JAWSDB_URL</c>.</description>
///   </item>
///   <item>
///     <description>Converts that URI into a regular MySQL connection string and
///     configures Pomelo via
///     <see cref="RelationalDatabaseFacadeExtensions.UseMySql(DbContextOptionsBuilder,string,Action{MySqlDbContextOptionsBuilder})"/>,
///     letting <see cref="ServerVersion.AutoDetect(string)"/> pick the correct
///     dialect.</description>
///   </item>
///   <item>
///     <description>Throws <see cref="InvalidOperationException"/> if
///     <c>JAWSDB_URL</c> is missing so design-time operations fail fast.</description>
///   </item>
/// </list>
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
