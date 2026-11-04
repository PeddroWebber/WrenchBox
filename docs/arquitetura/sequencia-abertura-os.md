# Sequência — abertura de ordem de serviço

Quem abre a ordem é o administrador, já autenticado. A API grava cliente, veículo, itens e a ordem com status Recebida, e conta essa abertura na métrica de volume. O e-mail não sai mais de dentro do processo da API quando `Notify:BaseUrl` está configurado: a API publica o evento na função e propaga o `traceparent`. Se o envio falha, a transição da ordem permanece. O que fica é o log `WorkOrderProcessingFailed`, a métrica de integração e uma linha em `integration_failures`.

```mermaid
sequenceDiagram
  participant Admin
  participant Gateway as API_Management
  participant API as WrenchBox_API
  participant Banco as PostgreSQL
  participant Funcao as Funcao_de_notificacao
  participant Mail as SMTP

  Admin->>Gateway: POST /api/v1/work-orders Bearer Admin
  Gateway->>API: encaminha
  API->>Banco: cliente, veiculo, itens e OS Recebida
  API->>API: metrica wrenchbox.work_orders.opened
  API-->>Admin: id e orderNumber
  Note over API,Funcao: Mudancas de status posteriores
  API->>Funcao: POST /notifications/work-orders traceparent
  Funcao->>Mail: e-mail da OS
  alt Falha no envio
    API->>API: log WorkOrderProcessingFailed
    API->>Banco: integration_failures
    Note over API: a transicao da OS nao e desfeita
  end
```

Os tempos médios aparecem em `GET /api/v1/metrics/operations` e nas métricas OpenTelemetry.

- Diagnóstico vai de `DiagnosisStartedAt` até `BudgetSentAt`.
- Execução vai de `ExecutionStartedAt` até `CompletedAt`.
- Finalização vai de `CompletedAt` até `DeliveredAt`.
