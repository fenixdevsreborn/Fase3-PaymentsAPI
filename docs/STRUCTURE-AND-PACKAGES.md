# Fase3-PaymentsAPI — Estrutura e pacotes

## Árvore final de pastas (principais)

```
Fase3-PaymentsAPI/
├── src/
│   ├── Fcg.Payments.Api/
│   │   ├── Authentication/
│   │   │   ├── FcgClaimTypes.cs
│   │   │   ├── FcgRoles.cs
│   │   │   ├── FcgPolicies.cs
│   │   │   ├── JwtOptions.cs
│   │   │   └── JwtBearerExtensions.cs
│   │   ├── Authorization/
│   │   │   ├── AuthorizationExtensions.cs
│   │   │   ├── UserClaimsExtensions.cs
│   │   │   ├── OwnerAuthorization.cs
│   │   │   ├── ICurrentUserAccessor.cs
│   │   │   └── CurrentUserAccessor.cs
│   │   ├── Observability/
│   │   │   ├── ObservabilityContext.cs
│   │   │   ├── ObservabilityContextAccessor.cs   # Implementa Application.Observability.IObservabilityContextAccessor
│   │   │   ├── ObservabilityOptions.cs
│   │   │   ├── FcgMetricNames.cs                 # HTTP + payments.created/paid/failed + exceptions
│   │   │   ├── FcgMeters.cs
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   ├── HttpMetricsMiddleware.cs
│   │   │   ├── ExceptionObservabilityMiddleware.cs
│   │   │   ├── ObservabilityServiceCollectionExtensions.cs
│   │   │   └── ObservabilityApplicationBuilderExtensions.cs
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs    # AddPaymentsApiAuth, AddPaymentsApiObservability
│   │   ├── OpenApi/
│   │   │   └── BearerSecuritySchemeTransformer.cs
│   │   ├── Controllers/
│   │   │   ├── PaymentsController.cs
│   │   │   └── WebhooksController.cs
│   │   └── Program.cs
│   ├── Fcg.Payments.Application/
│   │   ├── Observability/
│   │   │   └── IObservabilityContextAccessor.cs  # Contrato para TraceId/CorrelationId (audit e eventos)
│   │   └── Services/
│   ├── Fcg.Payments.Domain/
│   ├── Fcg.Payments.Infrastructure/
│   │   └── Outbox/                               # OutboxEventPublisher, OutboxRelayService, SqsNotificationMessage
│   └── Fcg.Payments.Contracts/
│       └── Events/
│           └── PaymentNotificationEvent.cs       # Payload padronizado para e-mail (TraceId, CorrelationId)
├── tests/
└── docs/
    └── STRUCTURE-AND-PACKAGES.md
```

## Evento padronizado para o serviço de e-mail

O contrato **PaymentNotificationEvent** (Fcg.Payments.Contracts.Events) é o payload enviado ao outbox e, após relay, à fila SQS consumida pela Lambda de notificação:

- **TemplateName:** `PaymentApproved` ou `PaymentFailed`
- **UserId, UserEmail, UserName, GameId, GameTitle, Amount, Currency, PaymentId, PaymentStatus, OccurredAt**
- **TraceId, CorrelationId:** preenchidos pelo PaymentService via IObservabilityContextAccessor para rastreio ponta a ponta.

O OutboxRelayService monta **SqsNotificationMessage** com CorrelationId e TraceId do evento, compatível com o contrato da Notification Lambda.

## Comandos NuGet necessários

**Fcg.Payments.Api:**

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.3
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.3
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore --version 10.0.0
dotnet add package Microsoft.OpenApi --version 2.0.0
dotnet add package Scalar.AspNetCore --version 2.0.36
```

**Fcg.Payments.Application:**

```bash
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 10.0.0
```

**Fcg.Payments.Infrastructure:** (conforme csproj existente: EF Core, AWS SDK, Polly, etc.)

Nenhum pacote do Fase3-Shared é necessário. JWT validado com a mesma chave/issuer/audience da Users API.
