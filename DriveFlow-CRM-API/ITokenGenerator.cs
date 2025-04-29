using Microsoft.AspNetCore.Identity;

namespace DriveFlow_CRM_API.Authentication.Tokens;

/// <summary>
/// Defines a component that can generate a signed security token
/// (typically a JWT) for an <see cref="IdentityUser" />.
/// </summary>
/// <remarks>
/// Implementations such as <c>JwtAccessTokenGenerator</c> or
/// <c>JwtRefreshTokenGenerator</c> decide how the token is built,
/// what claims are included, and how it is cryptographically signed.
/// </remarks>
public interface ITokenGenerator
{
    /// <summary>
    /// Creates a token that represents <paramref name="user" />.
    /// </summary>
    /// <param name="user">Identity account for which the token is issued.</param>
    /// <param name="roles">Roles assigned to the user and embedded as claims.</param>
    /// <param name="schoolId">
    /// Optional school identifier to include as a custom claim (use <c>0</c> if n/a).
    /// </param>
    /// <returns>The serialized token string (e.g., compact JWT).</returns>
    string GenerateToken(IdentityUser user, IList<string> roles, int schoolId);
}
