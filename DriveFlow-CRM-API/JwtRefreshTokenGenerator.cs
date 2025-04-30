using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DriveFlow_CRM_API.Authentication.Tokens
{
    /// <summary>
    /// Generates signed JWT <strong>refresh-tokens</strong> for ASP.NET Core Identity users.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>The signing key is read from the <c>JWT_KEY</c> environment variable, or <c>Jwt:Key</c> in <c>appsettings.json</c>.</item>
    ///   <item><c>Jwt:Issuer</c>, <c>Jwt:Audience</c> and a positive integer <c>Jwt:RefreshExpiresDays</c> are required.</item>
    ///   <item>The token includes only <c>sub</c>, <c>jti</c> and a <c>typ=refresh</c> claim.</item>
    /// </list>
    /// </remarks>
    public sealed class JwtRefreshTokenGenerator : ITokenGenerator
    {
        private const int MinSecretLength = 32;      // 256-bit HMAC
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secret;
        private readonly int _expiresDays;

        /// <summary>Creates the generator and validates configuration.</summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any mandatory JWT setting is missing or invalid.
        /// </exception>
        public JwtRefreshTokenGenerator(IConfiguration cfg)
        {
            _secret = cfg["JWT_KEY"] ?? cfg["Jwt:Key"]
                      ?? throw new InvalidOperationException("JWT secret key is missing.");

            if (_secret.Length < MinSecretLength)
                throw new InvalidOperationException(
                    $"JWT secret must be at least {MinSecretLength} characters.");

            IConfigurationSection jwt = cfg.GetSection("Jwt");

            _issuer = jwt["Issuer"]
                        ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
            _audience = jwt["Audience"]
                        ?? throw new InvalidOperationException("Jwt:Audience is missing.");

            if (!int.TryParse(jwt["RefreshExpiresDays"], out _expiresDays) || _expiresDays <= 0)
                throw new InvalidOperationException(
                    "Jwt:RefreshExpiresDays must be a positive integer.");
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="user"/> or <paramref name="roles"/> is <c>null</c>.
        /// </exception>
        public string GenerateToken(IdentityUser user, IList<string> roles, int schoolId)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (roles is null) throw new ArgumentNullException(nameof(roles));
            // roles and schoolId are kept for API symmetry but not used.

            // ───── claims ─────
            Claim[] claims =
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("typ", "refresh")
            };

            // ───── signing ─────
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_secret));
            SigningCredentials sig = new(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(_expiresDays),
                signingCredentials: sig);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
