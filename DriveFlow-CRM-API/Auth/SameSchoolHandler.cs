using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DriveFlow_CRM_API.Auth;

/// <summary>
/// Validates that the <c>schoolId</c> contained in the JWT matches the one in the URL
/// (works with both MVC filters and endpoint routing).
/// </summary>
public sealed class SameSchoolHandler
    : AuthorizationHandler<SameSchoolRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameSchoolRequirement requirement)
    {
        var claimId = context.User.FindFirstValue("schoolId");
        if (claimId is null) return Task.CompletedTask;

        string? routeId = null;

        // ── 1. Endpoint routing (most cases) ──
        if (context.Resource is Microsoft.AspNetCore.Http.HttpContext http)
            routeId = http.GetRouteData().Values["schoolId"]?.ToString();

        // ── 2. MVC / Razor Pages (fallback) ──
        else if (context.Resource is AuthorizationFilterContext mvc)
            routeId = mvc.RouteData.Values["schoolId"]?.ToString();

        if (routeId == claimId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
