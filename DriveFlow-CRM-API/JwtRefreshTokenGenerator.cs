using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace DriveFlow_CRM_API.Authentication.Tokens;

/// <summary>
/// Generates signed JWT <strong>refresh-tokens</strong> for ASP.NET Core Identity users.
/// </summary>
/// <remarks>
/// <para>
/// The secret signing key is read from <c>JWT_KEY</c> (environment variable) and
/// falls back to <c>Jwt:Key</c> in <c>appsettings.json</c>.  
/// Expiration in days is taken from <c>Jwt:RefreshExpiresDays</c>; if missing,
/// the factory defaults to seven days.
/// </para>
/// </remarks>
    public sealed class JwtRefreshTokenGenerator : ITokenGenerator
    {
        private readonly IConfiguration _cfg;

        /// <summary>Initializes the generator with application configuration.</summary>
        public JwtRefreshTokenGenerator(IConfiguration cfg) => _cfg = cfg;

        /// <inheritdoc />
        public string GenerateToken(IdentityUser user, IList<string> roles, int schoolId)
        {
            // ─────────────── Retrieve signing settings ───────────────
            var secret = _cfg["JWT_KEY"] ?? _cfg["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT secret missing or shorter than 32 characters (256 bits).");
            }

            var jwtSection = _cfg.GetSection("Jwt");
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiresDays = int.TryParse(jwtSection["RefreshExpiresDays"], out var d) ? d : 7;

            // ─────────────── Create claims ───────────────
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id)
            };

            // ─────────────── Sign and emit token ───────────────
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expiresDays),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

