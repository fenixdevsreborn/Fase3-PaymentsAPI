using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Fcg.Payments.IntegrationTests;

public class WebAppFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var authority = TestOidcServer.BaseUrl;
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["Jwt:Authority"] = authority,
                ["Jwt:Audience"] = "fcg-cloud-platform",
                ["Jwt:RequireHttpsMetadata"] = "false"
            });
        });
    }
}
