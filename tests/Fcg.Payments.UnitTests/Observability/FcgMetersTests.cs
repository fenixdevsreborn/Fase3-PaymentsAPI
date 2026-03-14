using Fcg.Payments.Api.Observability;
using Xunit;

namespace Fcg.Payments.UnitTests.Observability;

public class FcgMetersTests
{
    [Fact]
    public void Constructor_CreatesMeterWithName()
    {
        var meters = new FcgMeters("Fcg.Payments.Api.Test");

        Assert.NotNull(meters.Meter);
        Assert.Equal("Fcg.Payments.Api.Test", meters.Meter.Name);
    }

    [Fact]
    public void RecordPaymentCreated_DoesNotThrow()
    {
        var meters = new FcgMeters("Test");
        meters.RecordPaymentCreated();
    }

    [Fact]
    public void RecordPaymentPaid_DoesNotThrow()
    {
        var meters = new FcgMeters("Test");
        meters.RecordPaymentPaid();
    }

    [Fact]
    public void RecordPaymentFailed_DoesNotThrow()
    {
        var meters = new FcgMeters("Test");
        meters.RecordPaymentFailed();
    }
}
