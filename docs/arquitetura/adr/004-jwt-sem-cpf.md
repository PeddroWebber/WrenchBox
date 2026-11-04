# ADR 004 — JWT sem CPF

- Status: aceita
- Data: 2026-11-01

## Contexto

O CPF identifica o cliente na entrada, mas é dado pessoal. Colocá-lo no token espalha o documento por log de proxy, histórico de navegador e rastreio.

## Decisão

O token carrega o identificador interno (`customer_id` e `sub`) e a role. A API resolve o cliente por esse id. Audience e chave são as mesmas do JWT de administrador, para um único par de validação. A audience histórica `WrenchBox.Admin` foi mantida para não quebrar o ConfigMap e os testes já existentes; o nome não restringe a role.

## Consequências

Vazar o token não revela o CPF. O log da função registra o motivo da recusa (inválido, inexistente, inativo, bloqueado) sem devolver essa distinção ao chamador.
