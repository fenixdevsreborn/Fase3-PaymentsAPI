# JWT Authentication — Trust model Users API → Payments API

A Payments API **não emite** tokens; ela **valida** tokens emitidos pela **Users API**. O modelo de confiança é:

- **Authority:** URL base da Users API (ex.: `https://users-api.example.com`). A metadata OIDC é obtida em `{Authority}/.well-known/openid-configuration` e as chaves públicas em **JWKS** (RS256).
- **Audience:** valor único acordado (ex.: `fcg-cloud-platform`). Deve ser o mesmo configurado na Users API ao emitir o token.
- **Identidade do usuário:** o **`sub`** do JWT é a **fonte de verdade**. A API **não aceita** `userId` no body das requisições; criar e consultar pagamentos usa sempre o `sub` do token.

## Configuração

| Configuração | Descrição | Exemplo / env |
|-------------|-----------|----------------|
| `Jwt:Authority` | URL base da Users API | `Jwt__Authority` |
| `Jwt:Audience` | Audience esperada | `Jwt__Audience` |
| `Jwt:RequireHttpsMetadata` | Exigir HTTPS na metadata (use `false` só em dev local) | `Jwt__RequireHttpsMetadata` |
| `Jwt:MetadataRequestTimeoutSeconds` | Timeout para requisições de metadata/JWKS | `Jwt__MetadataRequestTimeoutSeconds` |

**Fallback local:** em `appsettings.Development.json` use `Authority` apontando para a Users API local (ex.: `https://localhost:5001`) e `RequireHttpsMetadata: false` se a metadata for servida por HTTP.

## Comportamento quando metadata/JWKS está indisponível

- Durante a **validação do token**, se a obtenção da metadata ou do JWKS falhar (rede, timeout, 5xx), a requisição resulta em **401 Unauthorized**.
- Erros de backchannel (metadata/JWKS) são registrados em **log** (ex.: `OnAuthenticationFailed`). Não há retry automático por requisição; o cliente deve reenviar o token em uma nova requisição.

## Autorização

- **Owner:** recursos (pagamentos) são filtrados/validados pelo `sub` do usuário autenticado.
- **Admin:** role `admin` no JWT permite ver mais informações quando a aplicação assim o definir (ex.: auditoria ampliada).

## Testes de integração

- Os testes **sem** token (Create/GetMe/GetById sem auth → 401) passam com o `WebAppFixture` e `TestOidcServer`.
- O teste **com** token válido (`GetMe_WithValidToken_Returns200`) está temporariamente ignorado: com Minimal Hosting, o `ConfigureAppConfiguration` do `WebApplicationFactory` pode rodar depois do carregamento da config pelo `Program`, então a Authority do teste pode não ser aplicada. Para validar autenticação de ponta a ponta, use `appsettings.Testing.json` (Authority fixa, ex. `http://127.0.0.1:5098`) ou teste manualmente contra a Users API.

## Resumo

- Validar JWT via **Authority** + **JWKS** (RS256).
- **Não aceitar** `userId` no body; usar **`sub`** do token para criar e consultar pagamentos.
- Configurar **Authority** e **Audience** via appsettings ou variáveis de ambiente; em dev, usar fallback local e `RequireHttpsMetadata: false` se necessário.
