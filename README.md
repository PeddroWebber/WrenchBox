# WrenchBox API

Backend MVP para gestão de oficinas mecânicas: ordens de serviço, clientes, veículos, catálogo de serviços, estoque de peças, APIs administrativas com JWT e acompanhamento pelo cliente via token de rastreamento.

## Arquitetura

Monólito em camadas com Domain-Driven Design (DDD):

| Projeto | Responsabilidade |
|---------|------------------|
| `WrenchBox.Domain` | Entidades, value objects, regras de negócio |
| `WrenchBox.Application` | Handlers MediatR, DTOs, FluentValidation |
| `WrenchBox.Infrastructure` | EF Core, PostgreSQL, JWT, repositórios |
| `WrenchBox.Api` | Controllers REST, Swagger, middleware |

Documentação de domínio, Event Storming, endpoints e segurança: **[docs/](docs/README.md)**

## Por que PostgreSQL?

Integridade relacional entre clientes, veículos e ordens de serviço; transações ACID na aprovação de orçamento com baixa de estoque; suporte maduro ao EF Core via Npgsql; execução simples no Docker.

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para `docker compose` e testes de integração)

## Execução

### Docker (recomendado)

```bash
docker compose up --build
```

- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger

Credenciais do administrador (desenvolvimento): `admin@wrenchbox.local` / `Admin@123`

Na primeira inicialização, o banco recebe dados de demonstração (catálogo, clientes, veículos e ordens de serviço). O seed é idempotente; para recarregar, use `docker compose down -v` antes de subir novamente.

### Local

```bash
docker compose up db -d
dotnet run --project src/WrenchBox.Api
```

Swagger: http://localhost:5000/swagger

## Variáveis de ambiente

| Variável | Descrição | Padrão (docker-compose) |
|----------|-----------|-------------------------|
| `ConnectionStrings__Default` | String de conexão PostgreSQL | `Host=db;...` |
| `Jwt__Secret` | Chave de assinatura JWT (mín. 32 caracteres) | definida no compose |
| `Jwt__Issuer` | Emissor do token | `WrenchBox` |
| `Jwt__Audience` | Audiência do token | `WrenchBox.Admin` |
| `Jwt__ExpiryMinutes` | Tempo de vida do token | `60` |

## API

Dois perfis de acesso:

- **Administrativo** — JWT Bearer: auth, CRUDs, ciclo de vida da OS, estoque e métricas
- **Cliente** — header `X-Tracking-Token`: consulta e aprovação de orçamento

Referência completa: **[docs/api-endpoints.md](docs/api-endpoints.md)**

### Fluxo de status da ordem de serviço

```
Received → InDiagnosis → AwaitingApproval → InExecution → Completed → Delivered
```

O token de rastreamento é gerado ao enviar o orçamento (`POST /api/v1/work-orders/{id}/send-budget`). A aprovação pelo cliente dispara a baixa automática de estoque.

## Testes

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

- Testes unitários em `WrenchBox.Domain.Tests` e `WrenchBox.Application.Tests`
- Testes de integração com Testcontainers (PostgreSQL); ignorados se Docker indisponível
- Meta de cobertura: **≥ 80%** de linhas em Domain e Application

## Estrutura da solução

```
src/
  WrenchBox.Domain/
  WrenchBox.Application/
  WrenchBox.Infrastructure/
  WrenchBox.Api/
tests/
  WrenchBox.Domain.Tests/
  WrenchBox.Application.Tests/
  WrenchBox.Integration.Tests/
Dockerfile
docker-compose.yml
docs/
```

Migrations EF Core são aplicadas automaticamente na inicialização da API.
