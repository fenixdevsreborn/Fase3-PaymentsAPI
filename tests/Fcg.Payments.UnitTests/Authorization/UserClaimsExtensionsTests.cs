using System.Security.Claims;
using Fcg.Payments.Api.Authentication;
using Fcg.Payments.Api.Authorization;
using Xunit;

namespace Fcg.Payments.UnitTests.Authorization;

public class UserClaimsExtensionsTests
{
    [Fact]
    public void GetUserId_WhenSubIsValidGuid_ReturnsGuid()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(FcgClaimTypes.Sub, id.ToString())
        }, "Test"));

        var result = principal.GetUserId();

        Assert.Equal(id, result);
    }

    [Fact]
    public void GetUserId_WhenSubMissing_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "Test"));

        var result = principal.GetUserId();

        Assert.Null(result);
    }

    [Fact]
    public void IsAdmin_WhenRoleAdmin_ReturnsTrue()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(FcgClaimTypes.Role, FcgRoles.Admin)
        }, "Test"));

        Assert.True(principal.IsAdmin());
    }

    [Fact]
    public void CanAccessResource_WhenAdmin_ReturnsTrue()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(FcgClaimTypes.Role, FcgRoles.Admin),
            new Claim(FcgClaimTypes.Sub, Guid.NewGuid().ToString())
        }, "Test"));

        Assert.True(principal.CanAccessResource(Guid.NewGuid()));
    }

    [Fact]
    public void CanAccessResource_WhenOwner_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(FcgClaimTypes.Sub, userId.ToString()),
            new Claim(FcgClaimTypes.Role, FcgRoles.User)
        }, "Test"));

        Assert.True(principal.CanAccessResource(userId));
    }
}
