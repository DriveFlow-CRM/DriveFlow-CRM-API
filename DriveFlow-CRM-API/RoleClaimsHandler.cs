using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DriveFlow_CRM_API.Authentication.Tokens.Handlers
{
    /// <summary>Adds one <c>role</c> claim per role.</summary>
    public sealed class RoleClaimsHandler : TokenClaimHandlerBase
    {
        public override void Handle(IdentityUser user,
                                    IList<string> roles,
                                    int schoolId,
                                    ICollection<Claim> claims)
        {
            foreach (var r in roles)
            {
                claims.Add(new Claim("userRole", r));
                claims.Add(new Claim(ClaimTypes.Role, r));
            }
            base.Handle(user, roles, schoolId, claims);
        }
    }
}
