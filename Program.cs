using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using DotNetEnv;
using Microsoft.AspNetCore.Identity; // For Identity
using DriveFlow_CRM_API.Models;
// using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // If you're using IdentityDbContext


var builder = WebApplication.CreateBuilder(args);

// 1. Load environment variables from the .env file
Env.Load();

// 2. Determine the port (commonly used on Heroku/containers)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// 3. Build the initial connection string (from appsettings or environment)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Check if we're in an environment with JAWSDB (Heroku)
var jawsDbUrl = Environment.GetEnvironmentVariable("JAWSDB_URL");
if (!string.IsNullOrEmpty(jawsDbUrl))
{
    var uri = new Uri(jawsDbUrl);
    connectionString =
            $"Server={uri.Host};Database={uri.AbsolutePath.Trim('/')};" +
            $"User ID={uri.UserInfo.Split(':')[0]};" +
            $"Password={uri.UserInfo.Split(':')[1]};" +
            $"Port={uri.Port};SSL Mode=Required;";
}

// 4. Register the ApplicationDbContext using MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// 5. Add Identity with roles, specifying ApplicationUser as the user model
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Enforce email confirmation before allowing login
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// 6. Add controllers
builder.Services.AddControllers();

// 7. Build the application
var app = builder.Build();

// (Optional) Log the connection string to verify
app.Logger.LogInformation("Using connection string: {0}", connectionString);

// 8. Configure the request pipeline
app.UseRouting();

// Identity requires authentication/authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// 9. Map controllers (no UseEndpoints needed in .NET 6 minimal hosting)
app.MapControllers();

// 10. Run the app
app.Run();
