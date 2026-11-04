# RFC 002 — PostgreSQL Flexible Server

- Status: aceita
- Data: 2026-11-01

## Contexto

A aplicação já usa PostgreSQL 16 via EF Core e Npgsql. Trocar de motor obrigaria a reescrever migrations, testes de Testcontainers e o modelo relacional sem ganho para o domínio.

## Decisão

Manter PostgreSQL e promovê-lo a Flexible Server, um banco por ambiente no mesmo servidor, TLS obrigatório, backup do serviço e papéis `wrenchbox_app` e `wrenchbox_auth`.

## Alternativas

SQL Server gerenciado exigiria outro provider EF e outra suíte de testes. Um Postgres em pod não atende o requisito de banco gerenciado. Cosmos DB não oferece a transação relacional da baixa de estoque junto da aprovação da OS.

## Consequências

A connection string da API e a da função saem do Key Vault. A migration EF continua sendo a fonte do esquema. O script `sql/roles.sql` do repositório do banco cria os papéis depois que o servidor existe.
