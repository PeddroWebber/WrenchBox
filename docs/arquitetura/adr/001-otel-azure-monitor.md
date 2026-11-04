# ADR 001 — OpenTelemetry e Azure Monitor

- Status: aceita
- Data: 2026-11-01

## Contexto

O enunciado pede uma ferramenta de observabilidade no estilo Datadog ou New Relic, com latência, CPU, memória, healthcheck, logs correlacionados e dashboards de negócio.

## Decisão

Instrumentar a API com os pacotes oficiais OpenTelemetry e com `Azure.Monitor.OpenTelemetry.AspNetCore`. No Azure, Application Insights e Container Insights cumprem o papel do APM. O exportador Prometheus fica ligado só quando `OpenTelemetry:Prometheus:Enabled` é verdadeiro, para o Grafana local.

Métricas de negócio no meter `WrenchBox`:

- `wrenchbox.work_orders.opened`
- `wrenchbox.work_orders.phase_duration` com label `phase` (`diagnosis`, `execution`, `finalization`)
- `wrenchbox.integrations.errors`

Logs JSON incluem `RequestId`, `TraceId` e `SpanId`. O header `X-Request-Id` é aceito ou gerado. O `traceparent` segue no HttpClient até a função.

## Consequências

Trocar o backend depois é configuração de exportador, não mudança de código de negócio. O vídeo pode mostrar o Grafana local ou o portal do Azure Monitor com as mesmas métricas.
