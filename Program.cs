using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
Env.Load();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Check if we're in Heroku
var jawsDbUrl = Environment.GetEnvironmentVariable("JAWSDB_URL");
if (!string.IsNullOrEmpty(jawsDbUrl))
{
    // Parse for Heroku
    var uri = new Uri(jawsDbUrl);
    connectionString = $"Server={uri.Host};Database={uri.AbsolutePath.Trim('/')};User ID={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};Port={uri.Port};SSL Mode=Required;";
}

// Database setup
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

app.Logger.LogInformation("Using connection string: {0}", connectionString);

// Configure the HTTP request pipeline
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    // Use our controllers
    endpoints.MapControllers();
});

app.Run();