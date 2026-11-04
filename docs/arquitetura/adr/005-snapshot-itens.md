# ADR 005 — Itens da OS como fotografia

- Status: aceita
- Data: 2026-11-01

## Contexto

O preço de peça e de serviço muda. Uma OS já enviada ao cliente não pode mudar de valor porque o catálogo foi atualizado.

## Decisão

`work_order_service_items` e `work_order_part_items` guardam nome, quantidade e preço no momento da abertura. Não há chave estrangeira para `services` nem para `parts`. A baixa de estoque, essa sim, referencia a peça viva e, a partir desta fase, a OS (`stock_movements.work_order_id`).

## Consequências

Relatórios históricos batem com o orçamento aprovado. Apagar um serviço do catálogo não apaga a linha da OS.
