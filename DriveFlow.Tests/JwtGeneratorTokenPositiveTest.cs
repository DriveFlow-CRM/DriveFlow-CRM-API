using System.IdentityModel.Tokens.Jwt;
using System.Text;

using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

using DriveFlow_CRM_API.Authentication.Tokens;
using DriveFlow_CRM_API.Authentication.Tokens.Handlers;

namespace DriveFlow.Tests;

/// <summary>
/// Integration‑test that verifies <see cref="JwtAccessTokenGenerator"/> together with the complete
/// chain‑of‑responsibility of <see cref="ITokenClaimHandler"/> implementations.
/// </summary>
public sealed class JwtGeneratorTests
{
    private readonly ITestOutputHelper _out;

    /// <summary>
    /// ctor injected de xUnit cu <see cref="ITestOutputHelper"/> 
    /// </summary>
    public JwtGeneratorTests(ITestOutputHelper output) => _out = output;

    // ───────────────────────  Access‑token validity  ───────────────────────

    /// <summary>
    /// Generates an access‑token for a dummy user and asserts that:
    /// <list type="bullet">
    ///   <item><description>all custom claims injected by the handlers exist exactly once;</description></item>
    ///   <item><description>JWT header, issuer, audience and lifetime are correct;</description></item>
    ///   <item><description>token signature validates with the configured secret.</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public void AccessToken_Contains_All_Expected_Metadata_And_Custom_Claims()
    {
        // ─────────────────────── Arrange ───────────────────────
        const string issuer = "DriveFlow.Api";
        const string audience = "DriveFlow.Spa";
        const string secret = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"; // dummy 32‑char key
        const int ttlMin = 60;

        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = issuer,
                ["Jwt:Audience"] = audience,
                ["Jwt:Key"] = secret,
                ["Jwt:AccessExpiresMinutes"] = ttlMin.ToString()
            })
            .Build();

        ITokenClaimHandler[] handlers =
        {
            new CoreUserClaimsHandler(),
            new RoleClaimsHandler(),
            new SchoolClaimHandler()
        };

        var sut = new JwtAccessTokenGenerator(cfg, handlers);
        var user = new IdentityUser { Id = "42", Email = "a@b.com" };
        var roles = new List<string> { "Instructor" };

        // ─────────────────────── Act ───────────────────────
        string jwt = sut.GenerateToken(user, roles, schoolId: 7);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        _out.WriteLine("Generated JWT:\n" + jwt);

        // ───────────────────────  Assert – custom claims ───────────────────────
        token.Claims.Should().ContainSingle(c => c.Type == "sub" && c.Value == "42");
        token.Claims.Should().ContainSingle(c => c.Type == "userId" && c.Value == "42");
        token.Claims.Should().ContainSingle(c => c.Type == "userEmail" && c.Value == "a@b.com");
        token.Claims.Should().ContainSingle(c => c.Type == "userRole" && c.Value == "Instructor");
        token.Claims.Should().ContainSingle(c => c.Type == "schoolId" && c.Value == "7");

        // ─────────────────────── Assert – header & registered ───────────────────────
        token.Header.Alg.Should().Be("HS256");
        token.Header.Typ.Should().Be("JWT");
        token.Issuer.Should().Be(issuer);
        token.Audiences.Should().Contain(audience);
        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(ttlMin), TimeSpan.FromSeconds(5));

        // ─────────────────────── Assert – signature ───────────────────────
        var tvp = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        new Action(() => new JwtSecurityTokenHandler().ValidateToken(jwt, tvp, out _))
            .Should().NotThrow();

        // ─────────────────────── Assert – no duplicates ───────────────────────
        token.Claims.Where(c => c.Type == "sub").Should().HaveCount(1);
        token.Claims.Where(c => c.Type == "userId").Should().HaveCount(1);
    }
}
