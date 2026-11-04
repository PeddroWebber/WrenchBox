# ADR 001 — OpenTelemetry e Azure Monitor

Status: aceita em 1º de novembro de 2026.

## Contexto

A fase pede observabilidade no espírito de um Datadog ou New Relic: latência, CPU, memória, healthcheck, logs correlacionados e painéis de negócio. A escolha da ferramenta é livre. Na Azure, o caminho oficial já cobre esse papel.

## Decisão

A API é instrumentada com os pacotes oficiais de OpenTelemetry e com `Azure.Monitor.OpenTelemetry.AspNetCore`. No Azure, Application Insights e Container Insights fazem o papel do APM. O exportador Prometheus só liga quando `OpenTelemetry:Prometheus:Enabled` é verdadeiro, para o Grafana do ensaio local.

As métricas de negócio ficam no meter `WrenchBox`:

- `wrenchbox.work_orders.opened`
- `wrenchbox.work_orders.phase_duration`, com o rótulo `phase` (`diagnosis`, `execution`, `finalization`)
- `wrenchbox.integrations.errors`

Os logs saem em JSON com `RequestId`, `TraceId` e `SpanId`. O header `X-Request-Id` é aceito quando vem na entrada e gerado quando não vem. O `traceparent` segue no HttpClient até a função.

## Consequências

Trocar o destino da telemetria depois é configuração de exportador, não mudança de regra de negócio. A apresentação pode mostrar o Grafana local ou o portal do Azure Monitor: as séries são as mesmas.
