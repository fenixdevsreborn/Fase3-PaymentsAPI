# Build — context = FASE3: docker build -f Fase3-PaymentsAPI/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Fase3-PaymentsAPI/src/Fcg.Payments.Api/Fcg.Payments.Api.csproj Fase3-PaymentsAPI/src/Fcg.Payments.Api/
COPY Fase3-PaymentsAPI/src/Fcg.Payments.Application/Fcg.Payments.Application.csproj Fase3-PaymentsAPI/src/Fcg.Payments.Application/
COPY Fase3-PaymentsAPI/src/Fcg.Payments.Contracts/Fcg.Payments.Contracts.csproj Fase3-PaymentsAPI/src/Fcg.Payments.Contracts/
COPY Fase3-PaymentsAPI/src/Fcg.Payments.Domain/Fcg.Payments.Domain.csproj Fase3-PaymentsAPI/src/Fcg.Payments.Domain/
COPY Fase3-PaymentsAPI/src/Fcg.Payments.Infrastructure/Fcg.Payments.Infrastructure.csproj Fase3-PaymentsAPI/src/Fcg.Payments.Infrastructure/

RUN dotnet restore Fase3-PaymentsAPI/src/Fcg.Payments.Api/Fcg.Payments.Api.csproj
COPY Fase3-PaymentsAPI/src Fase3-PaymentsAPI/src
RUN dotnet publish Fase3-PaymentsAPI/src/Fcg.Payments.Api/Fcg.Payments.Api.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fcg.Payments.Api.dll"]
