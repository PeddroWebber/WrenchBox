namespace WrenchBox.Domain.Entities;

public class WorkOrderPartItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; private set; }
    public Guid PartId { get; private set; }
    public string PartName { get; private set; } = string.Empty;
    public string PartSku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;

    private WorkOrderPartItem() { }

    public static WorkOrderPartItem Create(Guid workOrderId, Part part, int quantity)
    {
        if (quantity <= 0)
            throw new Exceptions.DomainException("Part quantity must be greater than zero.");

        if (!part.HasSufficientStock(quantity))
            throw new Exceptions.DomainException($"Insufficient stock for part '{part.Sku}'.");

        return new WorkOrderPartItem
        {
            WorkOrderId = workOrderId,
            PartId = part.Id,
            PartName = part.Name,
            PartSku = part.Sku,
            Quantity = quantity,
            UnitPrice = part.UnitPrice
        };
    }
}
