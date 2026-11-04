# ADR 004 — JWT sem CPF

Status: aceita em 1º de novembro de 2026.

## Contexto

O CPF identifica o cliente na entrada, mas é dado pessoal. Colocá-lo no token espalharia o documento por log de proxy, histórico de navegador e rastreio.

## Decisão

O token carrega o identificador interno, em `customer_id` e `sub`, e o papel. A API resolve o cliente por esse id. A audience e a chave são as mesmas do JWT de administrador, para haver um único par de validação. A audience histórica `WrenchBox.Admin` foi mantida para não quebrar o ConfigMap nem os testes já existentes. O nome da audience não restringe o papel.

## Consequências

Vazar o token não revela o CPF. O log da função registra o motivo da recusa — inválido, inexistente, inativo ou bloqueado — sem devolver essa distinção a quem tentou entrar.
