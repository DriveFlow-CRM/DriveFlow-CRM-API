using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AspNetCoreRateLimit;                       
using System.Text;
using DriveFlow_CRM_API.Models;
using DriveFlow_CRM_API.Authentication;
using DriveFlow_CRM_API.Authentication.Tokens;
using DriveFlow_CRM_API.Authentication.Tokens.Handlers;
using DriveFlow_CRM_API.Auth;
using Microsoft.AspNetCore.Authorization;
using DriveFlow_CRM_API.Json;

/// <summary>
/// Configures services (Swagger, EF Core, Identity, JWT, rate-limit), builds the HTTP
/// pipeline, seeds default data and starts the DriveFlow CRM Web API.
/// </summary>
public partial class Program
{
    // ──────────────────────────────── 1. Environment  ────────────────────────────────
    // Load variables from .env FIRST, so they are visible to the configuration builder.
    public static void Main(string[] args)
    {
        DotNetEnv.Env.Load();

        var builder = WebApplication.CreateBuilder(args);

        // Enable detailed startup errors (useful while debugging 500 responses)
        builder.WebHost.CaptureStartupErrors(true)
                       .UseSetting("detailedErrors", "true");

        // ─────────────────────────── Swagger / OpenAPI – Services ───────────────────────────
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DriveFlow CRM API",
                Version = "v1",
                Description = "REST endpoints for the DriveFlow platform."
            });

            // Include XML comments generated at build time (if the file exists)
            var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
                opt.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            // ───────────── JWT bearer schema so Swagger shows the Authorize button ─────────────
            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT in header – format: **Bearer &lt;token&gt;**",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
            });
        });

        // ────────────────────────── Environment & Configuration ────────────────────────────
        // 2. Determine the HTTP port (container platforms usually inject PORT).
        var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
        builder.WebHost.UseUrls($"http://*:{port}");

        // ───────────────────────────── Database & Identity ────────────────────────────────
        // 3. Resolve the base connection string from appsettings.json or environment variables.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        // 4. Convert Heroku-style JawsDB URI → MySQL connection string when present.
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

        // 5. Register the application's DbContext (Pomelo MySQL provider).
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        // 6. Configure ASP.NET Core Identity with role support.
        builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            // Require confirmed e-mail before sign-in.
            options.SignIn.RequireConfirmedAccount = true;

            // ───────────── Lockout (per-user cool-down) ─────────────
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;                // after 5 bad passwords
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10); // lock 10 min
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        // ──────────────────────── JWT Authentication & Authorization ───────────────────────
        // 1) Read JWT settings (Issuer & Audience from appsettings.json, KEY from env-var).
        var jwtSection = builder.Configuration.GetSection("Jwt");

        //    Prefer the environment variable JWT_KEY; fall back to Jwt:Key only for local dev.
        var jwtKey = builder.Configuration["JWT_KEY"] ?? jwtSection["Key"];

        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            throw new InvalidOperationException("JWT_KEY is missing or shorter than 32 characters (256 bits). Provide a strong secret via environment variables.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        // 3) Register Bearer authentication (signature, issuer, audience & lifetime validation).
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
           .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = signingKey,

                ClockSkew = TimeSpan.Zero,      // no grace period on expiry
                RoleClaimType = "userRole"
            };
        });
        // ───────────────────────  Add authorization policies  ───────────────────────
        builder.Services.AddAuthorization(options =>
        {
            // ───── SUPER-ADMIN  (no school comparison) ─────
            options.AddPolicy("SuperAdmin", policy => policy
                .RequireRole("SuperAdmin"));

            // ───── SCHOOL-ADMIN  (role + same school) ─────
            options.AddPolicy("SchoolAdmin", policy => policy
                .RequireRole("SchoolAdmin")
                .AddRequirements(new SameSchoolRequirement()));   // compares schoolId

            // ───── INSTRUCTOR  (role + same school) ─────
            options.AddPolicy("Instructor", policy => policy
                .RequireRole("Instructor")
                .AddRequirements(new SameSchoolRequirement()));   // compares schoolId

            // ───── STUDENT  (role + same school) ─────
            options.AddPolicy("Student", policy => policy
                .RequireRole("Student")
                .AddRequirements(new SameSchoolRequirement()));   // compares schoolId
        });

        // Register the custom handler once
        builder.Services.AddSingleton<IAuthorizationHandler, SameSchoolHandler>();

        // ─── Chain-of-Responsibility handlers (execution order) ───
        builder.Services.AddTransient<ITokenClaimHandler, CoreUserClaimsHandler>();
        builder.Services.AddTransient<ITokenClaimHandler, RoleClaimsHandler>();
        builder.Services.AddTransient<ITokenClaimHandler, SchoolClaimHandler>();

        // 3) Strategy pattern – register the default ACCESS-token generator.
        builder.Services.AddScoped<ITokenGenerator>(sp =>
            TokenGeneratorFactory.Create(TokenType.Access, sp));

        // 4) Refresh-token storage / validation service.
        builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // 5) HttpClient factory for external service communications
        builder.Services.AddHttpClient();

        // ─────────────────────────────── Rate-Limit / Cool-down ──────────────────────────────
        builder.Services.AddMemoryCache();

        builder.Services.Configure<IpRateLimitOptions>(opt =>
        {
            opt.EnableEndpointRateLimiting = true;
            opt.StackBlockedRequests = false;
            opt.GeneralRules = new List<RateLimitRule>
            {
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth", // login endpoint
            Limit    = 5,                // max 5 attempts
            Period   = "1m"              // per 1 minute window
        }
            };
        });

        builder.Services.AddInMemoryRateLimiting();
        builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        // ─────────────────────────────── MVC Controllers ─────────────────────────────────

        // 7. Register MVC controllers + JSON source-generated context.
        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                // insert our context at the start of the resolver chain
                o.JsonSerializerOptions.TypeInfoResolverChain.Insert(
                    0, AppJsonContext.Default);


            });

        // 8. Build the application instance.
        var app = builder.Build();

        // ─────────────────────────────── Database Seeding ────────────────────────────────
        // Run once at startup to ensure roles, admin user and initial data exist.
        using (var scope = app.Services.CreateScope())
        {
            SeedData.Initialize(scope.ServiceProvider);
        }

        // ───────────────────────────── Swagger / OpenAPI – Middleware ─────────────────────
        //    Swagger JSON available at /swagger/v1/swagger.json
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "DriveFlow CRM API v1");
            // c.RoutePrefix = string.Empty; // Serve UI at root if desired
        });

        // (Optional) Log the resolved connection string.
        app.Logger.LogInformation("Using connection string: {ConnectionString}", connectionString);

        // ──────────────────────────────── Request Pipeline ───────────────────────────────
        app.UseRouting();

        app.UseIpRateLimiting();
        app.UseAuthentication();            // Must precede UseAuthorization
        app.UseAuthorization();

#if DEBUG
        // Show detailed exception page in Debug builds
        app.UseDeveloperExceptionPage();
#endif

        // 9. Map controllers (no UseEndpoints needed in .NET 6 minimal hosting).
        app.MapControllers();

        // 10. Run the application (blocking call).
        app.Run();
    }
}