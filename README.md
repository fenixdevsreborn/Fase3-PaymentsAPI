# FCG Payments API

API de pagamentos da FCG Cloud Platform (.NET 10), integrada a Games API, Users API (JWT), gateway de pagamento e serviço de e-mail.

## Estrutura

- **src/Fcg.Payments.Api** – Host ASP.NET Core, controllers, JWT, Scalar
- **src/Fcg.Payments.Application** – Serviços (Payment, Audit), interfaces (gateway, evento, game client)
- **src/Fcg.Payments.Domain** – Entidades (Payment, AuditLog, OutboxEvent, IdempotencyRecord), enums, repositórios
- **src/Fcg.Payments.Infrastructure** – EF Core (PostgreSQL), repositórios, FakePaymentGateway, outbox, GameApiClient
- **src/Fcg.Payments.Contracts** – DTOs, eventos (PaymentNotificationEvent)
- **tests/Fcg.Payments.UnitTests** – Testes unitários (PaymentService, FakePaymentGateway)
- **tests/Fcg.Payments.IntegrationTests** – Testes de integração (API com auth)

## Pré-requisitos

- .NET 10 SDK
- PostgreSQL (ou use a connection string em appsettings)

## Pacotes NuGet (referência)

```bash
# Api
cd src/Fcg.Payments.Api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add package Scalar.AspNetCore --version 2.0.36

# Application
cd src/Fcg.Payments.Application
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.0

# Infrastructure
cd src/Fcg.Payments.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.0
dotnet add package Microsoft.Extensions.Http --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0

# UnitTests
cd tests/Fcg.Payments.UnitTests
dotnet add package Moq --version 4.20.72

# IntegrationTests
cd tests/Fcg.Payments.IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 10.0.0
```

## Migrations

```bash
cd src/Fcg.Payments.Infrastructure
dotnet ef migrations add NomeDaMigration --startup-project ../Fcg.Payments.Api
dotnet ef database update --startup-project ../Fcg.Payments.Api
```

## Configuração (appsettings)

- **ConnectionStrings:DefaultConnection** – PostgreSQL
- **Jwt:SigningKey** – Chave compartilhada com a Users API (mín. 32 caracteres)
- **Jwt:Issuer** / **Jwt:Audience** – Mesmos da Users API
- **GamesApi:BaseUrl** – URL base da Games API (ex.: http://localhost:5001)

## Endpoints

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| POST | /payments | Cria pagamento (body: `gameId`, opcional `currency`); usuário do JWT | Bearer |
| GET | /payments/{id} | Obtém pagamento (dono ou admin) | Bearer |
| GET | /payments/me | Lista pagamentos do usuário (paginado: pageNumber, pageSize) | Bearer |
| POST | /payments/{id}/confirm | Confirma pagamento (idempotencyKey opcional) | Bearer |
| POST | /payments/{id}/fail | Marca como falha (failureReason, idempotencyKey opcionais) | Bearer |
| POST | /payments/webhooks/provider | Webhook do provedor (providerReference, status) | AllowAnonymous |
| GET | /payments/{id}/audit | Trilha de auditoria do pagamento (dono ou admin) | Bearer |

- **userId** vem sempre do JWT (claim sub/NameIdentifier), nunca do body.
- Usuário só acessa seus próprios pagamentos; admin acessa qualquer um.

## Evento de notificação (e-mail)

Após confirmação ou falha, é gravado um evento no outbox (`PaymentNotification`) com o seguinte shape (serializado em JSON), consumível pela fila/Lambda de e-mail:

- **templateName**: `PaymentConfirmed` ou `PaymentFailed`
- **userId**, **userEmail**, **userName**
- **gameId**, **gameTitle**
- **amount**, **currency**, **paymentStatus**
- **occurredAt**, **paymentId**

## Status de pagamento

- Pending → Authorized (gateway) → Paid (confirm) ou Failed (fail/webhook)
- Transições: Pending/Authorized → Paid; Pending/Authorized → Failed; também Cancelled, Refunded via webhook.

## Idempotência

- **confirm** / **fail**: uso opcional de `idempotencyKey` no body; mesma chave retorna o mesmo resultado sem reexecutar captura/falha.
- **webhook**: atualização por `providerReference`; se o pagamento já estiver Paid/Failed, o webhook é ignorado (idempotente).

## Docker

### Docker Compose (PostgreSQL 17 + API)

O `docker-compose.yml` sobe **dois containers**: primeiro o **PostgreSQL 17** (com volume persistente), depois a **API** (que só inicia após o Postgres estar saudável).

- **postgres**: imagem `postgres:17-bookworm`, volume `payments_pgdata` para persistência, healthcheck; sobe primeiro.
- **fcg.payments.api**: depende de `postgres` com `condition: service_healthy`; conecta em `Host=postgres`.

```bash
# Build e execução (Postgres sobe primeiro, depois a API)
docker compose build
docker compose up -d

# API em http://localhost:8080 | Postgres em localhost:5432 (usuário/senha: postgres, database: fcg_payments)
docker compose logs -f
```

Para rodar mais de uma API ao mesmo tempo na mesma máquina, altere a porta do Postgres em um dos compose (ex.: `"5433:5432"`) para evitar conflito.

### Dockerfile.postgres (all-in-one)

Para deploy em que a task roda um único container com API + Postgres (ex.: ECS), use `Dockerfile.postgres` (imagem base `postgres:17-bookworm`).

## Rodar e documentação

```bash
cd src/Fcg.Payments.Api
dotnet run
```

- API: http://localhost:5xxx (conforme launchSettings)
- Scalar (doc): em Development, após MapOpenApi e MapScalarApiReference (ex.: /scalar/v1).
- Configure o Bearer (JWT) na UI do Scalar para testar endpoints protegidos.

## Testes

```bash
dotnet test
```
