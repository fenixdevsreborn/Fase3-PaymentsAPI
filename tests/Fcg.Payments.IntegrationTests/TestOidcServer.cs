using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fcg.Payments.Api.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Payments.IntegrationTests;

/// <summary>OIDC server for integration tests using HttpListener. Serves discovery + JWKS and creates tokens. Authority = http://127.0.0.1:port.</summary>
public static class TestOidcServer
{
    private static readonly Lazy<string> LazyAuthority = new(StartListener);
    private static string? _privateKeyPem;
    private static string? _baseUrlStored;

    public static string BaseUrl => LazyAuthority.Value;

    public static string CreateToken(Guid? sub = null, string? role = "user", string? scopes = "payments:read payments:write")
    {
        _ = LazyAuthority.Value;
        var key = RSA.Create();
        key.ImportFromPem(_privateKeyPem!);
        var securityKey = new RsaSecurityKey(key) { KeyId = "test-kid" };
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
        var claims = new List<Claim>
        {
            new(FcgClaimTypes.Sub, (sub ?? Guid.NewGuid()).ToString()),
            new(FcgClaimTypes.Role, role ?? "user"),
            new(FcgClaimTypes.Scope, scopes ?? "payments:read"),
            new(FcgClaimTypes.Jti, Guid.NewGuid().ToString())
        };
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _baseUrlStored,
            audience: "fcg-cloud-platform",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow,
            signingCredentials: creds
        );
        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string StartListener()
    {
        using var rsa = RSA.Create(2048);
        _privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var paramsPub = rsa.ExportParameters(false);
        var n = Convert.ToBase64String(paramsPub.Modulus!).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var e = Convert.ToBase64String(paramsPub.Exponent!).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var jwks = new { keys = new[] { new { kty = "RSA", use = "sig", alg = "RS256", kid = "test-kid", n, e } } };
        var jwksJson = JsonSerializer.Serialize(jwks);

        var port = 5098;
        for (var i = 0; i < 10; i++)
        {
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port + i}/");
                listener.Start();
                _baseUrlStored = $"http://127.0.0.1:{port + i}";
                var discovery = new
                {
                    issuer = _baseUrlStored,
                    jwks_uri = _baseUrlStored + "/.well-known/jwks.json",
                    response_types_supported = new[] { "token", "id_token" },
                    subject_types_supported = new[] { "public" },
                    id_token_signing_alg_values_supported = new[] { "RS256" }
                };
                var discoveryJson = JsonSerializer.Serialize(discovery);

                var thread = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            var ctx = listener.GetContext();
                            var path = ctx.Request.Url?.AbsolutePath.TrimEnd('/') ?? "";
                            string json;
                            if (path.Contains("openid-configuration", StringComparison.OrdinalIgnoreCase))
                                json = discoveryJson;
                            else if (path.Contains("jwks.json", StringComparison.OrdinalIgnoreCase))
                                json = jwksJson;
                            else
                            {
                                ctx.Response.StatusCode = 404;
                                ctx.Response.Close();
                                continue;
                            }
                            ctx.Response.ContentType = "application/json";
                            ctx.Response.ContentEncoding = Encoding.UTF8;
                            var buf = Encoding.UTF8.GetBytes(json);
                            ctx.Response.ContentLength64 = buf.Length;
                            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            ctx.Response.Close();
                        }
                        catch (HttpListenerException)
                        {
                            break;
                        }
                    }
                })
                { IsBackground = true };
                thread.Start();

                return _baseUrlStored;
            }
            catch (HttpListenerException)
            {
                if (i == 9) throw;
            }
        }

        throw new InvalidOperationException("Could not bind a port for TestOidcServer.");
    }
}
