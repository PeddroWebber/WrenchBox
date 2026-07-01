namespace WrenchBox.Domain.Entities;

public class WorkOrderServiceItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; private set; }
    public Guid ServiceId { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;

    private WorkOrderServiceItem() { }

    public static WorkOrderServiceItem Create(Guid workOrderId, Service service, int quantity)
    {
        if (quantity <= 0)
            throw new Exceptions.DomainException("Service quantity must be greater than zero.");

        return new WorkOrderServiceItem
        {
            WorkOrderId = workOrderId,
            ServiceId = service.Id,
            ServiceName = service.Name,
            Quantity = quantity,
            UnitPrice = service.UnitPrice
        };
    }
}
