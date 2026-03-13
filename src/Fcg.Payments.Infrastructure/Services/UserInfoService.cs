using Fcg.Payments.Application.Services;

namespace Fcg.Payments.Infrastructure.Services;

/// <summary>Stub: returns null until Users API client is integrated. Email/name can also be taken from JWT claims.</summary>
public class UserInfoService : IUserInfoService
{
    public Task<(string? Email, string? Name)> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<(string?, string?)>((null, null));
    }
}
