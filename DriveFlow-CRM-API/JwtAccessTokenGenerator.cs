using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DriveFlow_CRM_API.Authentication.Tokens.Handlers;   

namespace DriveFlow_CRM_API.Authentication.Tokens;

/// <summary>
/// Generates a signed JWT <strong>access-token</strong> for a given user and
/// their roles.  Secret is taken from the <c>JWT_KEY</c> environment variable;
/// if absent it falls back to <c>Jwt:Key</c> from configuration files.
/// </summary>
public sealed class JwtAccessTokenGenerator : ITokenGenerator
{
    private readonly IConfiguration _cfg;
    private readonly ITokenClaimHandler _pipeline;   // entry-point in the CoR

    /// <summary>
    /// DI constructor - receives configuration and the claim-builder chain.
    /// Handlers are wired in the order they are registered in <c>Program.cs</c>.
    /// </summary>
    public JwtAccessTokenGenerator(
        IConfiguration cfg,
        IEnumerable<ITokenClaimHandler> handlers)
    {
        _cfg = cfg;

        // ─────────────── Build the chain of responsibility ───────────────
        // Build chain in declared order
        ITokenClaimHandler? first = null;
        ITokenClaimHandler? current = null;

        foreach (var handler in handlers)
        {
            if (first is null)
            {
                first = current = handler;          // primul element
            }
            else
            {
                current = current!.SetNext(handler); // leagă și avansează
            }
        }

        _pipeline = first
            ?? throw new InvalidOperationException("No claim handlers registered");

    }

    public string GenerateToken(IdentityUser user, IList<string> roles, int schoolId)
    {
       // ─────────────── Guard-clauses: invalid input ───────────────
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        if (roles is null)
            throw new ArgumentNullException(nameof(roles));
        
       // ─────────────────────────────── Retrieve settings ───────────────────────────────
        var secret = _cfg["JWT_KEY"] ?? _cfg["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException(
                "JWT secret missing or too short (≥32 chars). Configure JWT_KEY env-var or Jwt:Key.");

        var jwtSection = _cfg.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiresMin = int.TryParse(jwtSection["AccessExpiresMinutes"], out var m) ? m : 60;

        // ──────────────────────────────── Build claims (CoR) ──────────────────────────────
        var claims = new List<Claim>();
        _pipeline.Handle(user, roles, schoolId, claims);

        // ──────────────────────────────── Create token ────────────────────────────────
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMin),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
