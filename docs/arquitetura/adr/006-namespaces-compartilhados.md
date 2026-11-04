# ADR 006 — Namespaces compartilhados

Status: aceita em 1º de novembro de 2026.

## Contexto

O crédito Azure for Students não comporta dois clusters AKS e dois servidores PostgreSQL só para separar homologação de produção. Mesmo assim, as duas branches precisam de deploy automático.

## Decisão

Há um AKS, com os namespaces `wrenchbox-homolog` e `wrenchbox-prod`. Há um Flexible Server, com os databases `wrenchbox_homolog` e `wrenchbox_prod`. Há um API Management, com duas APIs. A branch `homolog` publica no primeiro ambiente. A branch `main` publica no segundo. Os workflows são distintos e as branches ficam protegidas: sem push direto, só merge por pull request.

## Consequências

Uma falha de node afeta os dois ambientes. É um limite de orçamento, não o desenho final. As variáveis do Terraform permitem separar cluster e servidor quando o crédito deixar de ser o limite. Os segredos continuam separados por nome no Key Vault.
