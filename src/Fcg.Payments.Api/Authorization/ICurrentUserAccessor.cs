namespace Fcg.Payments.Api.Authorization;

/// <summary>Current authenticated user from JWT (sub).</summary>
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
    bool IsAdmin { get; }
}
