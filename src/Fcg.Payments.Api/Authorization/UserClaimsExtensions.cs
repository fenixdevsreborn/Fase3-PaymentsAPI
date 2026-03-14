using System.Security.Claims;
using Fcg.Payments.Api.Authentication;

namespace Fcg.Payments.Api.Authorization;

/// <summary>Extensions to get FCG claims from ClaimsPrincipal. User id always from JWT sub.</summary>
public static class UserClaimsExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst(FcgClaimTypes.Sub)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static string? GetRole(this ClaimsPrincipal user) =>
        user.FindFirst(FcgClaimTypes.Role)?.Value;

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        string.Equals(user.GetRole(), FcgRoles.Admin, StringComparison.OrdinalIgnoreCase);
}
