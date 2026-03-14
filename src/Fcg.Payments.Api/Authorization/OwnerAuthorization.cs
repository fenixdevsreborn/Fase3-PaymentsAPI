using System.Security.Claims;
using Fcg.Payments.Api.Authentication;

namespace Fcg.Payments.Api.Authorization;

/// <summary>Owner or admin may access the resource.</summary>
public static class OwnerAuthorization
{
    public static bool CanAccessResource(this ClaimsPrincipal user, Guid resourceOwnerId)
    {
        if (user.IsAdmin()) return true;
        var userId = user.GetUserId();
        return userId.HasValue && userId.Value == resourceOwnerId;
    }
}
