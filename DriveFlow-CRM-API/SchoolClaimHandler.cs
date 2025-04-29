using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DriveFlow_CRM_API.Authentication.Tokens.Handlers
{
    /// <summary>Adds <c>schoolId</c> claim.</summary>
    public sealed class SchoolClaimHandler : TokenClaimHandlerBase
    {
        public override void Handle(IdentityUser user,
                                    IList<string> roles,
                                    int schoolId,
                                    ICollection<Claim> claims)
        {
            claims.Add(new Claim("schoolId", schoolId.ToString()));
            base.Handle(user, roles, schoolId, claims);
        }
    }
}
