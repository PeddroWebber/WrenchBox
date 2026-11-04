# RFC 001 — Escolha da Azure

Status: aceita em 1º de novembro de 2026.

## Contexto

A fase pede a oficina em nuvem: função serverless, API Gateway, Kubernetes capaz de escalar e um banco gerenciado. A conta disponível é a Azure for Students. A escolha precisa caber nesse crédito e, ainda assim, ser uma arquitetura que se sustenta numa revisão.

## Decisão

Usamos Azure API Management no tier Consumption, Azure Functions no plano Consumption, um AKS com um node pool `Standard_B2s` e o PostgreSQL Flexible Server. O GitHub publica os quatro repositórios com identidade federada (OIDC). O workflow não guarda segredo de longa duração da Azure.

## Consequências

O API Management Consumption e o Functions Consumption não entram em rede virtual. O PostgreSQL fica com acesso público restrito aos serviços Azure e ao IP de saída do AKS, sempre com TLS. Endpoint privado e Functions Premium ficam como evolução, para quando houver orçamento. O perfil local — Traefik, função em container, Postgres e Grafana — reproduz o mesmo fluxo para desenvolver e gravar sem gastar crédito.
