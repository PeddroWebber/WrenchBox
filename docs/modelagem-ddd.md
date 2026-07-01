# Modelagem DDD

Documentação da arquitetura Domain-Driven Design aplicada ao WrenchBox.

---

## Estratégico — Context Map

```mermaid
graph TB
    subgraph Core [Core Domain]
        AT[Atendimento e OS]
    end

    subgraph Supporting [Supporting Subdomains]
        CA[Cadastro Cliente/Veículo]
        ES[Estoque e Catálogo]
    end

    subgraph Generic [Generic Subdomain]
        ID[Identidade e Acesso]
        NO[Notificação]
        ME[Métricas]
    end

    AT -->|usa| CA
    AT -->|consome| ES
    AT -->|notifica via| NO
    AT -->|alimenta| ME
    CA --> ID
    ES --> ID
    AT --> ID
```

| Subdomínio | Classificação | Justificativa |
|------------|---------------|---------------|
| **Atendimento e OS** | Core | Diferencial do negócio; fluxo de status, orçamento e aprovação |
| **Cadastro** | Supporting | Necessário, mas padrão de mercado |
| **Estoque/Catálogo** | Supporting | CRUD + movimentação; integrado ao core na aprovação |
| **Identidade (JWT)** | Generic | Infraestrutura de segurança |
| **Notificação** | Generic | Canal substituível (log → SMTP) |
| **Métricas** | Generic | Agregação de leitura sobre OS finalizadas |

---

## Tático — Agregados

### Diagrama de agregados

```mermaid
classDiagram
    class WorkOrder {
        +OrderNumber
        +Status
        +TotalAmount
        +TrackingToken
        +StartDiagnosis()
        +SendBudgetForApproval()
        +ApproveBudget()
        +Complete()
        +Deliver()
    }

    class WorkOrderServiceItem {
        +ServiceId
        +Quantity
        +UnitPrice
    }

    class WorkOrderPartItem {
        +PartId
        +Quantity
        +UnitPrice
    }

    class WorkOrderStatusHistory {
        +FromStatus
        +ToStatus
        +ChangedAt
    }

    class Customer {
        +Document
        +Name
        +AddVehicle()
    }

    class Vehicle {
        +Plate
        +Brand
        +Model
    }

    class Part {
        +Sku
        +StockQuantity
        +AdjustStock()
        +Deduct()
    }

    class StockMovement {
        +Type
        +Quantity
        +Reason
    }

    WorkOrder *-- WorkOrderServiceItem
    WorkOrder *-- WorkOrderPartItem
    WorkOrder *-- WorkOrderStatusHistory
    Customer *-- Vehicle
    Part *-- StockMovement

    WorkOrder ..> Customer : CustomerId
    WorkOrder ..> Vehicle : VehicleId
    WorkOrder ..> Part : ApproveBudget
```

### Regras de consistência transacional

| Agregado | Boundary | Uma transação |
|----------|----------|---------------|
| `WorkOrder` | OS + itens + histórico | Sim |
| `Customer` | Cliente + veículos (criação) | Sim (na OS, parcial) |
| `Part` | Peça + movimentação | Sim |
| `Service` | Serviço isolado | Sim |
| `AdminUser` | Usuário isolado | Sim |

**Cross-aggregate:** `ApproveBudget` modifica `WorkOrder` e múltiplos `Part` na mesma transação via `UnitOfWork`.

---

## Value Objects

```mermaid
classDiagram
    class Document {
        +Value: string
        +Type: DocumentType
        +Create(raw) Document
        +Formatted: string
    }

    class Plate {
        +Value: string
        +Create(raw) Plate
        +Formatted: string
    }

    Document --> DocumentType : Cpf | Cnpj
```

| Value Object | Imutável | Validação |
|--------------|----------|-----------|
| `Document` | Sim | CPF/CNPJ algoritmo oficial |
| `Plate` | Sim | Regex legado + Mercosul |

---

## Camadas e responsabilidades

```mermaid
flowchart TB
    subgraph Api [WrenchBox.Api]
        CTRL[Controllers]
        MW[ExceptionHandlingMiddleware]
        SW[Swagger]
    end

    subgraph App [WrenchBox.Application]
        CMD[Commands / Queries]
        VAL[FluentValidation]
        DTO[DTOs + Mappers]
        BEH[ValidationBehavior]
    end

    subgraph Domain [WrenchBox.Domain]
        ENT[Entities]
        VO[Value Objects]
        REPO_I[I*Repository]
        EX[DomainException]
    end

    subgraph Infra [WrenchBox.Infrastructure]
        EF[WrenchBoxDbContext]
        REP[Repositories]
        JWT[JWT / BCrypt]
        NOTIF[BudgetNotification]
    end

    CTRL --> CMD
    CMD --> BEH --> VAL
    CMD --> ENT
    CMD --> REPO_I
    REP -.implementa.-> REPO_I
    REP --> EF
    MW --> CTRL
```

| Camada | Pode referenciar | Não pode |
|--------|------------------|----------|
| Domain | — | Application, Infrastructure, Api |
| Application | Domain | Infrastructure (via interfaces) |
| Infrastructure | Domain, Application (interfaces) | — |
| Api | Application | Domain diretamente (idealmente) |

---

## Padrões aplicados

| Padrão | Onde | Descrição |
|--------|------|-----------|
| **CQRS (leve)** | Application | Commands mutam; Queries leem via MediatR |
| **Repository** | Domain/Infrastructure | Abstração de persistência |
| **Unit of Work** | Infrastructure | `SaveChangesAsync` transacional |
| **Domain Service (implícito)** | Domain entities | Lógica em `WorkOrder`, `Part` |
| **Application Service** | Handlers | Orquestração sem regra de negócio |
| **DTO** | Application | Contrato da API desacoplado do domínio |
| **Anti-corruption (leve)** | Mappers | `ToDto()`, `ToTrackingDto()` |

---

## Diagrama de sequência — Camadas na aprovação

```mermaid
sequenceDiagram
    participant C as TrackingController
    participant M as MediatR Handler
    participant D as WorkOrder (Domain)
    participant P as Part (Domain)
    participant R as Repositories
    participant DB as PostgreSQL

    C->>M: ApproveBudgetCommand
    M->>R: GetByTrackingTokenForUpdateAsync
    R->>DB: SELECT work_order + part_items
    M->>R: GetByIdsForUpdateAsync (parts)
    M->>D: ApproveBudget(partsById)
    D->>P: Deduct(qty, workOrderId)
    M->>R: SaveChangesAsync
    R->>DB: UPDATE + INSERT movements
    M->>R: GetByTrackingTokenAsync
    M-->>C: TrackingWorkOrderDto
```

---

## Ubiquitous Language no código

| Conceito de negócio | Namespace / tipo |
|---------------------|------------------|
| Ordem de Serviço | `WrenchBox.Domain.Entities.WorkOrder` |
| Status da OS | `WrenchBox.Domain.Enums.WorkOrderStatus` |
| Token de Acompanhamento | `WorkOrder.TrackingToken` |
| Orçamento | `WorkOrder.TotalAmount` |
| Baixa de estoque | `Part.Deduct()` |
| Movimentação | `StockMovement` |
| Notificação de orçamento | `IBudgetNotificationService` |

---

## Eventos de domínio (conceituais)

O MVP **não implementa** Domain Events explícitos (`IDomainEvent`). Os fatos de negócio são inferidos por:

- Mudança de status + `WorkOrderStatusHistory`
- `StockMovement` registrado
- Timestamps (`DiagnosisStartedAt`, `ApprovedAt`, etc.)

Evolução natural: extrair eventos como `BudgetApprovedEvent` para desacoplar notificações e métricas.

---

## Diagrama de implantação

```mermaid
graph LR
    subgraph Docker
        API[WrenchBox.Api :8080]
        PG[(PostgreSQL :5432)]
    end

    Admin[Administrador] -->|JWT| API
    Cliente[Cliente] -->|X-Tracking-Token| API
    API --> PG
```

---

## Referências cruzadas

- Vocabulário: [linguagem-ubiqua.md](linguagem-ubiqua.md)
- Regras detalhadas: [logica-de-negocio.md](logica-de-negocio.md)
- Event Storming (OS e estoque): [documentacao-ddd-completa.md](documentacao-ddd-completa.md)
- Event Storming Estoque: [event-storming-estoque.md](event-storming-estoque.md)
- Endpoints: [api-endpoints.md](api-endpoints.md)
