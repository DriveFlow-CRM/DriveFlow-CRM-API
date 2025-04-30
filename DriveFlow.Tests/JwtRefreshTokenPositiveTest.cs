using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;
using DriveFlow_CRM_API.Authentication.Tokens;
namespace DriveFlow.Tests;

/// <summary>
/// Happy-path integration test for <see cref="JwtRefreshTokenGenerator"/>.
/// Verifies that a refresh-token is produced and is at least 32 bytes long
/// (≈ 43 Base-64 characters).
/// </summary>
public sealed class JwtRefreshTokenPositiveTest
{
    private static IConfiguration BuildConfig() =>
    new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('x', 32), // 256-bit dummy key
            ["Jwt:RefreshExpiresDays"] = "14",               // two-week TTL
            ["Jwt:Issuer"] = "DriveFlow.Api",     
            ["Jwt:Audience"] = "DriveFlow.Spa"     
        })
        .Build();

    [Fact]
    public void RefreshToken_Has_Expected_Minimum_Length()
    {
        // Arrange
        var cfg = BuildConfig();
        var sut = new JwtRefreshTokenGenerator(cfg);
        var user = new IdentityUser { Id = "1", Email = "a@b.com" };

        // Act
        string token = sut.GenerateToken(
            user,
            roles: new List<string>(),  // pass an empty list if roles aren’t used
            schoolId: 0);

        // Assert – 32 raw bytes → Base-64 string ≥ 43 characters (44 if padded with “==”)
        token.Length.Should().BeGreaterThanOrEqualTo(43); // Corrected method name
        token.Should().NotBeNullOrWhiteSpace();
    }
}
