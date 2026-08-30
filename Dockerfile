FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WrenchBox.sln ./
COPY src/WrenchBox.Domain/WrenchBox.Domain.csproj src/WrenchBox.Domain/
COPY src/WrenchBox.Application/WrenchBox.Application.csproj src/WrenchBox.Application/
COPY src/WrenchBox.Infrastructure/WrenchBox.Infrastructure.csproj src/WrenchBox.Infrastructure/
COPY src/WrenchBox.Api/WrenchBox.Api.csproj src/WrenchBox.Api/

RUN dotnet restore src/WrenchBox.Api/WrenchBox.Api.csproj

COPY src/ src/
RUN dotnet publish src/WrenchBox.Api/WrenchBox.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
RUN chown -R $APP_UID /app
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WrenchBox.Api.dll"]
