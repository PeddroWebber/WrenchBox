namespace WrenchBox.Domain.Enums;

public static class WorkOrderStatusLabels
{
    public static string ToPortuguese(this WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.Received => "Recebida",
        WorkOrderStatus.InDiagnosis => "Diagnóstico",
        WorkOrderStatus.AwaitingApproval => "Aguardando Aprovação",
        WorkOrderStatus.InExecution => "Execução",
        WorkOrderStatus.Completed => "Finalizada",
        WorkOrderStatus.Delivered => "Entregue",
        _ => status.ToString()
    };

    public static int QueuePriority(this WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.InExecution => 0,
        WorkOrderStatus.AwaitingApproval => 1,
        WorkOrderStatus.InDiagnosis => 2,
        WorkOrderStatus.Received => 3,
        _ => 99
    };

    public static bool IsClosed(this WorkOrderStatus status) =>
        status is WorkOrderStatus.Completed or WorkOrderStatus.Delivered;
}
