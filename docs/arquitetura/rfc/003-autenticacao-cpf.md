# RFC 003 — Autenticação por CPF

- Status: aceita
- Data: 2026-11-01

## Contexto

As rotas sensíveis do cliente precisam de autenticação por CPF. O administrador já entra com e-mail e senha. O token de acompanhamento do e-mail é o fluxo da fase anterior e continua válido para o link do orçamento.

## Decisão

Uma Azure Function, isolada da API, valida o CPF, consulta `customers` e emite JWT HMAC com a mesma chave, issuer `WrenchBox` e audience `WrenchBox.Admin` já usados pelo login interno. Claims: `sub`, `customer_id` e role `Customer`. Expiração de 30 minutos. A API autoriza `/api/v1/me/*` só para essa role e ignora ordens de outro cliente (404).

O API Management aplica a mesma regra na borda. No laboratório, o Traefik chama `GET /auth/validate` da função antes de encaminhar.

## Consequências

A regra do dígito verificador existe nos dois repositórios, com os mesmos vetores de teste. O domínio não é referenciado pela função. Cliente Inativo ou Bloqueado não recebe token. A resposta HTTP não distingue o motivo, para não permitir enumeração de CPF.
