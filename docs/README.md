# Documentação WrenchBox

Documentação técnica e de domínio do **WrenchBox** — Sistema Integrado de Atendimento e Execução de Serviços para oficinas mecânicas.

## Índice

| Documento | Conteúdo |
|-----------|----------|
| [**Documentação DDD Completa**](documentacao-ddd-completa.md) | Event Storming, Context Map, agregados, linguagem ubíqua e mapeamento para o código |
| [Linguagem Ubíqua](linguagem-ubiqua.md) | Vocabulário compartilhado entre negócio e código |
| [Lógica de Negócio](logica-de-negocio.md) | Regras de domínio, agregados, invariantes e transições |
| [Modelagem DDD](modelagem-ddd.md) | Bounded contexts, camadas e diagramas |
| [Event Storming — Estoque](event-storming-estoque.md) | Fluxo de gestão de peças e insumos |
| [API — Endpoints](api-endpoints.md) | Referência de todos os endpoints REST |
| [Collection Postman](WrenchBox.postman_collection.json) | Collection das APIs |
| [Análise de Vulnerabilidades](analise-vulnerabilidades.md) | Revisão de segurança do MVP |

## Arquitetura em camadas

```
┌─────────────────────────────────────────┐
│           WrenchBox.Api                 │  Controllers REST, Swagger, JWT
├─────────────────────────────────────────┤
│        WrenchBox.Application            │  Commands/Queries (MediatR), DTOs, Validators
├─────────────────────────────────────────┤
│          WrenchBox.Domain               │  Entidades, Value Objects, Regras de negócio
├─────────────────────────────────────────┤
│       WrenchBox.Infrastructure          │  EF Core, PostgreSQL, Repositórios, JWT, Notificações
└─────────────────────────────────────────┘
```

## Atores do sistema

| Ator | Papel | Autenticação |
|------|-------|--------------|
| **Administrador** | Gestão de cadastros, OS, estoque e métricas | JWT Bearer (`Authorization: Bearer <token>`) |
| **Cliente** | Consulta e aprovação de orçamento | Token de acompanhamento (`X-Tracking-Token`) |
| **Sistema** | Geração de número de OS, notificação de orçamento, histórico | Interno |
