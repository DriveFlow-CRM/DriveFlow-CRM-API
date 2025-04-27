using System.Security.Claims;
using Microsoft.AspNetCore.Identity;              

namespace DriveFlow_CRM_API.Authentication.Tokens.Handlers;  

// ───────────────────────  Claim-handler chain ───────────────────────

/// <summary>
/// Link in the JWT claim-building pipeline.
/// </summary>
public interface ITokenClaimHandler
{
    /// <summary>Sets the next handler in the chain.</summary>
    ITokenClaimHandler SetNext(ITokenClaimHandler next);

    /// <summary>Adds custom claims for the specified user.</summary>
    /// <param name="user">Identity user whose claims are built.</param>
    /// <param name="roles">Roles already resolved for the user.</param>
    /// <param name="schoolId">School context identifier.</param>
    /// <param name="claims">Mutable collection to which new claims are appended.</param>
    void Handle(
        IdentityUser user,
        IList<string> roles,
        int schoolId,
        ICollection<Claim> claims);
}

/// <summary>
/// Base class that wires the chain and forwards to the next handler.
/// </summary>
public abstract class TokenClaimHandlerBase : ITokenClaimHandler
{
    private ITokenClaimHandler? _next;

    /// <inheritdoc/>
    public ITokenClaimHandler SetNext(ITokenClaimHandler next)
    {
        _next = next;
        return next;
    }

    /// <inheritdoc/>
    public virtual void Handle(
        IdentityUser user,
        IList<string> roles,
        int schoolId,
        ICollection<Claim> claims) =>
        _next?.Handle(user, roles, schoolId, claims);
}
