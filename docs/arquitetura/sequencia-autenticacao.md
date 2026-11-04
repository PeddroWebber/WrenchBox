# Sequência — autenticação por CPF

O cliente não envia senha. A função confere o algoritmo do CPF, a linha em `customers` e o status. Só um cliente Ativo recebe token. A resposta de erro é sempre a mesma; o motivo vai para o log.

```mermaid
sequenceDiagram
  participant Cliente
  participant Gateway as API_Management
  participant Funcao as Azure_Function
  participant Banco as PostgreSQL
  participant API as WrenchBox_API

  Cliente->>Gateway: POST /auth/token cpf
  Gateway->>Funcao: encaminha sem JWT
  Funcao->>Funcao: valida digitos do CPF
  Funcao->>Banco: SELECT id, status WHERE document = cpf
  alt CPF invalido, inexistente, inativo ou bloqueado
    Funcao-->>Cliente: 401 generico
    Funcao->>Funcao: log com o motivo e trace id
  else Cliente ativo
    Funcao-->>Cliente: JWT role Customer, claim customer_id
  end
  Cliente->>Gateway: GET /api/v1/me/work-orders Authorization Bearer
  Gateway->>Gateway: validate-jwt role Customer
  Gateway->>API: encaminha o token
  API->>API: confere customer_id e filtra as OS
  API-->>Cliente: somente as ordens daquele cliente
```

O administrador continua em `POST /api/v1/auth/login`. O gateway deixa essa rota aberta e exige role `Admin` no restante de `/api/v1`, exceto acompanhamento por e-mail e webhook, que têm segredo próprio.
