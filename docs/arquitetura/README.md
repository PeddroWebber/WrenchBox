# Arquitetura da Fase 3

Esta pasta reúne a visão de operação da oficina: como os componentes se falam, como o cliente entra, como uma ordem nasce e quais decisões ficaram registradas. Os diagramas abaixo são a referência para o deploy e para a apresentação.

| Documento | Conteúdo |
|-----------|----------|
| [Diagrama de componentes](diagrama-componentes.md) | Azure, APIs, banco e monitoramento |
| [Sequência — autenticação](sequencia-autenticacao.md) | CPF, função e JWT |
| [Sequência — abertura de OS](sequencia-abertura-os.md) | Abertura e notificação |
| [Banco de dados](banco-de-dados.md) | Justificativa do PostgreSQL e diagrama ER |
| [RFC 001](rfc/001-escolha-azure.md) | Nuvem |
| [RFC 002](rfc/002-postgresql.md) | Banco gerenciado |
| [RFC 003](rfc/003-autenticacao-cpf.md) | Autenticação do cliente |
| [ADR 001](adr/001-otel-azure-monitor.md) | Observabilidade |
| [ADR 002](adr/002-hpa.md) | Escalabilidade |
| [ADR 003](adr/003-comunicacao-http.md) | Comunicação síncrona |
| [ADR 004](adr/004-jwt-sem-cpf.md) | Token sem CPF |
| [ADR 005](adr/005-snapshot-itens.md) | Itens da OS |
| [ADR 006](adr/006-namespaces-compartilhados.md) | Homologação e produção |

O Swagger da API fica em `http://localhost:8080/swagger` no ensaio local e na URL do API Management depois do deploy. A collection está em [../WrenchBox.postman_collection.json](../WrenchBox.postman_collection.json).

Na Azure, a ordem de apply é banco, depois a função, depois Kubernetes e API Management, e por último esta aplicação.
