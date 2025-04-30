using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;
using DriveFlow_CRM_API.Authentication.Tokens;

namespace DriveFlow.Tests;

public sealed class JwtRefreshTokenNegativeTest
{
    // ──────────────────────────────────────────────
    //  Build a *valid* config, so tests can break
    //  exactly the setting they target.
    // ──────────────────────────────────────────────
    private static IConfiguration BuildConfig(
        Action<IDictionary<string, string?>>? mutate = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('x', 32),
            ["Jwt:RefreshExpiresDays"] = "7",
            ["Jwt:Issuer"] = "DriveFlow.Api",
            ["Jwt:Audience"] = "DriveFlow.Spa"
        };

        mutate?.Invoke(dict);                         // allow each test to override
        return new ConfigurationBuilder()
                 .AddInMemoryCollection(dict)
                 .Build();
    }

    // ───────── constructor validation ─────────

    [Fact] // empty / missing secret
    public void Ctor_Throws_When_Secret_Missing()
    {
        var cfg = BuildConfig(d => d["Jwt:Key"] = "");
        FluentActions.Invoking(() => new JwtRefreshTokenGenerator(cfg))
                     .Should().Throw<InvalidOperationException>();
    }

    [Fact] // TTL not numeric
    public void Ctor_Throws_When_Ttl_Not_Integer()
    {
        var cfg = BuildConfig(d => d["Jwt:RefreshExpiresDays"] = "abc");
        FluentActions.Invoking(() => new JwtRefreshTokenGenerator(cfg))
                     .Should().Throw<InvalidOperationException>();
    }

    // ───────── GenerateToken guards ─────────

    [Fact] // user == null
    public void GenerateToken_Throws_When_User_Null()
    {
        var sut = new JwtRefreshTokenGenerator(BuildConfig());

        FluentActions.Invoking(() => sut.GenerateToken(
                                   user: null!,
                                   roles: new List<string>(),
                                   schoolId: 0))
                     .Should().Throw<ArgumentNullException>();
    }

    [Fact] // roles == null
    public void GenerateToken_Throws_When_Roles_Null()
    {
        var sut = new JwtRefreshTokenGenerator(BuildConfig());
        var user = new IdentityUser { Id = "42", Email = "a@b.com" };

        FluentActions.Invoking(() => sut.GenerateToken(
                                   user,
                                   roles: null!,
                                   schoolId: 0))
                     .Should().Throw<ArgumentNullException>();
    }
}
