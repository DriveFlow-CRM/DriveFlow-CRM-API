using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

using DriveFlow_CRM_API.Authentication.Tokens;
using DriveFlow_CRM_API.Authentication.Tokens.Handlers;

namespace DriveFlow.Tests;

/// <summary>
/// Negative-path tests for <see cref="JwtAccessTokenGenerator"/>.
/// Each test compiles successfully and fails only at runtime,
/// verifying that defensive checks are triggered as expected.
/// </summary>
public sealed class JwtGeneratorTokenNegativeTest
{
    // ───────── Helper to build a minimal in-memory IConfiguration ─────────
    private static IConfiguration BuildConfig(Action<IDictionary<string, string?>>? mutate = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "DriveFlow.Api",
            ["Jwt:Audience"] = "DriveFlow.Spa",
            ["Jwt:Key"] = new string('x', 32),   // 32-character dummy key
            ["Jwt:AccessExpiresMinutes"] = "60"
        };
        mutate?.Invoke(dict);
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    // ───────── Missing or too-short secret ─────────
    [Fact]
    public void GenerateToken_ShouldThrow_When_Secret_Is_Missing_Or_Short()
    {
        var cfg = BuildConfig(d => d["Jwt:Key"] = ""); // invalid secret (empty)
        var sut = new JwtAccessTokenGenerator(cfg, new[] { new CoreUserClaimsHandler() });

        Action act = () => sut.GenerateToken(new IdentityUser { Id = "1" }, new List<string>(), schoolId: 1);

        act.Should().Throw<InvalidOperationException>();
    }

    // ───────── No claim-handlers registered ─────────
    [Fact]
    public void Ctor_ShouldThrow_When_No_Claim_Handlers()
    {
        var cfg = BuildConfig();

        Action act = () => new JwtAccessTokenGenerator(cfg, Array.Empty<ITokenClaimHandler>());

        act.Should().Throw<InvalidOperationException>();
    }

    // ───────── Invalid signature ─────────
    [Fact]
    public void ValidateToken_ShouldFail_With_Invalid_Signature()
    {
        const string wrongKey = "abcdefghijklmnopqrstuvwxabcdefghijkl";

        var cfg = BuildConfig();
        var sut = new JwtAccessTokenGenerator(cfg, new[] { new CoreUserClaimsHandler() });
        string jwt = sut.GenerateToken(new IdentityUser { Id = "42" }, new List<string>(), schoolId: 0);

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = "DriveFlow.Api",
            ValidAudience = "DriveFlow.Spa",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(wrongKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(jwt, tvp, out _);

        act.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    // ───────── Expired token ─────────
    [Fact]
    public void ValidateToken_ShouldFail_When_Token_Expired()
    {
        const string key = "12345678901234567890123456789012";

        var cfg = BuildConfig(d => d["Jwt:AccessExpiresMinutes"] = "-1"); // already expired
        cfg["Jwt:Key"] = key;

        var sut = new JwtAccessTokenGenerator(cfg, new[] { new CoreUserClaimsHandler() });
        string jwt = sut.GenerateToken(new IdentityUser { Id = "9" }, new List<string>(), schoolId: 0);

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = "DriveFlow.Api",
            ValidAudience = "DriveFlow.Spa",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero // no grace period
        };

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(jwt, tvp, out _);

        act.Should().Throw<SecurityTokenExpiredException>();
    }

    // ───────── Wrong issuer ─────────
    [Fact]
    public void ValidateToken_ShouldFail_When_Issuer_Wrong()
    {
        var cfg = BuildConfig();
        var sut = new JwtAccessTokenGenerator(cfg, new[] { new CoreUserClaimsHandler() });
        string jwt = sut.GenerateToken(new IdentityUser { Id = "8" }, new List<string>(), schoolId: 0);

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = "Another.Issuer",
            ValidAudience = "DriveFlow.Spa",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 32))),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(jwt, tvp, out _);

        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    // ───────── Wrong audience ─────────
    [Fact]
    public void ValidateToken_ShouldFail_When_Audience_Wrong()
    {
        var cfg = BuildConfig();
        var sut = new JwtAccessTokenGenerator(cfg, new[] { new CoreUserClaimsHandler() });
        string jwt = sut.GenerateToken(new IdentityUser { Id = "10" }, new List<string>(), schoolId: 0);

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = "DriveFlow.Api",
            ValidAudience = "OtherAudience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 32))),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(jwt, tvp, out _);

        act.Should().Throw<SecurityTokenInvalidAudienceException>();
    }

    // ───────── Null user argument ─────────
    [Fact]
    public void GenerateToken_ShouldThrow_When_User_Is_Null()
    {
        var cfg = BuildConfig();
        var sut = new JwtAccessTokenGenerator(cfg, new[] { new CoreUserClaimsHandler() });

        Action act = () => sut.GenerateToken(user: null!,
                                             roles: new List<string>(),
                                             schoolId: 0);

        act.Should().Throw<ArgumentNullException>();
    }

    // ───────── Null roles list ─────────
    [Fact]
    public void GenerateToken_ShouldThrow_When_Roles_List_Is_Null()
    {
        var cfg = BuildConfig();
        var sut = new JwtAccessTokenGenerator(cfg, new[] { new CoreUserClaimsHandler() });
        var user = new IdentityUser { Id = "11", Email = "a@b.com" };

        Action act = () => sut.GenerateToken(user, roles: null!, schoolId: 0);

        act.Should().Throw<ArgumentNullException>();
    }

    // ───────── Token with future Not-Before (nbf) ─────────
    [Fact]
    public void ValidateToken_ShouldFail_When_Token_NotYetValid()
    {
        // key & signing creds
        string keyStr = new string('x', 32);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // build a token that becomes valid in 10 minutes
        var futureToken = new JwtSecurityToken(
            issuer: "DriveFlow.Api",
            audience: "DriveFlow.Spa",
            claims: null,
            notBefore: DateTime.UtcNow.AddMinutes(10),
            expires: DateTime.UtcNow.AddMinutes(70),
            signingCredentials: creds);

        string jwt = new JwtSecurityTokenHandler().WriteToken(futureToken);

        var tvp = new TokenValidationParameters
        {
            ValidIssuer = "DriveFlow.Api",
            ValidAudience = "DriveFlow.Spa",
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero // strict
        };

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(jwt, tvp, out _);

        act.Should().Throw<SecurityTokenNotYetValidException>();
    }

}
