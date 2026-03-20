# Build — context = raiz do repositório do serviço.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Fcg.Payments.Api/Fcg.Payments.Api.csproj src/Fcg.Payments.Api/
COPY src/Fcg.Payments.Application/Fcg.Payments.Application.csproj src/Fcg.Payments.Application/
COPY src/Fcg.Payments.Contracts/Fcg.Payments.Contracts.csproj src/Fcg.Payments.Contracts/
COPY src/Fcg.Payments.Domain/Fcg.Payments.Domain.csproj src/Fcg.Payments.Domain/
COPY src/Fcg.Payments.Infrastructure/Fcg.Payments.Infrastructure.csproj src/Fcg.Payments.Infrastructure/

RUN dotnet restore src/Fcg.Payments.Api/Fcg.Payments.Api.csproj
COPY src src
RUN dotnet publish src/Fcg.Payments.Api/Fcg.Payments.Api.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fcg.Payments.Api.dll"]
