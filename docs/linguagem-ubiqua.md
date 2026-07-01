# Linguagem Ubíqua

Vocabulário oficial compartilhado entre equipe de negócio, desenvolvimento e documentação da API.

## Contexto: Atendimento e Ordem de Serviço

| Termo (PT) | Termo (EN/código) | Definição |
|------------|-------------------|-----------|
| **Ordem de Serviço (OS)** | `WorkOrder` | Registro formal de um atendimento na oficina, vinculado a um cliente, veículo, serviços e peças. |
| **Número da OS** | `OrderNumber` | Identificador legível sequencial (ex.: `WO-2026-00001`). |
| **Status da OS** | `WorkOrderStatus` | Estado atual no fluxo operacional (Recebida → Entregue). |
| **Orçamento** | `TotalAmount` | Valor total calculado automaticamente a partir de serviços e peças. |
| **Token de Acompanhamento** | `TrackingToken` | Chave única enviada ao cliente para consultar e aprovar o orçamento. |
| **Diagnóstico** | `StartDiagnosis` | Fase em que a oficina analisa o veículo antes de enviar o orçamento. |
| **Aprovação do Orçamento** | `ApproveBudget` | Ação do cliente que autoriza a execução e dispara baixa de estoque. |
| **Histórico de Status** | `StatusHistory` | Trilha de auditoria de cada transição de status da OS. |
| **Item de Serviço** | `WorkOrderServiceItem` | Serviço do catálogo incluído na OS com quantidade e preço congelado. |
| **Item de Peça** | `WorkOrderPartItem` | Peça do catálogo incluída na OS com quantidade e preço congelado. |

## Contexto: Cadastro

| Termo (PT) | Termo (EN/código) | Definição |
|------------|-------------------|-----------|
| **Cliente** | `Customer` | Pessoa física (CPF) ou jurídica (CNPJ) atendida pela oficina. |
| **Documento** | `Document` | Value Object com CPF ou CNPJ validado algoritmicamente. |
| **Veículo** | `Vehicle` | Automóvel identificado por placa, marca, modelo e ano. |
| **Placa** | `Plate` | Value Object no formato brasileiro (legado ou Mercosul). |
| **Serviço** | `Service` | Item do catálogo de mão de obra (ex.: Troca de Óleo). |
| **Peça / Insumo** | `Part` | Item físico com SKU, preço e controle de estoque. |

## Contexto: Estoque

| Termo (PT) | Termo (EN/código) | Definição |
|------------|-------------------|-----------|
| **Estoque** | `StockQuantity` | Quantidade disponível de uma peça no depósito. |
| **Estoque Mínimo** | `MinimumStock` | Limite de alerta para reposição. |
| **Movimentação de Estoque** | `StockMovement` | Registro de entrada, saída ou ajuste. |
| **Ajuste de Estoque** | `AdjustStock` | Correção manual (+/-) com motivo registrado. |
| **Baixa de Estoque** | `Deduct` | Saída automática ao aprovar orçamento da OS. |
| **SKU** | `Sku` | Código único de identificação da peça. |

## Contexto: Segurança e Operação

| Termo (PT) | Termo (EN/código) | Definição |
|------------|-------------------|-----------|
| **Administrador** | `AdminUser` | Usuário interno com acesso JWT às APIs administrativas. |
| **Notificação de Orçamento** | `BudgetNotification` | Envio (simulado em log) do orçamento ao e-mail do cliente. |
| **Tempo Médio de Execução** | `AverageExecutionTime` | Métrica calculada entre início da execução e conclusão da OS. |

## Status da Ordem de Serviço (enum)

| Valor API | Significado (PT) | Descrição |
|-----------|------------------|-----------|
| `Received` | Recebida | OS criada na recepção. |
| `InDiagnosis` | Em diagnóstico | Oficina analisando o veículo. |
| `AwaitingApproval` | Aguardando aprovação | Orçamento enviado; aguarda cliente. |
| `InExecution` | Em execução | Cliente aprovou; serviço em andamento. |
| `Completed` | Finalizada | Serviço concluído; aguardando retirada. |
| `Delivered` | Entregue | Veículo devolvido ao cliente. |

## Tipos de Movimentação de Estoque (enum)

| Valor API | Significado |
|-----------|-------------|
| `Adjustment` | Ajuste manual de estoque |
| `Deduction` | Baixa por aprovação de OS |
| `Release` | Reservado para futuras liberações |
