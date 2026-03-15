using System.Net;
using Fcg.Payments.Api.Authentication;

namespace Fcg.Payments.IntegrationTests;

/// <summary>Redirects JWT metadata/JWKS requests to the test OIDC server so validation uses the test issuer and keys.</summary>
public sealed class TestBackchannelHttpHandlerFactory : IBackchannelHttpHandlerFactory
{
    public HttpMessageHandler Create()
    {
        var baseUrl = TestOidcServer.BaseUrl;
        return new RedirectToTestOidcHandler(baseUrl);
    }

    private sealed class RedirectToTestOidcHandler : DelegatingHandler
    {
        private readonly Uri _testOidcBase;

        public RedirectToTestOidcHandler(string testOidcBaseUrl)
        {
            _testOidcBase = new Uri(testOidcBaseUrl.TrimEnd('/') + "/");
            InnerHandler = new SocketsHttpHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "/";
            var query = request.RequestUri?.Query ?? "";
            request.RequestUri = new Uri(_testOidcBase, path + query);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
