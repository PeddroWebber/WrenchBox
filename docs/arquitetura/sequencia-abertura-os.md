# Sequência — abertura de ordem de serviço

A abertura continua sendo uma operação do administrador. A notificação saiu do processo da API quando `Notify:BaseUrl` está configurado.

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

Tempos médios expostos em `GET /api/v1/metrics/operations` e nas métricas OpenTelemetry:

- Diagnóstico: `DiagnosisStartedAt` até `BudgetSentAt`
- Execução: `ExecutionStartedAt` até `CompletedAt`
- Finalização: `CompletedAt` até `DeliveredAt`
