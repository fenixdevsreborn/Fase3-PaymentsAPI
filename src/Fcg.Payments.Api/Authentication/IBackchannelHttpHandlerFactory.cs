using System.Net.Http;

namespace Fcg.Payments.Api.Authentication;

/// <summary>Optional factory for JWT metadata backchannel. When registered, JwtBearer uses it instead of the default HttpClient. Used in tests to mock OIDC discovery/JWKS.</summary>
public interface IBackchannelHttpHandlerFactory
{
    HttpMessageHandler Create();
}
