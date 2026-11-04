# ADR 003 — Comunicação HTTP síncrona

Status: aceita em 1º de novembro de 2026.

## Contexto

Há três conversas: o cliente com o gateway, o gateway com a API, e a API com a função de notificação. Um barramento aumentaria o número de componentes sem estar no escopo desta fase.

## Decisão

Tudo é HTTP. O gateway roteia. A API publica o evento da ordem em `POST /notifications/work-orders`, com a chave da função. Se `Notify:BaseUrl` estiver vazio, o SMTP interno, via MailKit, envia o mesmo conteúdo. Falha de envio grava o log `WorkOrderProcessingFailed`, incrementa a métrica de integração e não desfaz a mudança de status.

## Consequências

A notificação é síncrona e curta. Se a função estiver fora, a ordem já foi salva. A falha fica visível no log, na tabela `integration_failures` e no alerta do Azure Monitor. Uma fila seria o passo seguinte se o volume de e-mail passar a importar.
