# ADR 005 — Itens da OS como fotografia

Status: aceita em 1º de novembro de 2026.

## Contexto

O preço de peça e de serviço muda. Uma ordem já enviada ao cliente não pode mudar de valor porque o catálogo foi atualizado.

## Decisão

`work_order_service_items` e `work_order_part_items` guardam nome, quantidade e preço no momento da abertura. Não há chave estrangeira para `services` nem para `parts`. A baixa de estoque, essa sim, referencia a peça viva e, a partir desta fase, a ordem (`stock_movements.work_order_id`).

## Consequências

O relatório histórico bate com o orçamento que o cliente aprovou. Apagar um serviço do catálogo não apaga a linha da ordem.
