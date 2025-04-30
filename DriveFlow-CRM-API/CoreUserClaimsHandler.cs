using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DriveFlow_CRM_API.Authentication.Tokens.Handlers
{
    /// <summary>Adds sub, userId, userEmail.</summary>
    public sealed class CoreUserClaimsHandler : TokenClaimHandlerBase
    {
        public override void Handle(IdentityUser user,
                                    IList<string> roles,
                                    int schoolId,
                                    ICollection<Claim> claims)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (roles is null) throw new ArgumentNullException(nameof(roles));
            if (claims is null) throw new ArgumentNullException(nameof(claims));

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim("userId", user.Id));
            claims.Add(new Claim("userEmail", user.Email ?? string.Empty));
            base.Handle(user, roles, schoolId, claims);
        }
    }
}
