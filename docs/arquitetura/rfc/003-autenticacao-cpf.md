# RFC 003 — Autenticação por CPF

Status: aceita em 1º de novembro de 2026.

## Contexto

As rotas da área do cliente precisam de autenticação por CPF. O administrador já entra com e-mail e senha. O token de acompanhamento do e-mail é o fluxo da fase anterior e continua válido no link do orçamento.

## Decisão

Uma Azure Function, isolada da API, valida o CPF, consulta `customers` e emite um JWT HMAC. A chave, o issuer `WrenchBox` e a audience `WrenchBox.Admin` são os mesmos do login interno. O token leva `sub`, `customer_id` e o papel `Customer`, e expira em 30 minutos. A API só libera `/api/v1/me/*` para esse papel e responde 404 quando a ordem não é daquele cliente.

O API Management aplica a mesma regra na borda. No laboratório, o Traefik chama `GET /auth/validate` da função antes de encaminhar.

## Consequências

A regra do dígito verificador existe nos dois repositórios, com os mesmos vetores de teste. A função não referencia o projeto de domínio. Cliente inativo ou bloqueado não recebe token. A resposta HTTP não distingue o motivo, para não permitir enumeração de CPF.
