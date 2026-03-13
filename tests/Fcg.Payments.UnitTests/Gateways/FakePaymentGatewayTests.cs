using Fcg.Payments.Infrastructure.Gateways;
using Xunit;

namespace Fcg.Payments.UnitTests.Gateways;

public class FakePaymentGatewayTests
{
    private readonly FakePaymentGateway _sut = new();

    [Fact]
    public void ProviderName_ReturnsFake()
    {
        Assert.Equal("Fake", _sut.ProviderName);
    }

    [Fact]
    public async Task AuthorizeAsync_ReturnsSuccess_WithReference()
    {
        var id = Guid.NewGuid();
        var result = await _sut.AuthorizeAsync(id, 99.99m, "BRL", default);

        Assert.True(result.Success);
        Assert.NotNull(result.ProviderReference);
        Assert.Contains(id.ToString("N"), result.ProviderReference);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CaptureAsync_ReturnsSuccess()
    {
        var result = await _sut.CaptureAsync("ref-123", default);
        Assert.True(result.Success);
        Assert.Equal("ref-123", result.ProviderReference);
    }

    [Fact]
    public async Task FailAsync_ReturnsSuccess()
    {
        var result = await _sut.FailAsync("ref-123", "reason", default);
        Assert.True(result.Success);
    }
}
