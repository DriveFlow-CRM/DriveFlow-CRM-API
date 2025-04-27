using DriveFlow_CRM_API.Authentication.Tokens.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace DriveFlow_CRM_API.Authentication.Tokens;

/// <summary>
/// Distinguishes between the two JWT flavours issued by the API.
/// </summary>
public enum TokenType
{
    /// <summary>Short-lived token placed in the <c>Authorization</c> header.</summary>
    Access,

    /// <summary>Long-lived token used to obtain a fresh <see cref="Access"/> token.</summary>
    Refresh
}

/// <summary>
/// Factory that creates a concrete <see cref="ITokenGenerator"/> for the requested
/// <see cref="TokenType" />.
/// </summary>
public static class TokenGeneratorFactory
{
    /// <summary>
    /// Returns an <see cref="ITokenGenerator" /> implementation suited to
    /// <paramref name="type" />.
    /// </summary>
    /// <param name="type">
    /// Kind of token to generate (<see cref="TokenType.Access" /> or
    /// <see cref="TokenType.Refresh" />).
    /// </param>
    /// <param name="sp">
    /// <see cref="IServiceProvider" /> – used to resolve configuration and the claim
    /// handlers (Chain of Responsibility) registered in DI.
    /// </param>
    /// <returns>
    /// An instance of <see cref="JwtAccessTokenGenerator" /> or
    /// <see cref="JwtRefreshTokenGenerator" />.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if an unrecognised <paramref name="type" /> is supplied.
    /// </exception>
    public static ITokenGenerator Create(TokenType type, IServiceProvider sp) =>
        type switch
        {
            TokenType.Access => new JwtAccessTokenGenerator(
                                     sp.GetRequiredService<IConfiguration>(),
                                     sp.GetServices<ITokenClaimHandler>()),   
            TokenType.Refresh => new JwtRefreshTokenGenerator(
                                     sp.GetRequiredService<IConfiguration>()),
            _ => throw new NotSupportedException($"Unsupported token type: {type}")
        };
}
