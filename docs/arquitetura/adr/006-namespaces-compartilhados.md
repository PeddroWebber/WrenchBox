# ADR 006 — Namespaces compartilhados

- Status: aceita
- Data: 2026-11-01

## Contexto

O crédito Azure for Students não comporta dois clusters AKS e dois servidores PostgreSQL só para separar homologação de produção. O enunciado exige deploy automático das duas branches.

## Decisão

Um AKS, namespaces `wrenchbox-homolog` e `wrenchbox-prod`. Um Flexible Server, databases `wrenchbox_homolog` e `wrenchbox_prod`. Um API Management com duas APIs. A branch `homolog` publica no primeiro; `main` publica no segundo. Os workflows são distintos e as branches ficam protegidas.

## Consequências

Uma falha de node afeta os dois ambientes. É uma limitação de orçamento, não o desenho final. As variáveis do Terraform permitem separar cluster e servidor quando o crédito deixar de ser o limite. Segredos continuam separados por nome no Key Vault.
