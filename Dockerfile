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
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WrenchBox.Api.dll"]
