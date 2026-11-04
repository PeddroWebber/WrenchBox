# Sequência — autenticação por CPF

O cliente não escolhe senha. Ele informa o CPF. A função confere os dígitos, procura a linha em `customers` e lê o status. Só um cliente Ativo recebe token. Se o CPF é inválido, se não existe ou se o cliente está inativo ou bloqueado, a resposta é sempre a mesma. O motivo fica no log, junto do trace id, para o operador.

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

O administrador continua entrando em `POST /api/v1/auth/login`, com e-mail e senha. O gateway deixa essa rota aberta e exige o papel `Admin` no restante de `/api/v1`. O acompanhamento pelo link do e-mail e o webhook ficam de fora dessa regra: cada um tem o próprio segredo.
