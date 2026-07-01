# Referência de Endpoints

Base URL: `/api/v1`  
Documentação interativa: `/swagger`

---

## Legenda de autenticação

| Símbolo | Significado |
|---------|-------------|
| 🔓 | Público (sem autenticação) |
| 🔐 | JWT Bearer — header `Authorization: Bearer <token>` (role `Admin`) |
| 🎫 | Token de acompanhamento — header `X-Tracking-Token: <token>` |

---

## Autenticação

### `POST /auth/login` 🔓

Autentica administrador e retorna JWT.

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `email` | string | Sim | E-mail do administrador |
| `password` | string | Sim | Senha |

**Resposta 200:** `{ "token": "...", "expiresAt": "..." }`

**Erros:** 401 credenciais inválidas; 400 validação.

---

## Clientes

Base: `/customers` 🔐

### `GET /customers`

Lista clientes paginados.

| Query | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `page` | int | 1 | Página |
| `pageSize` | int | 20 | Itens por página |
| `search` | string | — | Busca por nome ou documento |

**Resposta 200:** `PagedResult<CustomerDto>`

---

### `GET /customers/{id}`

Retorna cliente por ID.

**Erros:** 404 se não existir.

---

### `POST /customers`

Cadastra novo cliente.

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `document` | string | Sim | CPF ou CNPJ válido |
| `name` | string | Sim | Nome completo / razão social |
| `email` | string | Sim | E-mail |
| `phone` | string | Sim | Telefone |

**Resposta 201:** `CustomerDto`

---

### `PUT /customers/{id}`

Atualiza nome, e-mail e telefone (documento imutável).

**Resposta 200:** `CustomerDto`

---

### `DELETE /customers/{id}`

Remove cliente.

**Resposta 204:** sem corpo.

---

## Veículos

Base: `/vehicles` 🔐

### `GET /vehicles`

| Query | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `customerId` | guid | — | Filtrar por cliente |
| `page` | int | 1 | Página |
| `pageSize` | int | 20 | Itens por página |

---

### `GET /vehicles/{id}`

Retorna veículo por ID.

---

### `POST /vehicles`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `customerId` | guid | Sim | Cliente proprietário |
| `plate` | string | Sim | Placa (legado ou Mercosul) |
| `brand` | string | Sim | Marca |
| `model` | string | Sim | Modelo |
| `year` | int | Sim | Ano (1900 – ano atual + 1) |

**Resposta 201:** `VehicleDto`

---

### `PUT /vehicles/{id}`

Atualiza marca, modelo e ano (placa imutável).

---

### `DELETE /vehicles/{id}`

Remove veículo.

**Resposta 204**

---

## Serviços (catálogo de mão de obra)

Base: `/services` 🔐

### `GET /services`

| Query | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `page` | int | 1 | Página |
| `pageSize` | int | 20 | Itens por página |
| `activeOnly` | bool | — | Filtrar apenas ativos |

---

### `GET /services/{id}`

Retorna serviço por ID.

---

### `POST /services`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `name` | string | Sim | Nome do serviço |
| `description` | string | Sim | Descrição |
| `unitPrice` | decimal | Sim | Preço unitário (≥ 0) |
| `estimatedDurationMinutes` | int | Sim | Duração estimada (> 0) |

---

### `PUT /services/{id}`

Atualiza serviço incluindo flag `isActive`.

---

### `DELETE /services/{id}`

Remove serviço do catálogo.

---

## Peças e insumos

Base: `/parts` 🔐

### `GET /parts`

| Query | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `page` | int | 1 | Página |
| `pageSize` | int | 20 | Itens por página |
| `activeOnly` | bool | — | Filtrar apenas ativos |

**Resposta inclui:** `isBelowMinimumStock` (alerta de reposição).

---

### `GET /parts/{id}`

Retorna peça com quantidade em estoque.

---

### `POST /parts`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `name` | string | Sim | Nome da peça |
| `sku` | string | Sim | Código SKU (único) |
| `unitPrice` | decimal | Sim | Preço unitário |
| `stockQuantity` | int | Sim | Estoque inicial |
| `minimumStock` | int | Sim | Estoque mínimo para alerta |

---

### `PUT /parts/{id}`

Atualiza nome, preço, estoque mínimo e `isActive` (não altera quantidade — use PATCH stock).

---

### `PATCH /parts/{id}/stock`

Ajuste manual de estoque.

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `quantity` | int | Sim | Delta (+ entrada, − saída); não pode ser 0 |
| `reason` | string | Sim | Motivo do ajuste |

Registra movimentação tipo `Adjustment`.

---

### `DELETE /parts/{id}`

Remove peça.

---

## Ordens de Serviço

Base: `/work-orders` 🔐

### `GET /work-orders`

| Query | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `page` | int | 1 | Página |
| `pageSize` | int | 20 | Itens por página |
| `status` | WorkOrderStatus | — | Filtrar por status |
| `customerId` | guid | — | Filtrar por cliente |

**Valores de status:** `Received`, `InDiagnosis`, `AwaitingApproval`, `InExecution`, `Completed`, `Delivered`

---

### `GET /work-orders/{id}`

Retorna OS completa: itens, histórico, token (se gerado), valores.

---

### `POST /work-orders`

Abre nova OS (cria cliente/veículo se necessário).

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `customerDocument` | string | Sim | CPF/CNPJ |
| `customerName` | string | Sim | Nome do cliente |
| `customerEmail` | string | Sim | E-mail |
| `customerPhone` | string | Sim | Telefone |
| `vehiclePlate` | string | Sim | Placa |
| `vehicleBrand` | string | Sim | Marca |
| `vehicleModel` | string | Sim | Modelo |
| `vehicleYear` | int | Sim | Ano |
| `services` | array | Sim (≥1) | `{ serviceId, quantity }` |
| `parts` | array | Não | `{ partId, quantity }` |
| `notes` | string | Não | Observações |

**Resposta 201:** `WorkOrderDto` com status `Received` e `totalAmount` calculado.

---

### `POST /work-orders/{id}/start-diagnosis`

Inicia fase de diagnóstico.

**Pré-condição:** status `Received`  
**Resposta 200:** OS com status `InDiagnosis`

---

### `POST /work-orders/{id}/send-budget`

Envia orçamento ao cliente.

**Pré-condição:** status `InDiagnosis`  
**Resposta 200:**

```json
{
  "workOrderId": "guid",
  "trackingToken": "string",
  "notificationSent": true
}
```

Dispara notificação via `IBudgetNotificationService`.

---

### `POST /work-orders/{id}/complete`

Marca serviço como finalizado.

**Pré-condição:** status `InExecution`  
**Resposta 200:** OS com status `Completed`

---

### `POST /work-orders/{id}/deliver`

Registra entrega do veículo ao cliente.

**Pré-condição:** status `Completed`  
**Resposta 200:** OS com status `Delivered`

---

## Acompanhamento (cliente)

Base: `/tracking/work-orders` 🎫

### `GET /tracking/work-orders`

Consulta OS pelo token de acompanhamento.

**Header obrigatório:** `X-Tracking-Token`

**Resposta 200:** `TrackingWorkOrderDto` (sem dados sensíveis de IDs internos)

**Erros:** 400 token ausente; 404 token inválido.

---

### `POST /tracking/work-orders/approve`

Cliente aprova orçamento.

**Header obrigatório:** `X-Tracking-Token`

**Pré-condição:** status `AwaitingApproval`  
**Efeito:** baixa estoque; status → `InExecution`

**Erros:** 400 estoque insuficiente ou status inválido.

---

## Métricas

Base: `/metrics` 🔐

### `GET /metrics/average-execution-time`

Retorna estatísticas de tempo de execução das OS finalizadas.

**Resposta 200:**

```json
{
  "averageMinutes": 123.45,
  "completedOrdersCount": 42
}
```

Calculado com base em `ExecutionStartedAt` e `CompletedAt`.

---

## Resumo por método HTTP

| Recurso | GET | POST | PUT | PATCH | DELETE |
|---------|-----|------|-----|-------|--------|
| `/auth/login` | — | ✅ | — | — | — |
| `/customers` | ✅ lista | ✅ | — | — | — |
| `/customers/{id}` | ✅ | — | ✅ | — | ✅ |
| `/vehicles` | ✅ lista | ✅ | — | — | — |
| `/vehicles/{id}` | ✅ | — | ✅ | — | ✅ |
| `/services` | ✅ lista | ✅ | — | — | — |
| `/services/{id}` | ✅ | — | ✅ | — | ✅ |
| `/parts` | ✅ lista | ✅ | — | — | — |
| `/parts/{id}` | ✅ | — | ✅ | — | ✅ |
| `/parts/{id}/stock` | — | — | — | ✅ | — |
| `/work-orders` | ✅ lista | ✅ | — | — | — |
| `/work-orders/{id}` | ✅ | — | — | — | — |
| `/work-orders/{id}/start-diagnosis` | — | ✅ | — | — | — |
| `/work-orders/{id}/send-budget` | — | ✅ | — | — | — |
| `/work-orders/{id}/complete` | — | ✅ | — | — | — |
| `/work-orders/{id}/deliver` | — | ✅ | — | — | — |
| `/tracking/work-orders` | ✅ | — | — | — | — |
| `/tracking/work-orders/approve` | — | ✅ | — | — | — |
| `/metrics/average-execution-time` | ✅ | — | — | — | — |
