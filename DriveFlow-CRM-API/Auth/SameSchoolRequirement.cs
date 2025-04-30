
using Microsoft.AspNetCore.Authorization;

namespace DriveFlow_CRM_API.Auth;

/// <summary>
/// Requirement: the user must belong to the same school as in the route.
/// </summary>
public sealed class SameSchoolRequirement : IAuthorizationRequirement { }
