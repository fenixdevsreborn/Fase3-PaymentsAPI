using System.Net;
using System.Net.Http.Json;
using Fcg.Payments.Contracts.Payments;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Fcg.Payments.IntegrationTests;

public class PaymentsApiTests : IClassFixture<WebAppFixture>
{
    private readonly HttpClient _client;

    public PaymentsApiTests(WebAppFixture factory)
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

    [Fact(Skip = "JWT Authority no teste depende da ordem de config do WebApplicationFactory (Minimal Hosting). Use appsettings.Testing.json com Authority fixa ou valide manualmente com Users API.")]
    public async Task GetMe_WithValidToken_Returns200()
    {
        var token = TestOidcServer.CreateToken(sub: Guid.NewGuid(), role: "user");
        var request = new HttpRequestMessage(HttpMethod.Get, "/payments/me");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
