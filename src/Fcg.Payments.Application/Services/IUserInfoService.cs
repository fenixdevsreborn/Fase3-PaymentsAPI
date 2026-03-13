namespace Fcg.Payments.Application.Services;

/// <summary>Resolves user email/name for notifications (optional; can be from Users API or claims).</summary>
public interface IUserInfoService
{
    Task<(string? Email, string? Name)> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default);
}
