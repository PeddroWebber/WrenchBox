namespace WrenchBox.Domain.Enums;

public enum WorkOrderStatus
{
    Received = 0,
    InDiagnosis = 1,
    AwaitingApproval = 2,
    InExecution = 3,
    Completed = 4,
    Delivered = 5
}
