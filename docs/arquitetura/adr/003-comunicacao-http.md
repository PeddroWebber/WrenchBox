# ADR 003 — Comunicação HTTP síncrona

- Status: aceita
- Data: 2026-11-01

## Contexto

Há três conversas: cliente e gateway, gateway e API, API e função de notificação. Um barramento aumentaria o número de componentes sem estar no enunciado.

## Decisão

Tudo é HTTP. O gateway roteia. A API publica o evento de OS em `POST /notifications/work-orders` com a chave da função. Se `Notify:BaseUrl` estiver vazio, o SMTP interno (MailKit) envia o mesmo conteúdo. Falha de envio grava `WorkOrderProcessingFailed`, incrementa a métrica de integração e não desfaz a mudança de status.

## Consequências

A notificação é síncrona e curta. Se a função estiver fora, a OS já foi salva e a falha fica visível no log, na tabela `integration_failures` e no alerta do Azure Monitor. Uma fila seria o passo seguinte se o volume de e-mail passar a importar.
