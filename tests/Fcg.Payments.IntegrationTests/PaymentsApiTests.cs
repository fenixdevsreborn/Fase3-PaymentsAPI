using System.Net;
using System.Net.Http.Json;
using Fcg.Payments.Contracts.Payments;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Fcg.Payments.IntegrationTests;

public class PaymentsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PaymentsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/payments", new CreatePaymentRequest { GameId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/payments/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/payments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
