# RFC 002 — PostgreSQL Flexible Server

Status: aceita em 1º de novembro de 2026.

## Contexto

A aplicação já fala com PostgreSQL 16 pelo EF Core e pelo Npgsql. Trocar de motor obrigaria a reescrever migrations, a suíte de Testcontainers e o modelo relacional, sem ganho para o domínio da oficina.

## Decisão

Mantemos o PostgreSQL e o promovemos a Flexible Server. Há um banco por ambiente no mesmo servidor, TLS obrigatório, backup do serviço e dois papéis: `wrenchbox_app` para a API e `wrenchbox_auth` para a função.

## Alternativas

Um SQL Server gerenciado exigiria outro provider do EF e outra suíte de testes. Um Postgres em pod não atende o pedido de banco gerenciado. O Cosmos DB não oferece a transação relacional em que a aprovação da ordem e a baixa de estoque acontecem juntas.

## Consequências

A connection string da API e a da função saem do Key Vault. A migration do EF Core continua sendo a fonte do esquema. O script `sql/roles.sql`, no repositório do banco, cria os papéis depois que o servidor existe.
