# RFC 001 — Escolha da Azure

- Status: aceita
- Data: 2026-11-01

## Contexto

O desafio pede deploy em nuvem, função serverless, API Gateway, Kubernetes com escalabilidade e banco gerenciado. A conta disponível é Azure for Students.

## Decisão

Usar Azure API Management (Consumption), Azure Functions no plano Consumption, AKS com um node pool `Standard_B2s` e PostgreSQL Flexible Server. Identidade federada do GitHub (OIDC) publica os quatro repositórios, sem segredo de longa duração no workflow.

## Consequências

O API Management Consumption e o Functions Consumption não entram em rede virtual. O PostgreSQL fica com acesso público restrito a serviços Azure e ao IP de saída do AKS, com TLS. Endpoint privado e Functions Premium ficam como evolução quando houver orçamento. O perfil local (Traefik, função em container, Postgres e Grafana) reproduz o fluxo para o vídeo sem gastar crédito.
