namespace Fcg.Payments.Api.Authentication;

/// <summary>Authorization policy names. Same as Users API.</summary>
public static class FcgPolicies
{
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";
    public const string RequireAdmin = "RequireAdmin";
}
