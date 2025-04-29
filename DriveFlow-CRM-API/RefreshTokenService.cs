using Microsoft.AspNetCore.Identity;
using DriveFlow_CRM_API.Models;


 namespace DriveFlow_CRM_API.Authentication;

/// <summary>
/// Contract for persisting and validating refresh-tokens that belong to an
/// <see cref="ApplicationUser" />.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Saves a refresh-token together with its UTC expiration timestamp.</summary>
    /// <param name="user">The account that owns the token.</param>
    /// <param name="token">JWT refresh-token string.</param>
    /// <param name="expires">Absolute expiration time in UTC.</param>
    Task StoreAsync(ApplicationUser user, string token, DateTime expires);

    /// <summary>
    /// Validates that <paramref name="token" /> matches the stored value and is not expired.
    /// </summary>
    /// <param name="user">The account that owns the token.</param>
    /// <param name="token">Refresh-token provided by the client.</param>
    /// <returns><see langword="true"/> if the token matches and is still valid.</returns>
    Task<bool> ValidateAsync(ApplicationUser user, string token);
}

/// <inheritdoc cref="IRefreshTokenService" />
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly UserManager<ApplicationUser> _users;

    /// <summary>DI constructor—receives the Identity <see cref="UserManager{TUser}" />.</summary>
    public RefreshTokenService(UserManager<ApplicationUser> users) => _users = users;

    // Keys used by ASP.NET Core Identity’s token store.
    private const string Provider = "DriveFlow";
    private const string NameToken = "Refresh";
    private const string NameExpiry = "RefreshExpires";

    /// <inheritdoc />
    public async Task StoreAsync(ApplicationUser user, string token, DateTime expires)
    {
        await _users.SetAuthenticationTokenAsync(user, Provider, NameToken, token);
        await _users.SetAuthenticationTokenAsync(
            user,
            Provider,
            NameExpiry,
            expires.ToUniversalTime().Ticks.ToString());
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(ApplicationUser user, string token)
    {
        var saved = await _users.GetAuthenticationTokenAsync(user, Provider, NameToken);
        var ticks = await _users.GetAuthenticationTokenAsync(user, Provider, NameExpiry);

        return saved == token &&
               long.TryParse(ticks, out var t) &&
               DateTime.UtcNow < new DateTime(t, DateTimeKind.Utc);
    }
}
