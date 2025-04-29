using DriveFlow_CRM_API.Authentication.Tokens;
using DriveFlow_CRM_API.Authentication;
using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

/// <summary>
/// Authentication endpoints (login &amp; refresh) for the DriveFlow CRM API.
/// </summary>
/// <remarks>
/// <para>
/// Exposes two endpoints:
/// <list type="bullet">
///   <item>
///     <description>
///       <c>POST /api/auth</c> &mdash; verifies credentials and returns an
///       <strong>access‑token</strong> plus a <strong>refresh‑token</strong>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>POST /api/auth/refresh</c> &mdash; exchanges a valid refresh‑token for a new
///       access‑token without re‑authenticating.
///     </description>
///   </item>
/// </list>
/// </para>
/// Tokens are signed according to the algorithm configured in <c>Jwt:Algorithm</c>
/// (default <c>HS256</c>). All timestamps are expressed in UTC.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ITokenGenerator _tok;       // Access‑token generator
    private readonly IRefreshTokenService _rtok; // Refresh‑token persistence
    private readonly IConfiguration _cfg;

    /// <summary>
    /// Constructor injected by the framework with request‑scoped services.
    /// </summary>
    public AuthController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ITokenGenerator tok,
        IRefreshTokenService rtok,
        IConfiguration cfg)
    {
        _users = users;
        _signIn = signIn;
        _tok = tok;
        _rtok = rtok;
        _cfg = cfg;
    }

    // ─────────────────────────   LOGIN   ─────────────────────────
    /// <summary>
    /// Authenticates a user and returns an access-token plus a refresh-token.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>
    ///     Secret key is read from <c>JWT_KEY</c> (env) or <c>Jwt:Key</c> (appsettings).
    ///   </description></item>
    ///   <item><description>
    ///     <b>Sample request</b>:<br/>
    ///     <code>
    ///     POST /api/auth<br/>
    ///     {<br/>
    ///       &quot;email&quot;: &quot;student@test.com&quot;,<br/>
    ///       &quot;password&quot;: &quot;Student231!&quot;<br/>
    ///     }
    ///     </code>
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <param name="dto">Credentials (e-mail &amp; password).</param>
    /// <response code="200">Login successful – returns tokens and user metadata.</response>
    /// <response code="401">E-mail or password incorrect.</response>
    /// <response code="404">Account not found.</response>
    [HttpPost]
    public async Task<IActionResult> LoginAsync(LoginDto dto)
    {
        // 1. Locate the account
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null)
            return NotFound(new { error = 404, message = "Nu exista cont!" });

        // 2. Verify password
        var valid = await _signIn.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!valid.Succeeded)
            return Unauthorized(new { error = 401, message = "Emailul sau parola e gresita!" });

        // 3. Fetch the single role assigned to this account
        var roles = await _users.GetRolesAsync(user);
        var role = roles.Count > 0 ? roles[0] : "Student"; // Fallback for safety

        // 4. Domain‑specific data (placeholder until school relationship exists)
        var schoolId = 0;

        // 5. Generate ACCESS token
        var accessTok = _tok.GenerateToken(user, roles, schoolId);

        // 6. Generate REFRESH token (can use different lifetime / key)
        var refreshGen = TokenGeneratorFactory.Create(TokenType.Refresh, HttpContext.RequestServices);
        var refreshTok = refreshGen.GenerateToken(user, roles, schoolId);
        var refreshExp = DateTime.UtcNow.AddDays(int.Parse(_cfg["Jwt:RefreshExpiresDays"]!));
        await _rtok.StoreAsync(user, refreshTok, refreshExp);

        // 7. Build response DTO
        var response = new
        {
            token = accessTok,
            refreshToken = refreshTok,
            expiresIn = int.Parse(_cfg["Jwt:AccessExpiresMinutes"]!) * 60,
            userId = user.Id,
            userType = role,
            userEmail = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            userPhone = user.PhoneNumber,
            schoolId
        };

        // (Optional) Send token in Authorization header as well, for convenience
        Response.Headers["Authorization"] = $"Bearer {accessTok}";

        return Ok(response);
    }

    // ─────────────────────────   REFRESH   ─────────────────────────
    /// <summary>
    /// Exchanges a valid refresh‑token for a new access‑token.
    /// </summary>
    /// <param name="dto">Payload containing the refresh‑token.</param>
    /// <response code="200">New access‑token issued.</response>
    /// <response code="401">Refresh‑token invalid or user no longer exists.</response>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync(RefreshDto dto)
    {
        // 1. Extract userId (sub claim) from the JWT
        var jwtObj = new JwtSecurityTokenHandler().ReadJwtToken(dto.RefreshToken);
        var userId = jwtObj.Subject;

        var user = await _users.FindByIdAsync(userId!);
        if (user is null)
            return Unauthorized();

        // 2. Validate the refresh‑token against the store
        var ok = await _rtok.ValidateAsync(user, dto.RefreshToken);
        if (!ok)
            return Unauthorized();

        // 3. Issue a new ACCESS token
        var roles = await _users.GetRolesAsync(user);
        var schoolId = 0; // Same placeholder as above
        var newAccess = _tok.GenerateToken(user, roles, schoolId);

        // Optional header for convenience
        Response.Headers["Authorization"] = $"Bearer {newAccess}";

        return Ok(new { token = newAccess });
    }
}

/// <summary>
/// DTO used for login credentials.
/// </summary>
/// <param name="Email">E‑mail address (UserName in Identity).</param>
/// <param name="Password">Account password in plain text.</param>
public record LoginDto(string Email, string Password);

/// <summary>
/// DTO used to request a new access‑token using a refresh‑token.
/// </summary>
/// <param name="RefreshToken">The previously issued refresh‑token.</param>
public record RefreshDto(string RefreshToken);